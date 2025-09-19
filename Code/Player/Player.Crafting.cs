// File: Code/Player/Player.Crafting.cs (Additions/Modifications)
#nullable enable
using Sandbox;
using Sandbox.Citizen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ResourceFarmer.Resources;
using ResourceFarmer.Crafting;
using ResourceFarmer.Items;
using ResourceFarmer.SavingService;
using Sandbox.Services;

// --- Player Class Modifications ---
namespace ResourceFarmer.PlayerBase;

public sealed partial class Player : Component
{
	// ... (Existing properties: EquippedTool, Inventory, Level etc.) ...

	// --- Upgrade Logic ---

	/// <summary>
	/// Calculates the resource cost to upgrade the currently equipped tool.
	/// Returns null if no tool equipped or cannot be upgraded further.
	/// </summary>
	public Dictionary<ResourceType, float> GetCurrentToolUpgradeCost()
	{
		if ( EquippedTool == null ) return null;

		// --- Define Upgrade Cost Formula ---
		// Example: Cost scales with Level, varies by Material base cost.
		// This needs careful balancing!
		int nextLevel = EquippedTool.Level + 1;
		float levelMultiplier = MathF.Pow( 1.8f, EquippedTool.Level ); // Exponential cost increase
		var costs = new Dictionary<ResourceType, float>();

		// Base cost based on material (Add more materials)
		switch ( EquippedTool.Material?.ToLowerInvariant() )
		{
			case "wood":
				costs.Add( ResourceType.Wood, 15f * levelMultiplier );
				costs.Add( ResourceType.Fiber, 5f * levelMultiplier );
				break;
			case "stone":
				costs.Add( ResourceType.Stone, 25f * levelMultiplier );
				costs.Add( ResourceType.Wood, 5f * levelMultiplier );
				break;
			case "copper":
				costs.Add( ResourceType.CopperOre, 20f * levelMultiplier ); // Assuming Ore is used directly for upgrades
				costs.Add( ResourceType.Coal, 10f * levelMultiplier );
				break;
			case "iron":
				costs.Add( ResourceType.IronOre, 25f * levelMultiplier );
				costs.Add( ResourceType.Coal, 15f * levelMultiplier );
				break;
			// Add costs for Tin, Silver, Gold, Mithril, Adamantite etc.
			default:
				// Maybe cannot upgrade unknown materials? Or use a default?
				// Returning null indicates cannot upgrade this material/level maybe.
				return null;
		}

		// Maybe add a small cost based on Quality or number of Bonuses?
		float qualityCostFactor = 1.0f + EquippedTool.Quality; // Higher quality costs slightly more?
		foreach ( var key in costs.Keys.ToList() )
		{
			costs[key] *= qualityCostFactor;
		}

		// TODO: Check against a Max Level cap? If Level >= MaxLevel, return null.

		return costs;
	}

	/// <summary>
	/// Checks if the player can afford the upgrade cost for the equipped tool.
	/// </summary>
	public bool CanAffordUpgrade()
	{
		var costs = GetCurrentToolUpgradeCost();
		if ( costs == null ) return false; // Cannot upgrade or no cost defined

		foreach ( var cost in costs )
		{
			if ( !Inventory.TryGetValue( cost.Key, out var currentAmount ) || currentAmount < cost.Value )
			{
				return false; // Not enough of this resource
			}
		}
		return true; // Can afford all costs
	}

