#nullable enable // Enable nullable reference types context for this file

using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading; // Required for CancellationTokenSource used in timeout
using ResourceFarmer.Resources;
using ResourceFarmer.PlayerBase; // Assuming your ResourceType enum is defined here
using ResourceFarmer.Items; // For ToolBase, AppliedBonusInstance, ToolBonusName

// Assuming your Player class and ResourceType enum are accessible
// using MyGameNamespace;

namespace ResourceFarmer.SavingService
{
	// --- Data Transfer Objects (DTOs) ---
	// Consider moving these to separate files

	/// <summary>
	/// DTO for serializing ToolBase data
	/// </summary>
	public class ToolDataDto
	{
		public string ToolType { get; set; } = string.Empty;
		public string Material { get; set; } = string.Empty;
		public int Level { get; set; }
		public float Quality { get; set; }
		public List<AppliedBonusDto> AppliedBonuses { get; set; } = new();
	}

	/// <summary>
	/// DTO for serializing AppliedBonusInstance data
	/// </summary>
	public class AppliedBonusDto
	{
		public string Name { get; set; } = string.Empty;
		public float ActualMagnitude { get; set; }
	}

	/// <summary>
	/// DTO for tracking crafting progress and achievements
	/// </summary>
	public class CraftingProgressDto
	{
		public List<string> UnlockedRecipes { get; set; } = new();
		public Dictionary<string, int> ItemsCraftedCount { get; set; } = new(); // Recipe name -> count
		public Dictionary<string, int> MaterialsUsedCount { get; set; } = new(); // Material type -> count
		public DateTime LastCraftingActivity { get; set; }
	}

	/// <summary>
	/// DTO matching the structure expected/returned by the backend over WebSocket.
	/// </summary>
	public class PlayerDataApiDto
	{
		public required string SteamId { get; set; }
		public int Level { get; set; }
		public double Experience { get; set; }
		public float Money { get; set; }
		public int PrestigePoints { get; set; }
		public string? ResourcesJson { get; set; }
		public string? EquippedToolJson { get; set; } // NEW: Serialized equipped tool data
		public string? CraftingProgressJson { get; set; } // NEW: Crafting achievements/progress
		// public string? PlantDataJson { get; set; }
		public DateTime LastSaved { get; set; }
	}

	// --- WebSocket Message Structure Examples ---
	// Define the structure for messages sent TO the server

	public class WebSocketMessageBaseOut
	{
		public required string Action { get; set; } // e.g., "save"
	}

	public class SaveDataMessage : WebSocketMessageBaseOut
	{
		public PlayerDataApiDto? Payload { get; set; }
	}

	// --- Incoming message structure (basic example) ---
	public class WebSocketMessageBaseIn
	{
		public string? Action { get; set; } // e.g., "ack", "error", "server_update"
	}

	public class LoadDataRequestMessage : WebSocketMessageBaseOut // Changed base type
	{
		public string? SteamId { get; set; }
		public string RequestId { get; set; } = Guid.NewGuid().ToString();
	}

	public class LoadDataResponseMessage : WebSocketMessageBaseIn // Changed base type
	{
		public string? RequestId { get; set; } // To match response with request
		public PlayerDataApiDto? Payload { get; set; }
		public bool Found { get; set; } = false;
	}

	public class ErrorMessage : WebSocketMessageBaseIn // Changed base type
	{
		public string? RequestId { get; set; }
		public string? Error { get; set; }
	}


	/// <summary>
	/// Component managing the WebSocket connection to the external data persistence backend.
	/// Connects using SteamID in the URL query parameter.
	/// Handles sending save data ("fire and forget" with retry) over the WebSocket.
	/// Load functionality uses a TaskCompletionSource with manual timeout handling.
	/// </summary>
	public sealed class SavingServiceComponent : Component
	{
		// --- Configuration ---
		[Property] public string WebSocketUrl { get; set; } = "ws://localhost:5000/ws"; // Base URL without query params
		[Property] public float LoadRequestTimeoutSeconds { get; set; } = 10.0f;

		[Property] public bool Disabled { get; set; } = true; // Enable/disable the component

