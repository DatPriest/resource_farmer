using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sandbox.Internal; // Assuming Log is here, adjust if needed
						// Make sure you have the correct using statement for WebSocket, e.g., using Sandbox.System; or Sandbox.Network;
using Sandbox.Network; // Example: Adjust namespace if needed

namespace ResourceFarmer.WebSocketTest
{
	/// <summary>
	/// Provides a static method for quickly testing WebSocket connections.
	/// </summary>
	public class WebSocketTester
	{
		// !!! Optional: For debugging lifetime issues ONLY - uncomment temporarily if needed !!!
		// private static WebSocket _debugSocketHolder;

		/// <summary>
		/// Performs a WebSocket connection test: connects, authenticates (optional),
		/// sends a message, and logs received messages. Designed for quick testing.
		/// Call using fire-and-forget: _ = TestWebSocketConnection();
		/// </summary>
		public static async Task TestWebSocketConnection()
		{
			// --- Configuration ---
			long steamId = 123456789; // TODO: Use actual Steam ID if needed
			string connectionUri = $"wss://127.0.0.1:80/ws?steamId={steamId}"; // Ensure Port 7176 and path are correct
			string authServiceName = "YourServiceName"; // TODO: Replace if needed
			bool useAuthentication = false; // TODO: Set to true if testing auth
			string initialMessageToSend = "Hello from single method test!";
			// --- End Configuration ---

			Log.Info( $"[WebSocket Test] Starting test for URI: {connectionUri}" );

			WebSocket socket = null; // Declare outside try block for potential cleanup in finally
			try
			{
				socket = new WebSocket();
				// !!! Optional: For debugging lifetime issues ONLY !!!
				// _debugSocketHolder = socket;

				Log.Info( "[WebSocket Test] WebSocket instance created." );
				Dictionary<string, string> headers = null;

				// --- Authentication (Optional) ---
				if ( useAuthentication )
				{
					Log.Info( $"[WebSocket Test] Attempting to get auth token for '{authServiceName}'..." );
					try
					{
						// Assuming Sandbox.Services.Auth is the correct way to get the token
						var token = await Sandbox.Services.Auth.GetToken( authServiceName );

						if ( !string.IsNullOrEmpty( token ) )
						{
							Log.Info( "[WebSocket Test] Successfully obtained auth token." );
							headers = new Dictionary<string, string>() { { "Authorization", token } };
							Log.Info( "[WebSocket Test] Headers created for authentication." );
						}
						else
						{
							Log.Warning( "[WebSocket Test] Failed to get auth token (token was null or empty). Proceeding without Authorization header." );
						}
					}
					catch ( Exception ex )
					{
						Log.Error( ex, $"[WebSocket Test] Exception while trying to get auth token for '{authServiceName}'. Proceeding without Authorization header." );
					}
				}
				else
				{
					Log.Info( "[WebSocket Test] Skipping authentication." );
				}
				// --- End Authentication ---

				// --- Event Handlers ---
				socket.OnMessageReceived += ( message ) =>
				{
					Log.Info( $"[WebSocket Test] << Message Received: {message}" );
				};

				socket.OnDisconnected += ( status, reason ) =>
				{
					// This is where your original error appeared
					Log.Warning( $"[WebSocket Test] !! WebSocket disconnected. Status: {status}, Reason: '{reason}'" );
					// Consider if cleanup is needed here - disposing the socket again might be redundant if already disposed
					// socket?.Dispose(); // Be cautious with disposing in event handlers
				};




				// --- Connection and Sending ---
				Log.Info( $"[WebSocket Test] Attempting to connect..." );

				if ( headers != null )
				{
					await socket.Connect( connectionUri, headers );
					Log.Info( "[WebSocket Test] Connection attempt with headers initiated." );
				}
				else
				{
					await socket.Connect( connectionUri );
					Log.Info( "[WebSocket Test] Connection attempt without headers initiated." );
				}

				// Short delay to allow connection state to potentially stabilize (optional, for debugging)
				// await Task.Delay(100);

				// Check connection state *after* the attempt.
				// Note: Depending on the library, IsConnected might not be immediately true.
				// Sending a message might be a better test of successful connection.
				Log.Info( $"[WebSocket Test] Post-Connect Check: IsConnected={socket.IsConnected}, State={socket.SubProtocol}" ); // Log current state

				if ( socket.IsConnected ) // Or maybe check socket.State == WebSocketState.Open
				{
					Log.Info( "[WebSocket Test] Connection appears successful based on IsConnected flag!" );

					if ( !string.IsNullOrEmpty( initialMessageToSend ) )
					{
						Log.Info( $"[WebSocket Test] >> Sending initial message: '{initialMessageToSend}'" );
						await socket.Send( initialMessageToSend );
						Log.Info( "[WebSocket Test] Initial message sent." );
					}
				}
				else
				{
					// *** REMOVED THE Dispose() CALL FROM HERE ***
					Log.Warning( $"[WebSocket Test] Connection attempt completed, but socket is not in a fully connected state immediately after await. State={socket.SubProtocol}. Will rely on events or future operations." );
					// If the state isn't 'Open' or 'Connecting', it might have failed silently.
					// You might still want to dispose *if* the state indicates a terminal failure (e.g., Closed, Aborted)
					// if (socket.State == WebSocketState.Closed || socket.State == WebSocketState.Aborted) {
					//    Log.Warning($"[WebSocket Test] Socket state indicates failure ({socket.State}), disposing.");
					//    socket.Dispose();
					// }
				}
			}
			catch ( Exception ex )
			{
				Log.Error( ex, $"[WebSocket Test] !! Exception during Connect/Send for {connectionUri}." );
				// Ensure socket is disposed ONLY on explicit exception during connect/send setup
				socket?.Dispose(); // Use null-conditional operator
				socket = null; // Prevent finally block from disposing again
			}
			// Removed Finally block that might have disposed too early.
			// Relying on OnDisconnected or explicit errors for now in this test setup.
			// For robust apps, proper lifecycle management (like in a class) is better.

			Log.Info( "[WebSocket Test] Test method async execution finished (connection and event handlers may remain active)." );

			await Task.Delay( 10000 );
		}
	}
}
