#nullable enable
using Sandbox; // <-- Add this line
using Sandbox.Citizen; // Required for CitizenAnimationHelper
using System;
using System.Collections.Generic; // Required for Dictionary
using System.Linq; // Required for Linq operations like Sum
using System.Threading.Tasks; // Required for async Task
using ResourceFarmer.Resources;
using ResourceFarmer.Crafting; // Required for CraftingRecipeResource
using ResourceFarmer.SavingService;
using static Sandbox.Soundscape;
using ResourceFarmer.Items;
namespace ResourceFarmer.PlayerBase;
public sealed partial class Player : Component
{
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Eye { get; set; }
	[Property] public float EyeHeight { get; set; } = 64.0f; // Still potentially useful if Eye isn't set

	[Property] public bool IsHost => Networking.IsHost; // Property to check if the player is the host
	public CharacterController Controller => Components.Get<CharacterController>();
	public PlayerController PlayerController => Components.Get<PlayerController>( FindMode.InSelf );
	public CitizenAnimationHelper AnimationHelper => Components.Get<CitizenAnimationHelper>( FindMode.InSelf );
	private Transform _initialSpawnPoint;
	[Property, Sync] public SkinnedModelRenderer? BodyRenderer { get; set; }
	// --- Add Sound Property ---
	[Property, Category( "Audio" )] public SoundEvent SwingSound { get; set; } // Assign swing sound (e.g., axe_swing.sound)


	private TimeSince _timeSinceLastAttack = 0f;
	[Property] public float BaseAttackRate { get; set; } = 1f; // Time in seconds between attacks






	[Sync] // Replicate inventory to the owning client for UI
	public IDictionary<ResourceType, float> Inventory { get; set; } = new Dictionary<ResourceType, float>(); // Use float
	[Sync] public float Money { get; set; } = 0f;
	[Sync] public double Experience { get; set; } = 0f;
	[Sync] public int Level { get; set; } = 1;
	[Sync] public int PrestigePoints { get; set; } = 0;

	[Sync] public ToolBase EquippedTool { get; set; } = null; // Use ToolBase for equipped tool

	[Property] public double CurrentTotalInventoryItems => Inventory.Sum(kvp => kvp.Value); // Total items in inventory
	public float ExperienceToNextLevel => Level * 100; // Example formula

	// Add properties for new components
	public PlayerInteractionComponent Interaction => Components.Get<PlayerInteractionComponent>();
	public PlayerGatheringComponent Gathering => Components.Get<PlayerGatheringComponent>();
	public PlayerInventoryComponent InventoryComponent => Components.Get<PlayerInventoryComponent>();

	// Reference to the saving service
	private SavingServiceComponent? _savingService;

	protected override void OnStart()
	{
		_initialSpawnPoint = Transform.World;

		// Find the Saving Service (assuming one exists in the scene)
		// This might need adjustment based on how SavingServiceComponent is managed
		if (Networking.IsHost)
		{
			_savingService = Scene.GetAllComponents<SavingServiceComponent>().FirstOrDefault();
			if (_savingService == null)
			{
				Log.Warning("[Player] SavingServiceComponent not found in the scene!");
			}
			else
			{
				// Load data for this player when they start/connect (only on host)
				_ = LoadAndApplyDataAsync();
			}
		}

		var gatheringComp = Components.GetOrCreate<PlayerGatheringComponent>();
		if (gatheringComp != null) gatheringComp.OwnerPlayer = this;
		var interactionComp = Components.GetOrCreate<PlayerInteractionComponent>();
		if (interactionComp != null) interactionComp.OwnerPlayer = this;
		var inventoryComp = Components.GetOrCreate<PlayerInventoryComponent>();
		if (inventoryComp != null) inventoryComp.OwnerPlayer = this;
		
		Inventory[ResourceType.Wood] = 20f; // Initialize with 20 wood

		if ( Body.IsValid() && BodyRenderer == null ) BodyRenderer = Body.Components.Get<SkinnedModelRenderer>();
		if ( BodyRenderer != null && Body.IsValid() ) ClothingContainer.CreateFromLocalUser().Apply( BodyRenderer );
	}

	private TimeSince _timeSinceLastSave = 0f; // Timer for auto-save