		private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		private WebSocket? _webSocket;
		private bool _isConnecting = false;
		private bool _isConnected = false;
		private long _hostSteamId = 0; // Store the host's SteamID for reconnects

		// --- Retry Queue for Failed Sends ---
		private readonly ConcurrentQueue<string> _retryQueue = new ConcurrentQueue<string>();
		private TimeSince _timeSinceLastRetryAttempt = 0;
		private const float RetryAttemptInterval = 5.0f;

		// --- Pending Load Requests ---
		private readonly ConcurrentDictionary<string, TaskCompletionSource<PlayerDataApiDto?>> _pendingLoadRequests = new();


		// --- Component Lifecycle ---

		protected override void OnStart()
		{

			if ( !Networking.IsHost ) { Enabled = false; return; }
			if ( Disabled ) { Log.Warning( "[SavingServiceComponent] Component is disabled. Exiting." ); return; }

			// Store the host's SteamID when the component starts
			_hostSteamId = Game.SteamId; // Assumes Game.SteamId is the host's ID
			if ( _hostSteamId == 0 )
			{
				Log.Error( "[SavingServiceComponent] Failed to get valid host SteamID on start!" );
				Enabled = false;
				return;
			}

			Log.Info( $"[SavingServiceComponent] Starting for host: {_hostSteamId}" );
			_ = ConnectWebSocketAsync( _hostSteamId ); // Connect using the host's SteamID
		}

		protected override void OnDestroy()
		{
			if ( !Networking.IsHost ) return;
			Log.Info( "[SavingServiceComponent] Destroying..." );
			DisconnectWebSocket( "Component destroyed" );
		}

		protected override void OnFixedUpdate()
		{
			if ( Disabled ) return; // Skip if disabled
			if ( !Networking.IsHost ) return;
			if ( !_isConnected && !_isConnecting && _timeSinceLastRetryAttempt > RetryAttemptInterval )
			{
				Log.Info( "[SavingServiceComponent] WebSocket disconnected, attempting reconnect..." );
				_ = ReconnectWithDelayAsync();
				_timeSinceLastRetryAttempt = 0;
			}
			if ( _isConnected && !_retryQueue.IsEmpty && _timeSinceLastRetryAttempt > RetryAttemptInterval )
			{
				if ( _retryQueue.TryDequeue( out string? messageToRetry ) )
				{
					Log.Info( $"[SavingServiceComponent] Attempting to retry sending message..." );
					_ = SendWebSocketMessageAsync( messageToRetry );
				}
				_timeSinceLastRetryAttempt = 0;
			}
		}

		private async Task ReconnectWithDelayAsync()
		{
			if ( _isConnecting || _isConnected || _hostSteamId == 0 ) return; // Don't reconnect if already connected or no valid SteamID
			_isConnecting = true;
			await Task.DelaySeconds( RetryAttemptInterval );
			if ( !_isConnected )
			{
				// Use the stored host SteamID for reconnection attempts
				await ConnectWebSocketAsync( _hostSteamId, null );
			}
			else { _isConnecting = false; }
		}


		// --- Public Methods ---

		/// <summary>
		/// Sends player data ("Fire and Forget" with retry queue).
		/// </summary>
		public async Task SaveDataAsync( Player? player )
		{
			if ( player?.Network.Owner == null )
			{ Log.Warning( "[SavingServiceComponent] Cannot save: Invalid player or Network Owner." ); return; }

			long steamId = player.Network.Owner.SteamId;
			Log.Info( $"[SavingServiceComponent] Preparing save message for SteamID: {steamId}" );
			try
			{
				var dataToSend = CreateDtoFromPlayer( player );
				var message = new SaveDataMessage { Action = "save", Payload = dataToSend };
				string jsonPayload = JsonSerializer.Serialize<object>( message, GetJsonOptions() );
				await SendWebSocketMessageAsync( jsonPayload );
				Log.Info( $"[SavingServiceComponent] Save message queued/sent for SteamID: {steamId}" );
			}
			catch ( Exception ex ) { Log.Error( $"[SavingServiceComponent] Error preparing save message for {steamId}: {ex.Message}" ); }
		}