	/// <summary>
	/// RPC called by the client to request upgrading the equipped tool. Runs on Host/Server.
	/// </summary>
	[Rpc.Broadcast(NetFlags.Reliable )]
	public void RequestUpgradeEquippedTool()
	{
		if ( EquippedTool == null )
		{
			Log.Warning( $"[Player Upgrade Host] Request failed: No tool equipped." );
			// NotifyClientUpgradeResult(false, "No tool equipped."); // Optional feedback
			return;
		}

		var costs = GetCurrentToolUpgradeCost();
		if ( costs == null )
		{
			Log.Warning( $"[Player Upgrade Host] Request failed: Tool cannot be upgraded further or cost not defined." );
			// NotifyClientUpgradeResult(false, "Cannot upgrade further.");
			return;
		}

		// Verify affordability server-side
		foreach ( var cost in costs )
		{
			if ( !Inventory.TryGetValue( cost.Key, out var currentAmount ) || currentAmount < cost.Value )
			{
				Log.Warning( $"[Player Upgrade Host] Upgrade failed. Not enough {cost.Key}. Need {cost.Value:F1}, Have {currentAmount:F1}" );
				// NotifyClientUpgradeResult(false, $"Not enough {cost.Key}.");
				return;
			}
		}

		// --- Affordability confirmed: Deduct resources ---
		foreach ( var cost in costs )
		{
			Inventory[cost.Key] -= cost.Value;
			if ( Inventory[cost.Key] <= 0.01f ) Inventory.Remove( cost.Key );
		}
		Log.Info( $"[Player Upgrade Host] Deducted resources for upgrading {EquippedTool.Material} {EquippedTool.ToolType}." );


		// --- Apply Upgrade ---
		int previousLevel = EquippedTool.Level;
		float previousQuality = EquippedTool.Quality;
		var previousBonuses = EquippedTool.AppliedBonuses?.ToList() ?? new List<AppliedBonusInstance>(); // Copy existing bonuses

		int newLevel = previousLevel + 1;
		float qualityIncrease = Game.Random.Float( 0.01f, 0.05f ); // Small random quality bump on upgrade
		float newQuality = Math.Clamp( previousQuality + qualityIncrease, 0f, 1f );

		// Chance to add a *new* bonus (if max not reached)
		// TODO: Define max bonuses per tool?
		int maxBonusSlots = 3; // Example max
		float addBonusChance = 0.1f + (previousQuality * 0.1f); // Higher quality = slightly higher chance? Example: 10-20%
		List<AppliedBonusInstance> finalBonuses = previousBonuses; // Start with old bonuses
		if ( previousBonuses.Count < maxBonusSlots && Game.Random.Float( 0f, 1f ) < addBonusChance )
		{
			var newBonusList = ToolBonusRegistry.GetRandomBonuses( 1, 1 );
			if ( newBonusList.Any() && !previousBonuses.Any( b => b.Name == newBonusList[0].Name ) ) // Ensure it's actually new
			{
				finalBonuses.Add( newBonusList[0] );
				Log.Info( $"[Player Upgrade Host] Added new bonus: {newBonusList[0].Name}" );
			}
		}

		// --- Create NEW Tool Instance ---
		// This is important for [Sync] properties to update correctly
		var upgradedTool = new ToolBase(
			EquippedTool.ToolType,
			EquippedTool.Material,
			newLevel,
			newQuality,
			finalBonuses // Assign potentially updated bonus list
		);

		// Assign the new instance
		EquippedTool = upgradedTool;

		Log.Info( $"[Player Upgrade Host] Upgraded Tool to Lvl:{newLevel} Q:{newQuality:P1} Bonuses:[{string.Join( ",", finalBonuses.Select( b => b.Name ) )}]" );

		// Grant XP for upgrading?
		AddExperience( newLevel * 10 ); // Example XP

		// Save game
		if ( _savingService != null ) _ = _savingService.SaveDataAsync( this );

		// TODO: Notify client of success
		// NotifyClientUpgradeResult(true, "Upgrade successful!");
	}


	// --- Bonus Manipulation (Placeholder - High Cost) ---

	public Dictionary<ResourceType, float> GetAddBonusCost()
	{
		// Define high cost for adding a bonus (e.g., rare materials)
		return new Dictionary<ResourceType, float> { { ResourceType.CrystalShard, 5 }, { ResourceType.GoldOre, 10 } }; // Example cost
	}
	public Dictionary<ResourceType, float> GetRemoveBonusCost( AppliedBonusInstance bonusToRemove )
	{
		// Define high cost for removing a bonus (maybe based on tier/magnitude?)
		return new Dictionary<ResourceType, float> { { ResourceType.EssenceDust, 20 } }; // Example cost
	}

	[Rpc.Broadcast(NetFlags.Reliable)]
	public void RequestAddBonus()
	{
		Log.Info( $"[Player Bonus Host] Received RequestAddBonus" );
		// TODO: Implement cost check (GetAddBonusCost), deduction, max bonus check, add bonus logic (similar to upgrade), assign NEW ToolBase instance.
	}

	[Rpc.Broadcast(NetFlags.Reliable)]
	public void RequestRemoveBonus( ToolBonusName bonusName ) // Pass Name to identify
	{
		Log.Info( $"[Player Bonus Host] Received RequestRemoveBonus for {bonusName}" );
		// TODO: Implement cost check (GetRemoveBonusCost), deduction, find bonus by Name in list, create NEW ToolBase instance *without* that bonus, assign instance.
	}

