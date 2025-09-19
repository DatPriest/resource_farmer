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
	/// Server command for upgrading equipped tool. Called from client via ConsoleSystem.Run.
	/// </summary>
	[ConCmd.Server("upgrade_tool")]
	public static void UpgradeEquippedTool()
	{
		var caller = ConsoleSystem.Caller;
		if ( caller?.Pawn is not Player player )
		{
			Log.Warning( "[UpgradeEquippedTool] Invalid caller or player not found." );
			return;
		}

		Log.Info( $"[UpgradeEquippedTool] Player {caller.DisplayName} requesting tool upgrade" );
		player.ProcessToolUpgrade();
	}

	/// <summary>
	/// Processes the actual tool upgrade request.
	/// </summary>
	public void ProcessToolUpgrade()
	{
		if ( EquippedTool == null )
		{
			Log.Warning( "[Player] Upgrade request failed: No tool equipped." );
			return;
		}

		var costs = GetCurrentToolUpgradeCost();
		if ( costs == null )
		{
			Log.Warning( "[Player] Upgrade request failed: Tool cannot be upgraded further or cost not defined." );
			return;
		}

		// Verify affordability server-side
		foreach ( var cost in costs )
		{
			if ( !Inventory.TryGetValue( cost.Key, out var currentAmount ) || currentAmount < cost.Value )
			{
				Log.Warning( $"[Player] Upgrade failed. Not enough {cost.Key}. Need {cost.Value:F1}, Have {currentAmount:F1}" );
				return;
			}
		}

		// --- Affordability confirmed: Deduct resources ---
		foreach ( var cost in costs )
		{
			Inventory[cost.Key] -= cost.Value;
			if ( Inventory[cost.Key] <= 0.01f ) Inventory.Remove( cost.Key );
		}
		Log.Info( $"[Player] Deducted resources for upgrading {EquippedTool.Material} {EquippedTool.ToolType}." );


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
				Log.Info( $"[Player] Added new bonus: {newBonusList[0].Name}" );
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

		Log.Info( $"[Player] Upgraded Tool to Lvl:{newLevel} Q:{newQuality:P1} Bonuses:[{string.Join( ",", finalBonuses.Select( b => b.Name ) )}]" );

		// --- Track Upgrade Achievement (NEW) ---
		TrackUpgradeAchievement( upgradedTool, costs );

		// Grant XP for upgrading?
		AddExperience( newLevel * 10 ); // Example XP

		// Save game
		if ( _savingService != null ) _ = _savingService.SaveDataAsync( this );

		// TODO: Notify client of success
		// NotifyClientUpgradeResult(true, "Upgrade successful!");
	}

	/// <summary>
	/// Tracks upgrade progress for achievements system.
	/// </summary>
	private void TrackUpgradeAchievement( ToolBase upgradedTool, Dictionary<ResourceType, float> costs )
	{
		if ( Networking.IsClient || upgradedTool == null || costs == null ) return; // Server-side only with null checks

		// Ensure collections are initialized
		UnlockedRecipes ??= new HashSet<string>();
		ItemsCraftedCount ??= new Dictionary<string, int>();
		MaterialsUsedCount ??= new Dictionary<string, int>();

		// Track this as a "virtual recipe" for upgrade achievements
		string upgradeRecipeName = $"Upgrade_{upgradedTool.Material}_{upgradedTool.ToolType}_to_Level_{upgradedTool.Level}";
		UnlockedRecipes.Add( upgradeRecipeName );

		// Increment upgrade count
		if ( ItemsCraftedCount.ContainsKey( upgradeRecipeName ) )
			ItemsCraftedCount[upgradeRecipeName]++;
		else
			ItemsCraftedCount[upgradeRecipeName] = 1;

		// Track materials used for upgrade
		foreach ( var cost in costs )
		{
			string materialKey = cost.Key.ToString();
			if ( MaterialsUsedCount.ContainsKey( materialKey ) )
				MaterialsUsedCount[materialKey] += (int)cost.Value;
			else
				MaterialsUsedCount[materialKey] = (int)cost.Value;
		}

		// Update last activity timestamp
		LastCraftingActivity = DateTime.UtcNow;

		Log.Info( $"[Player Upgrade] Achievement progress updated for upgrade to Level {upgradedTool.Level}" );

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

	[ConCmd.Server("add_tool_bonus")]
	public static void AddToolBonus()
	{
		var caller = ConsoleSystem.Caller;
		if ( caller?.Pawn is not Player player )
		{
			Log.Warning( "[AddToolBonus] Invalid caller or player not found." );
			return;
		}

		Log.Info( $"[AddToolBonus] Player {caller.DisplayName} requesting to add tool bonus" );
		player.ProcessAddBonus();
	}

	[ConCmd.Server("remove_tool_bonus")]
	public static void RemoveToolBonus( string bonusName )
	{
		var caller = ConsoleSystem.Caller;
		if ( caller?.Pawn is not Player player )
		{
			Log.Warning( "[RemoveToolBonus] Invalid caller or player not found." );
			return;
		}

		if ( !Enum.TryParse<ToolBonusName>( bonusName, true, out var parsedBonusName ) )
		{
			Log.Warning( $"[RemoveToolBonus] Invalid bonus name: {bonusName}" );
			return;
		}

		Log.Info( $"[RemoveToolBonus] Player {caller.DisplayName} requesting to remove bonus {bonusName}" );
		player.ProcessRemoveBonus( parsedBonusName );
	}

	public void ProcessAddBonus()
	{
		Log.Info( "[Player] Processing RequestAddBonus" );
		// TODO: Implement cost check (GetAddBonusCost), deduction, max bonus check, add bonus logic (similar to upgrade), assign NEW ToolBase instance.
	}

	public void ProcessRemoveBonus( ToolBonusName bonusName )
	{
		Log.Info( $"[Player] Processing RequestRemoveBonus for {bonusName}" );
		// TODO: Implement cost check (GetRemoveBonusCost), deduction, find bonus by Name in list, create NEW ToolBase instance *without* that bonus, assign instance.
	}

	/// <summary>
	/// Server command for crafting items. Called from client via ConsoleSystem.Run.
	/// </summary>
	/// <param name="toolType">The tool type to craft (e.g., "Axe")</param>
	/// <param name="material">The material name (e.g., "Wood")</param>
	/// <param name="level">The level of the item to craft</param>
	[ConCmd.Server("craft_item")]
	public static void CraftItem( string toolType, string material, int level )
	{
		var caller = ConsoleSystem.Caller;
		if ( caller?.Pawn is not Player player )
		{
			Log.Warning( "[CraftItem] Invalid caller or player not found." );
			return;
		}

		// Parse the tool type
		if ( !Enum.TryParse<ResourceType>( toolType, true, out var parsedToolType ) )
		{
			Log.Warning( $"[CraftItem] Invalid tool type: {toolType}" );
			return;
		}

		// Find the recipe
		var recipe = RecipeManager.Instance?.FindRecipe( parsedToolType, material, level );
		if ( recipe == null )
		{
			Log.Warning( $"[CraftItem] Recipe not found for {toolType} {material} Level {level}" );
			return;
		}

		Log.Info( $"[CraftItem] Player {caller.DisplayName} requesting craft: {recipe.Name}" );

		// Use the existing crafting logic
		player.ProcessCraftingRequest( recipe );
	}

	/// <summary>
	/// Processes the actual crafting request. Moved from RequestCraftItem for reuse.
	/// </summary>
	/// <param name="recipe">The recipe to craft</param>
	public void ProcessCraftingRequest( CraftingRecipeResource recipe )
	{
		if ( recipe == null )
		{
			Log.Warning( "[Player] Crafting request received, but recipe is null." );
			return;
		}

		Log.Info( $"[Player] Processing craft request for: {recipe.Name}" );

		// --- Check Requirements (Resources, Profession Level) ---
		if ( recipe.Costs == null ) 
		{ 
			Log.Error( $"[Player] Recipe {recipe.Name} has no costs defined." );
			return; 
		}
		foreach ( var cost in recipe.Costs )
		{
			if ( !Inventory.TryGetValue( cost.Key, out var currentAmount ) || currentAmount < cost.Value )
			{
				Log.Warning( $"[Player] Crafting {recipe.Name} failed. Not enough {cost.Key}." );
				return; // Not enough resources
			}
		}

		int playerProfessionLevel = GetProfessionLevel( recipe.ToolType );
		if ( playerProfessionLevel < recipe.RequiredProfessionLevel )
		{
			Log.Warning( $"[Player] Crafting {recipe.Name} failed. Required Level {recipe.RequiredProfessionLevel}, Player Level {playerProfessionLevel}" );
			return; // Level too low
		}

		// --- Requirements met: Deduct resources ---
		foreach ( var cost in recipe.Costs )
		{
			Inventory[cost.Key] -= cost.Value;
			if ( Inventory[cost.Key] <= 0.01f ) Inventory.Remove( cost.Key );
		}
		Log.Info( $"[Player] Deducted resources for {recipe.Name}." );


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

		Log.Info( $"[Player] Quality Calc for {recipe.Name}: Base={baseQuality:P1}, SkillRatio={skillRatio:P1}, Variance={qualityVariance:P2}, Final={finalQuality:P1}" );


		// --- Determine Bonuses ---
		List<AppliedBonusInstance> appliedBonuses = null; // Use AppliedBonusInstance
		if ( recipe.CanHaveBonuses )
		{
			appliedBonuses = ToolBonusRegistry.GetRandomBonuses( recipe.MinBonuses, recipe.MaxBonuses ); // Get list of instances
			if ( appliedBonuses?.Any() == true )
			{
				Log.Info( $"[Player] Generated Bonuses for {recipe.Name}: {string.Join( ", ", appliedBonuses.Select( b => $"{b.Name}({b.ActualMagnitude:F3})" ) )}" );
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
		
		// --- Track Crafting Progress (NEW) ---
		TrackCraftingAchievement( recipe );

		// --- Equip & Finalize ---
		EquippedTool = craftedTool;
		AddExperience( (double)recipe.ToolType * 5 + recipe.Level * 2 ); // Grant XP

		if ( _savingService != null ) _ = _savingService.SaveDataAsync( this ); // Save

		// TODO: Notify client UI of success/update
	}

	/// <summary>
	/// Tracks crafting progress for achievements system.
	/// </summary>
	private void TrackCraftingAchievement( CraftingRecipeResource recipe )
	{
		if ( Networking.IsClient || recipe == null ) return; // Server-side only with null check

		// Ensure collections are initialized
		UnlockedRecipes ??= new HashSet<string>();
		ItemsCraftedCount ??= new Dictionary<string, int>();
		MaterialsUsedCount ??= new Dictionary<string, int>();

		// Track recipe as unlocked
		UnlockedRecipes.Add( recipe.Name );

		// Increment crafted count for this recipe
		if ( ItemsCraftedCount.ContainsKey( recipe.Name ) )
			ItemsCraftedCount[recipe.Name]++;
		else
			ItemsCraftedCount[recipe.Name] = 1;

		// Track materials used
		if ( recipe.Costs != null )
		{
			foreach ( var cost in recipe.Costs )
			{
				string materialKey = cost.Key.ToString();
				if ( MaterialsUsedCount.ContainsKey( materialKey ) )
					MaterialsUsedCount[materialKey] += (int)cost.Value;
				else
					MaterialsUsedCount[materialKey] = (int)cost.Value;
			}
		}

		// Update last activity timestamp
		LastCraftingActivity = DateTime.UtcNow;

		Log.Info( $"[Player Crafting] Progress updated: {UnlockedRecipes.Count} recipes unlocked, {ItemsCraftedCount.Values.Sum()} total items crafted" );
	}



	// ... (Keep other methods: RequestCraftItem, GetProfessionLevel, Saving/Loading etc.) ...
}