		/// <summary>
		/// Requests player data using TaskCompletionSource and manual timeout.
		/// </summary>
		public async Task<PlayerDataApiDto?> LoadDataAsync( long steamId )
		{
			if ( Disabled ) { Log.Warning( "[SavingServiceComponent] LoadDataAsync called but component is disabled." ); return null; }
			if ( steamId == 0 )
			{
				Log.Warning( "[SavingServiceComponent] Cannot load: Invalid SteamID (0)." );
				return null;
			}

			// --- Wait for connection with timeout ---
			var connectionWaitTimeout = TimeSpan.FromSeconds( LoadRequestTimeoutSeconds ); // Reuse timeout or define a specific one
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			while ( !_isConnected && stopwatch.Elapsed < connectionWaitTimeout )
			{
				if ( _isConnecting )
				{
					Log.Info( $"[SavingServiceComponent] LoadDataAsync waiting for connection... (SteamID: {steamId})" );
				}
				else
				{
					// If not connected and not connecting, trigger a reconnect attempt
					Log.Warning( $"[SavingServiceComponent] LoadDataAsync found disconnected state, attempting reconnect... (SteamID: {steamId})" );
					_ = ReconnectWithDelayAsync(); // Fire and forget reconnect
				}
				await Task.Delay( 100 ); // Wait a short interval before checking again
			}
			stopwatch.Stop();

			if ( !_isConnected || _webSocket == null )
			{
				Log.Warning( $"[SavingServiceComponent] Cannot load: Connection timed out or failed after {stopwatch.ElapsedMilliseconds}ms. (SteamID: {steamId})" );
				return null; // Return null if connection failed/timed out
			}
			// --- End Wait for connection ---


			string steamIdStr = steamId.ToString();
			var message = new LoadDataRequestMessage { Action = "load", SteamId = steamIdStr };
			string requestId = message.RequestId;

			var tcs = new TaskCompletionSource<PlayerDataApiDto?>();
			if ( !_pendingLoadRequests.TryAdd( requestId, tcs ) )
			{ Log.Error( $"[SavingServiceComponent] Failed to add pending load request (duplicate RequestId?): {requestId}" ); return null; }

			Log.Info( $"[SavingServiceComponent] Preparing load request for SteamID: {steamIdStr} (ReqID: {requestId})" );
			var cts = new CancellationTokenSource();
			// Start the timeout *after* successfully adding the request
			_ = HandleLoadTimeoutAsync( requestId, tcs, TimeSpan.FromSeconds( LoadRequestTimeoutSeconds ), cts.Token );

			try
			{
				string jsonPayload = JsonSerializer.Serialize<object>( message, GetJsonOptions() );
				await SendWebSocketMessageAsync( jsonPayload ); // This now assumes connection is established
				Log.Info( $"[SavingServiceComponent] Load request sent for SteamID: {steamIdStr} (ReqID: {requestId})" );

				// Await the response from the server (or timeout)
				PlayerDataApiDto? result = await tcs.Task; // This task is completed by HandleWebSocketMessage or HandleLoadTimeoutAsync

				Log.Info( $"[SavingServiceComponent] LoadDataAsync completed for ReqID: {requestId}. Result: {(result == null ? "null/not found" : "Data received")}" );
				return result;
			}
			catch ( TaskCanceledException )
			{
				// This catch block is specifically for the tcs.Task being cancelled (likely by HandleLoadTimeoutAsync)
				Log.Warning( $"[SavingServiceComponent] Load request timed out (TaskCanceled) for SteamID: {steamIdStr} (ReqID: {requestId})" );
				// No need to return null here, finally block handles cleanup
				return null; // Explicitly return null on timeout cancellation
			}
			catch ( Exception ex )
			{
				Log.Error( $"[SavingServiceComponent] Error during load request/wait for {steamIdStr} (ReqID: {requestId}): {ex.Message}" );
				tcs.TrySetException( ex ); // Propagate exception if something else went wrong
				return null; // Return null on other exceptions
			}
			finally
			{
				// Ensure cleanup happens regardless of success, failure, or timeout
				_pendingLoadRequests.TryRemove( requestId, out _ );
				if ( !cts.IsCancellationRequested )
				{
					cts.Cancel(); // Cancel the timeout task if it's still running
				}
				cts.Dispose();
			}
		}