	// Add RPC for crafting request
	[Rpc.Broadcast( NetFlags.Reliable )]
	public void RequestCraftItem( CraftingRecipeResource recipe )
	{
		if ( Networking.IsClient ) return; // Server-side only

		if ( recipe == null )
		{
			Log.Warning( "[Player Host] Crafting request received, but recipe is null." );
			// NotifyClientCraftResult(false, "Recipe not found."); // Optional: Send feedback
			return;
		}

		Log.Info( $"[Player Host] Received craft request for: {recipe.Name}" );

		// --- Check Requirements (Resources, Profession Level) ---
		if ( recipe.Costs == null ) { /* Log Error */ return; }
		foreach ( var cost in recipe.Costs )
		{
			if ( !Inventory.TryGetValue( cost.Key, out var currentAmount ) || currentAmount < cost.Value )
			{
				Log.Warning( $"[Player Host] Crafting {recipe.Name} failed. Not enough {cost.Key}." );
				return; // Not enough resources
			}
		}

		int playerProfessionLevel = GetProfessionLevel( recipe.ToolType ); // Use placeholder/real method
		if ( playerProfessionLevel < recipe.RequiredProfessionLevel )
		{
			Log.Warning( $"[Player Host] Crafting {recipe.Name} failed. Required Level {recipe.RequiredProfessionLevel}, Player Level {playerProfessionLevel}" );
			return; // Level too low
		}

		// --- Requirements met: Deduct resources ---
		foreach ( var cost in recipe.Costs )
		{
			Inventory[cost.Key] -= cost.Value;
			if ( Inventory[cost.Key] <= 0.01f ) Inventory.Remove( cost.Key );
		}
		Log.Info( $"[Player Host] Deducted resources for {recipe.Name}." );


		// --- Calculate Final Quality ---
		float baseQuality = recipe.OutputQuality;
		int maxProfessionLevel = GetMaxProfessionLevel(); // Get max level for calculation

		// Calculate skill influence factor (0.0 at level 0, approaches 1.0 at max level)
		// Clamp level to avoid issues if it somehow exceeds max
		float skillRatio = Math.Clamp( (float)playerProfessionLevel / maxProfessionLevel, 0f, 1f );

		// Define base variance range (e.g., +/- 15%)
		float baseVarianceRange = 0.15f;

		// Higher skill reduces the *potential negative* variance and slightly increases positive potential
		// Max negative variance goes from -baseVarianceRange towards 0 as skill increases
		float maxNegativeVariance = -baseVarianceRange * (1f - skillRatio);
		// Max positive variance goes from +baseVarianceRange towards baseVarianceRange*1.5 (example) as skill increases
		float maxPositiveVariance = baseVarianceRange * (1f + skillRatio * 0.5f);

		// Generate random variance within the skill-adjusted range
		float qualityVariance = Game.Random.Float( maxNegativeVariance, maxPositiveVariance );

		// Apply variance and clamp the result (e.g., between 5% and 100%)
		float finalQuality = Math.Clamp( baseQuality + qualityVariance, 0.05f, 1.0f );

		Log.Info( $"[Player Host] Quality Calc for {recipe.Name}: Base={baseQuality:P1}, SkillRatio={skillRatio:P1}, Variance={qualityVariance:P2}, Final={finalQuality:P1}" );


		// --- Determine Bonuses ---
		List<AppliedBonusInstance> appliedBonuses = null; // Use AppliedBonusInstance
		if ( recipe.CanHaveBonuses )
		{
			appliedBonuses = ToolBonusRegistry.GetRandomBonuses( recipe.MinBonuses, recipe.MaxBonuses ); // Get list of instances
			if ( appliedBonuses?.Any() == true )
			{
				Log.Info( $"[Player Host] Generated Bonuses for {recipe.Name}: {string.Join( ", ", appliedBonuses.Select( b => $"{b.Name}({b.ActualMagnitude:F3})" ) )}" );
			}
		}

		// --- Create the Tool ---
		var craftedTool = new ToolBase(
			recipe.ToolType,
			recipe.Material,
			recipe.Level,
			finalQuality, // Use calculated final quality
			appliedBonuses // Assign list of AppliedBonusInstance
		);
		Log.Info( $"[Player Host] Created Tool: {craftedTool.Material} {craftedTool.ToolType} Lvl:{craftedTool.Level} Q:{craftedTool.Quality:P1} Bonuses:[{string.Join( ",", craftedTool.AppliedBonuses?.Select( b => b.Name ) ?? new List<ToolBonusName>() )}]" );
		// --- Equip & Finalize ---
		EquippedTool = craftedTool;
		AddExperience( (double)recipe.ToolType * 5 + recipe.Level * 2 ); // Grant XP

		if ( _savingService != null ) _ = _savingService.SaveDataAsync( this ); // Save

		// TODO: Notify client UI of success/update
	}



	// ... (Keep other methods: RequestCraftItem, GetProfessionLevel, Saving/Loading etc.) ...
}