	protected override void OnUpdate()
	{

		// Still calculate WishVelocity and call UpdateAnimationParameters locally
		if ( !IsProxy )
		{
			var moveInput = Input.AnalogMove;
			// !! Replace with your actual input/movement logic to calculate wish velocity !!
			WishVelocity = (Eye.Transform.Rotation.Forward * moveInput.x + Eye.Transform.Rotation.Left * moveInput.y).Normal * 200.0f;
			WishVelocity = WishVelocity.WithZ( 0 );

			UpdateAnimationParameters(); // Call the method now defined in Player.Animations.cs
		}

		if ( IsProxy ) return;

		// --- Calculate Current Attack Speed ---
		float currentAttackRate = BaseAttackRate;
		if ( EquippedTool != null )
		{
			// Higher speed multiplier = faster attacks (shorter delay)
			currentAttackRate /= EquippedTool.GetGatherSpeedMultiplier();
			// Ensure minimum attack rate
			currentAttackRate = MathF.Max( 0.2f, currentAttackRate ); // Minimum 0.2s delay
		}

		// --- Input Handling ---
		// Use calculated attack rate for cooldown check
		if ( Input.Pressed( "Attack1" ) && _timeSinceLastAttack > currentAttackRate )
		{
			_timeSinceLastAttack = 0f;

			// Play swing sound etc.
			if ( SwingSound != null ) Sound.Play( SwingSound );
			TriggerAttackAnimation();
			_timeSinceLastAttack = 0f; // Reset timer

			TriggerAttackAnimation(); // Trigger animation locally first

			// Request the action from the server (which performs the raycast)
			Interaction?.RequestPrimaryAction();
		}

		if ( Input.Pressed( "Attack2" ) ) // Secondary might have its own cooldown or logic
		{
			// TriggerAttackAnimation( isSecondary: true ); // If secondary has animation
			Interaction?.RequestSecondaryAction();
		}

		if ( Input.Pressed( "F10" ) && Network.IsOwner )
		{
			Log.Info( "[Player] F10 pressed, requesting manual save..." );
			RequestManualSave();
		}

		if ( Networking.IsHost )
		{
			if ( _timeSinceLastSave >= 60.0f )
			{
				if ( _savingService != null ) _ = _savingService.SaveDataAsync( this );
				else Log.Warning( "[Player] Auto-save triggered, but SavingService is not available." );
				_timeSinceLastSave = 0f;
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		// Keep your movement logic here (or move it to another partial class if desired)
		if ( !IsProxy )
		{
				if ( Controller != null )
				{
					// !! Replace with your actual movement code !!
					Controller.Accelerate( WishVelocity );
					Controller.ApplyFriction( 4.0f );
					Controller.Move();
					if ( !Controller.IsOnGround ) Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
				}
		}
	}


	protected override void OnDestroy()
	{
		// Attempt to save data when the player object is destroyed (e.g., player leaves)
		if (Networking.IsHost && _savingService != null && Network.Owner != null)
		{
			Log.Info($"[Player] OnDestroy: Triggering final save for {Network.Owner.SteamId}");
			_ = _savingService.SaveDataAsync(this);
		}
		base.OnDestroy();
	}

	public void GatherResource(ResourceType type, double resourceDifficulty, float amount)
	{
		if (type == ResourceType.None) return; // Don't add None to inventory

		// Use the enhanced inventory component if available
		var inventoryComp = InventoryComponent;
		if (inventoryComp != null)
		{
			float actualAmount = inventoryComp.AddResource(type, amount);
			if (actualAmount < amount)
			{
				Log.Info($"[Player] Inventory full! Only added {actualAmount:F2} of {amount:F2} {type}");
			}
			
			if (actualAmount > 0)
			{
				var experience = actualAmount * 3 * resourceDifficulty;
				AddExperience(experience);
			}
		}
		else
		{
			// Fallback to old method
			Inventory.TryGetValue(type, out var currentAmount);
			Inventory[type] = currentAmount + amount;
			Log.Info($"Gathered {amount:F2} {type}. Total: {Inventory[type]:F2}");
			ResourceManager.Instance?.UpdateInventory(Inventory);
			var experience = amount * 3 * resourceDifficulty;
			AddExperience(experience);
		}
	}


	public void AddExperience(double amount)
	{
		amount *= new Random().NextInt64(0, 2);
		Experience += amount; // Randomize XP gain slightly
		Log.Info($"Gained {amount:F2} XP. Total: {Experience:F2}");

		bool leveledUp = false;
		while (Experience >= ExperienceToNextLevel)
		{
			Experience -= ExperienceToNextLevel;
			Level++;
			leveledUp = true; // Mark that a level up occurred
			Log.Info($"Leveled up to Level {Level}!");
		}

		// Save data if a level up occurred
		if (leveledUp && _savingService != null)
		{
			Log.Info($"[Player] Saving data after level up to {Level}.");
			_ = _savingService.SaveDataAsync(this);
		}
	}

	[Rpc.Broadcast(NetFlags.Reliable)]
	public void SellResources()
	{
		if (Networking.IsClient) return; // Ensure server execution

		if (Inventory.Count == 0)
		{
			Log.Info("Inventory empty, nothing to sell.");
			return;
		}

		float totalValue = 0f;
		float totalExpGain = 0f;

		var itemsToSell = Inventory.ToList();
		Inventory.Clear();

		foreach (var kvp in itemsToSell)
		{
			float valuePerUnit = kvp.Key switch
			{
				ResourceType.Wood => 0.5f,
				ResourceType.Stone => 0.8f,
				ResourceType.Fiber => 0.3f,
				ResourceType.CopperOre => 1.5f,
				ResourceType.TinOre => 1.8f,
				ResourceType.IronOre => 3.0f,
				ResourceType.Coal => 1.0f,
				_ => 0.1f // Default small value
			};
			totalValue += kvp.Value * valuePerUnit; // kvp.Value is float
			totalExpGain += kvp.Value * 0.2f; // Example XP for selling (float)
		}

		Money += totalValue;
		AddExperience(totalExpGain); // AddExperience might trigger a save if level up occurs

		Log.Info($"Sold resources for ${totalValue:F2}. Gained {totalExpGain:F2} EXP. Current Money: {Money:F2}");

		ResourceManager.Instance?.UpdateInventory(Inventory);

		// Save data after selling completes (regardless of level up)
		if (_savingService != null)
		{
			Log.Info("[Player] Saving data after selling resources.");
			_ = _savingService.SaveDataAsync(this);
		}
	}

	[Rpc.Broadcast(NetFlags.Reliable)]
	public void BuyMoneyUpgrade(string upgradeId)
	{
		if (Networking.IsClient) return;

		Log.Info($"Server: Received request to buy Money Upgrade: {upgradeId}");
	}

	[Rpc.Broadcast(NetFlags.Reliable)]
	public void BuyPrestigeUpgrade(string upgradeId)
	{
		if (Networking.IsClient) return;

		Log.Info($"Server: Received request to buy Prestige Upgrade: {upgradeId}");
	}

	/// <summary>
	/// Gets the profession level for a specific tool type.
	/// For now, this is simplified to use the overall player level.
	/// In the future, this could track individual profession levels.
	/// </summary>
	/// <param name="toolType">The ResourceType representing the profession</param>
	/// <returns>The profession level for this tool type</returns>
	public int GetProfessionLevel(ResourceType toolType)
	{
		// Simple implementation: use overall player level as profession level
		// In a more complex system, you could have separate profession levels
		// stored in a Dictionary<ResourceType, int> ProfessionLevels property
		return Level;
	}

	/// <summary>
	/// Gets the maximum profession level possible in the game.
	/// </summary>
	/// <returns>The maximum profession level</returns>
	public int GetMaxProfessionLevel()
	{
		// Define the maximum profession level
		return 100; // Example maximum level
	}

	[Rpc.Broadcast(NetFlags.Reliable)]
	public void Prestige()
	{
		if (Networking.IsClient) return;

		Log.Info("Server: Received request to Prestige.");
	}

	/// <summary>
	/// Resets the player's core stats and clears inventory/tools/professions in memory.
	/// Called server-side before saving the reset state.
	/// </summary>
	public void ResetStatsInMemory()
	{
		if (Networking.IsClient) return;

		Log.Info($"[Player {GameObject.Name}] Resetting stats in memory...");

		// Reset Core Stats
		Money = 0f;
		Level = 1; // Reset to level 1
		Experience = 0f;
		PrestigePoints = 0;

		// Clear Collections
		Inventory?.Clear();

		// Clear Profession Component Data

		// Trigger UI update for local player after reset
		ResourceManager.Instance?.UpdateInventory(Inventory);

		Log.Info($"[Player {GameObject.Name}] In-memory stats reset complete.");
	}


	/// <summary>
	/// Loads player data from the SavingService and applies it. Called by the host.
	/// </summary>
	private async Task LoadAndApplyDataAsync()
	{
		if (!Networking.IsHost || _savingService == null || Network.Owner == null) return;

		long steamId = Network.Owner.SteamId;
		Log.Info($"[Player] Attempting to load data for SteamID: {steamId}");

		try
		{
			var loadedData = await _savingService.LoadDataAsync(steamId);
			if (loadedData != null)
			{
				if (String.IsNullOrEmpty(loadedData.SteamId))
				{
					Log.Warning($"[Player] Loaded data for {steamId} could not be found");
					return;
				}
				Log.Info($"[Player] Data loaded successfully for {steamId}. Applying...");
				// Use the static method from SavingServiceComponent to apply data
				SavingServiceComponent.ApplyLoadedData(this, loadedData);
				Log.Info($"[Player] Data applied for {steamId}.");
				// Optional: Force UI update if needed after loading
				ResourceManager.Instance?.UpdateInventory(Inventory);
			}
			else
			{
				Log.Info($"[Player] No existing data found for {steamId} or load failed. Starting fresh.");
				// Optionally initialize default values if needed, though properties already have defaults.
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, $"[Player] Exception during LoadAndApplyDataAsync for {steamId}");
		}
	}

	/// <summary>
	/// RPC called by the client owner to request the server (host) to save their data.
	/// </summary>
	[Rpc.Broadcast(NetFlags.HostOnly)]
	private void RequestManualSave()
	{
		if (!Networking.IsHost) return; // Should already be host due to RpcTarget, but double-check

		if (_savingService != null)
		{
			Log.Info($"[Player] Host received manual save request from {Network.OwnerConnection?.SteamId}. Saving...");
			_ = _savingService.SaveDataAsync(this);
		}
		else
		{
			Log.Warning("[Player] Host received manual save request, but SavingService is not available.");
		}
	}
}