		/// <summary>
		/// Helper task to handle timeouts for load requests.
		/// </summary>
		private async Task HandleLoadTimeoutAsync( string requestId, TaskCompletionSource<PlayerDataApiDto?> tcs, TimeSpan delay, CancellationToken cancellationToken )
		{
			try
			{
				await Task.Delay( 10000, cancellationToken );
				if ( tcs.TrySetCanceled( cancellationToken ) ) { Log.Warning( $"[SavingServiceComponent] Load request timed out internally for ReqID: {requestId}" ); }
			}
			catch ( TaskCanceledException ) { Log.Info( $"[SavingServiceComponent] Load timeout task cancelled for ReqID: {requestId} (likely completed normally)." ); }
			catch ( Exception ex ) { Log.Error( $"[SavingServiceComponent] Error in HandleLoadTimeoutAsync for ReqID {requestId}: {ex.Message}" ); tcs.TrySetException( ex ); }
		}

		/// <summary>
		/// Applies loaded DTO data back to the S&amp;box Player component.
		/// </summary>
		public static void ApplyLoadedData( Player? player, PlayerDataApiDto? loadedData )
		{
			if ( player == null || loadedData == null ) { Log.Warning( "[SavingServiceComponent] ApplyLoadedData called with null player or data." ); return; }
			Log.Info( $"[SavingServiceComponent] Applying loaded data to Player for SteamID: {loadedData.SteamId}" );
			
			// Apply basic stats
			player.Level = loadedData.Level;
			player.Experience = loadedData.Experience;
			player.Money = loadedData.Money;
			player.PrestigePoints = loadedData.PrestigePoints;
			
			// Apply inventory
			player.Inventory = ConvertApiFormatToInventory( loadedData.ResourcesJson, loadedData.SteamId );
			
			// Apply equipped tool
			var loadedTool = ConvertJsonToTool( loadedData.EquippedToolJson, loadedData.SteamId );
			if ( loadedTool != null )
			{
				player.EquippedTool = loadedTool;
				Log.Info( $"[SavingServiceComponent] Restored equipped tool: {loadedTool.Material} {loadedTool.ToolType} Level {loadedTool.Level}" );
			}
			else
			{
				Log.Info( $"[SavingServiceComponent] No equipped tool data found for {loadedData.SteamId}" );
			}
			
			// Apply crafting progress
			ApplyCraftingProgressFromJson( player, loadedData.CraftingProgressJson, loadedData.SteamId );
			
			Log.Info( $"[SavingServiceComponent] Finished applying data for SteamID: {loadedData.SteamId}" );
		}


		// --- WebSocket Connection & Handling ---

		/// <summary>
		/// Establishes the WebSocket connection, appending SteamID to the URL.
		/// </summary>
		/// <param name="steamId">The SteamID of the host establishing the connection.</param>
		/// <param name="headers">Optional dictionary of headers.</param>
		private async Task ConnectWebSocketAsync( long steamId, Dictionary<string, string>? headers = null ) // Added steamId parameter
		{
			if ( steamId == 0 ) { Log.Error( "[SavingServiceComponent] Cannot connect: Invalid SteamID (0)." ); _isConnecting = false; return; }
			if ( _isConnecting || _isConnected ) return;
			_isConnecting = true;

			// Construct the URL with the steamId query parameter
			string connectionUrl = $"{WebSocketUrl}?steamId={steamId}";
			Log.Info( $"[SavingServiceComponent] Attempting WebSocket connection to {connectionUrl}..." );

			try
			{
				_webSocket?.Dispose();
				_webSocket = new WebSocket();
				_webSocket.OnMessageReceived += HandleWebSocketMessage;
				_webSocket.OnDisconnected += HandleWebSocketDisconnect;

				// TODO: Add Auth headers if needed, separate from steamId in URL
				// headers ??= new Dictionary<string, string>();
				// headers["Authorization"] = "Bearer YOUR_TOKEN";

				if ( headers != null && headers.Count > 0 )
				{
					Log.Info( "[SavingServiceComponent] Connecting with custom headers..." );
					await _webSocket.Connect( connectionUrl, headers ); // Use URL with steamId
				}
				else
				{
					Log.Info( "[SavingServiceComponent] Connecting without custom headers..." );
					await _webSocket.Connect( connectionUrl ); // Use URL with steamId
				}

				if ( _webSocket.IsConnected )
				{
					_isConnected = true;
					_isConnecting = false;
					Log.Info( "[SavingServiceComponent] WebSocket connected successfully." );
					_timeSinceLastRetryAttempt = RetryAttemptInterval;
				}
				else { HandleWebSocketDisconnect( 4000, "Connection failed immediately" ); }
			}
			catch ( Exception ex ) { HandleWebSocketDisconnect( 4001, $"Connection exception: {ex.Message}" ); }
			finally { if ( !_isConnected ) { _isConnecting = false; } }
		}

		private void DisconnectWebSocket( string reason = "Normal disconnect" )
		{
			if ( _webSocket == null && !_isConnected ) return;
			_isConnected = false;
			_isConnecting = false;
			foreach ( var pending in _pendingLoadRequests ) { pending.Value.TrySetCanceled(); }
			_pendingLoadRequests.Clear();

			var socketToClose = _webSocket;
			_webSocket = null;

			if ( socketToClose != null )
			{
				socketToClose.OnMessageReceived -= HandleWebSocketMessage;
				socketToClose.OnDisconnected -= HandleWebSocketDisconnect;
				Log.Info( $"[SavingServiceComponent] Closing WebSocket connection: {reason}" );
				try { socketToClose.Dispose(); } // Use Dispose for cleanup
				catch ( Exception ex ) { Log.Error( $"[SavingServiceComponent] Error during WebSocket Dispose(): {ex.Message}" ); }
				finally { Log.Info( "[SavingServiceComponent] WebSocket disposed." ); }
			}
		}

		private void HandleWebSocketDisconnect( int status, string reason )
		{
			if ( !_isConnected && !_isConnecting ) return;
			Log.Warning( $"[SavingServiceComponent] WebSocket disconnected! Status: {status}, Reason: {reason}" );
			_isConnected = false;
			_isConnecting = false;
			foreach ( var pending in _pendingLoadRequests ) { pending.Value.TrySetCanceled(); }
			_pendingLoadRequests.Clear();
			_webSocket?.Dispose();
			_webSocket = null;
		}

		private void HandleWebSocketMessage( string message )
		{
			Log.Info( $"[SavingServiceComponent] WebSocket message received: {message}" );
			try
			{
				var baseMessage = JsonSerializer.Deserialize<WebSocketMessageBaseIn>( message, GetJsonOptions() );
				switch ( baseMessage?.Action?.ToLowerInvariant() )
				{
					case "loadresponse":
						var response = JsonSerializer.Deserialize<LoadDataResponseMessage>( message, GetJsonOptions() );
						if ( response?.RequestId != null && _pendingLoadRequests.TryRemove( response.RequestId, out var tcs ) )
						{
							Log.Info( $"[SavingServiceComponent] Received load response for ReqID: {response.RequestId}. Found: {response.Found}" );
							tcs.TrySetResult( response.Found ? response.Payload : null );
						}
						else { Log.Warning( $"[SavingServiceComponent] Received load response with unknown/missing RequestId: {response?.RequestId}" ); }
						break;
					case "error":
						var error = JsonSerializer.Deserialize<ErrorMessage>( message, GetJsonOptions() );
						Log.Error( $"[SavingServiceComponent] Received error from server: {error?.Error} (ReqID: {error?.RequestId})" );
						if ( error?.RequestId != null && _pendingLoadRequests.TryRemove( error.RequestId, out var errorTcs ) ) { errorTcs.TrySetResult( null ); }
						break;
					default:
						Log.Warning( $"[SavingServiceComponent] Received WebSocket message with unhandled action: {baseMessage?.Action}" );
						break;
				}
			}
			catch ( JsonException jsonEx ) { Log.Error( $"[SavingServiceComponent] Failed to deserialize WebSocket message: {jsonEx.Message}. Message: {message}" ); }
			catch ( Exception ex ) { Log.Error( $"[SavingServiceComponent] Error processing WebSocket message: {ex.Message}" ); }
		}

		/// <summary>
		/// Attempts to send a message payload over the WebSocket. Queues on failure.
		/// </summary>
		private async Task SendWebSocketMessageAsync( string jsonPayload )
		{
			if ( !_isConnected || _webSocket == null )
			{ Log.Warning( "[SavingServiceComponent] WebSocket not connected. Queuing message for retry." ); _retryQueue.Enqueue( jsonPayload ); return; }
			try { await _webSocket.Send( jsonPayload ); }
			catch ( Exception ex ) { Log.Error( $"[SavingServiceComponent] Error sending WebSocket message: {ex.Message}. Queuing for retry." ); _retryQueue.Enqueue( jsonPayload ); }
		}

		// --- Private Helper Methods --- (Unchanged)

		private static JsonSerializerOptions GetJsonOptions() => _jsonOptions;

		// --- Private Helper Method Implementations (Copied from previous for completeness) ---
		private static PlayerDataApiDto CreateDtoFromPlayer( Player player )
		{
			long steamId = player.Network!.Owner!.SteamId;
			var resourcesDict = ConvertInventoryToApiFormat( player.Inventory );
			var equippedToolJson = ConvertToolToJson( player.EquippedTool );
			var craftingProgressJson = ConvertCraftingProgressToJson( player );
			
			return new PlayerDataApiDto
			{
				SteamId = steamId.ToString(),
				Level = player.Level,
				Experience = player.Experience,
				Money = player.Money,
				PrestigePoints = player.PrestigePoints,
				ResourcesJson = JsonSerializer.Serialize( resourcesDict, GetJsonOptions() ),
				EquippedToolJson = equippedToolJson,
				CraftingProgressJson = craftingProgressJson,
				LastSaved = DateTime.UtcNow
			};
		}
		private static Dictionary<string, float> ConvertInventoryToApiFormat( IDictionary<ResourceType, float>? inventory )
		{
			if ( inventory == null ) return new Dictionary<string, float>();
			try { return inventory.ToDictionary( kvp => kvp.Key.ToString(), kvp => kvp.Value ); }
			catch ( Exception ex ) { Log.Error( $"[SavingServiceComponent] Error converting inventory keys: {ex.Message}" ); return new Dictionary<string, float>(); }
		}

		private static string? ConvertToolToJson( ToolBase? tool )
		{
			if ( tool == null ) return null;
			try
			{
				var toolDto = new ToolDataDto
				{
					ToolType = tool.ToolType.ToString(),
					Material = tool.Material ?? string.Empty,
					Level = tool.Level,
					Quality = tool.Quality,
					AppliedBonuses = tool.AppliedBonuses?.Select( b => new AppliedBonusDto 
					{ 
						Name = b.Name.ToString(), 
						ActualMagnitude = b.ActualMagnitude 
					} ).ToList() ?? new List<AppliedBonusDto>()
				};
				return JsonSerializer.Serialize( toolDto, GetJsonOptions() );
			}
			catch ( Exception ex )
			{
				Log.Error( $"[SavingServiceComponent] Error serializing tool: {ex.Message}" );
				return null;
			}
		}

		private static string? ConvertCraftingProgressToJson( Player player )
		{
			try
			{
				var progressDto = new CraftingProgressDto
				{
					UnlockedRecipes = player.UnlockedRecipes?.ToList() ?? new List<string>(),
					ItemsCraftedCount = player.ItemsCraftedCount ?? new Dictionary<string, int>(),
					MaterialsUsedCount = player.MaterialsUsedCount ?? new Dictionary<string, int>(),
					LastCraftingActivity = player.LastCraftingActivity
				};
				return JsonSerializer.Serialize( progressDto, GetJsonOptions() );
			}
			catch ( Exception ex )
			{
				Log.Error( $"[SavingServiceComponent] Error serializing crafting progress: {ex.Message}" );
				return null;
			}
		}
		private static IDictionary<ResourceType, float> ConvertApiFormatToInventory( string? resourcesJson, string steamIdForLogging )
		{
			var inventory = new Dictionary<ResourceType, float>();
			if ( string.IsNullOrEmpty( resourcesJson ) ) return inventory;
			try
			{
				var dict = JsonSerializer.Deserialize<Dictionary<string, float>>( resourcesJson, GetJsonOptions() ) ?? new Dictionary<string, float>();
				foreach ( var kvp in dict )
				{
					if ( Enum.TryParse<ResourceType>( kvp.Key, true, out var type ) ) inventory[type] = kvp.Value;
					else Log.Warning( $"[SavingServiceComponent] Unknown resource type '{kvp.Key}' in loaded data for {steamIdForLogging}. Skipping." );
				}
			}
			catch ( Exception ex ) { Log.Error( $"[SavingServiceComponent] Error applying ResourcesJson for {steamIdForLogging}: {ex.Message}" ); return new Dictionary<ResourceType, float>(); }
			return inventory;
		}

		private static ToolBase? ConvertJsonToTool( string? toolJson, string steamIdForLogging )
		{
			if ( string.IsNullOrEmpty( toolJson ) ) return null;
			try
			{
				var toolDto = JsonSerializer.Deserialize<ToolDataDto>( toolJson, GetJsonOptions() );
				if ( toolDto == null ) return null;

				// Convert tool type string back to enum
				if ( !Enum.TryParse<ResourceType>( toolDto.ToolType, true, out var toolType ) )
				{
					Log.Warning( $"[SavingServiceComponent] Unknown tool type '{toolDto.ToolType}' for {steamIdForLogging}. Skipping tool." );
					return null;
				}

				// Convert bonuses back to AppliedBonusInstance
				var bonuses = toolDto.AppliedBonuses?.Select( b => 
				{
					if ( Enum.TryParse<ToolBonusName>( b.Name, true, out var bonusName ) )
					{
						return new AppliedBonusInstance
						{
							Name = bonusName,
							ActualMagnitude = b.ActualMagnitude
						};
					}
					Log.Warning( $"[SavingServiceComponent] Unknown bonus name '{b.Name}' for {steamIdForLogging}. Skipping bonus." );
					return default( AppliedBonusInstance? );
				} ).Where( b => b.HasValue ).Select( b => b!.Value ).ToList() ?? new List<AppliedBonusInstance>();

				return new ToolBase( toolType, toolDto.Material, toolDto.Level, toolDto.Quality, bonuses );
			}
			catch ( Exception ex )
			{
				Log.Error( $"[SavingServiceComponent] Error deserializing tool for {steamIdForLogging}: {ex.Message}" );
				return null;
			}
		}

		private static void ApplyCraftingProgressFromJson( Player player, string? craftingProgressJson, string steamIdForLogging )
		{
			if ( string.IsNullOrEmpty( craftingProgressJson ) ) return;
			try
			{
				var progressDto = JsonSerializer.Deserialize<CraftingProgressDto>( craftingProgressJson, GetJsonOptions() );
				if ( progressDto != null )
				{
					player.UnlockedRecipes = progressDto.UnlockedRecipes?.ToHashSet() ?? new HashSet<string>();
					player.ItemsCraftedCount = progressDto.ItemsCraftedCount ?? new Dictionary<string, int>();
					player.MaterialsUsedCount = progressDto.MaterialsUsedCount ?? new Dictionary<string, int>();
					player.LastCraftingActivity = progressDto.LastCraftingActivity;
					Log.Info( $"[SavingServiceComponent] Applied crafting progress for {steamIdForLogging}: {player.UnlockedRecipes.Count} recipes, {player.ItemsCraftedCount.Count} crafted items tracked." );
				}
			}
			catch ( Exception ex )
			{
				Log.Error( $"[SavingServiceComponent] Error applying crafting progress for {steamIdForLogging}: {ex.Message}" );
				// Initialize with defaults on error
				player.UnlockedRecipes = new HashSet<string>();
				player.ItemsCraftedCount = new Dictionary<string, int>();
				player.MaterialsUsedCount = new Dictionary<string, int>();
				player.LastCraftingActivity = DateTime.UtcNow;
			}
		}
	}
}
