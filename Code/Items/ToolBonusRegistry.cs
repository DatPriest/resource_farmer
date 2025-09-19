// File: Code/Items/ToolBonusRegistry.cs (Updated)
using System;
using System.Collections.Generic;
using System.Linq;
using ResourceFarmer.Resources;
using Sandbox; // For Game.Random

namespace ResourceFarmer.Items
{
	[Obsolete( "Its functionality is replaced by BonusManager" )]
	public static class ToolBonusRegistry
	{
		// Update definitions with Min/Max, IsPercentage, IsPositiveEffect
		public static readonly List<ToolBonusDefinition> AllBonuses = new()
		{
			// Positive Examples
			new ToolBonusDefinition { Name = ToolBonusName.Sturdy, Effect = ToolBonusEffect.DurabilityBonus, MinMagnitude = 0.05f, MaxMagnitude = 0.20f, IsPercentage = true, IsPositiveEffect = true },
			new ToolBonusDefinition { Name = ToolBonusName.Keen, Effect = ToolBonusEffect.GatherCritChance, MinMagnitude = 0.01f, MaxMagnitude = 0.05f, IsPercentage = true, IsPositiveEffect = true },
			new ToolBonusDefinition { Name = ToolBonusName.Efficient, Effect = ToolBonusEffect.GatherSpeedMultiplier, MinMagnitude = 0.05f, MaxMagnitude = 0.15f, IsPercentage = true, IsPositiveEffect = true },
			new ToolBonusDefinition { Name = ToolBonusName.Resourceful, Effect = ToolBonusEffect.GatherAmountMultiplier, MinMagnitude = 0.05f, MaxMagnitude = 0.15f, IsPercentage = true, IsPositiveEffect = true },
			new ToolBonusDefinition { Name = ToolBonusName.Swift, Effect = ToolBonusEffect.GatherSpeedMultiplier, MinMagnitude = 0.10f, MaxMagnitude = 0.25f, IsPercentage = true, IsPositiveEffect = true },
			new ToolBonusDefinition { Name = ToolBonusName.MoonTouched, Effect = ToolBonusEffect.GatherCritMultiplier, MinMagnitude = 0.10f, MaxMagnitude = 0.40f, IsPercentage = true, IsPositiveEffect = true }, // Crit Multiplier boost
            new ToolBonusDefinition { Name = ToolBonusName.Blessed, Effect = ToolBonusEffect.QualityBonus, MinMagnitude = 0.05f, MaxMagnitude = 0.15f, IsPercentage = false, IsPositiveEffect = true }, // Flat Quality bonus

			// Negative Examples
            new ToolBonusDefinition { Name = ToolBonusName.Heavy, Effect = ToolBonusEffect.GatherSpeedPenalty, MinMagnitude = -0.15f, MaxMagnitude = -0.05f, IsPercentage = true, IsPositiveEffect = false },
			new ToolBonusDefinition { Name = ToolBonusName.Dull, Effect = ToolBonusEffect.GatherAmountPenalty, MinMagnitude = -0.15f, MaxMagnitude = -0.05f, IsPercentage = true, IsPositiveEffect = false },
			new ToolBonusDefinition { Name = ToolBonusName.Clumsy, Effect = ToolBonusEffect.GatherCritChance, MinMagnitude = -0.04f, MaxMagnitude = -0.01f, IsPercentage = true, IsPositiveEffect = false },
			new ToolBonusDefinition { Name = ToolBonusName.Brittle, Effect = ToolBonusEffect.DurabilityPenalty, MinMagnitude = -0.30f, MaxMagnitude = -0.10f, IsPercentage = true, IsPositiveEffect = false },
			new ToolBonusDefinition { Name = ToolBonusName.Cracked, Effect = ToolBonusEffect.DurabilityPenalty, MinMagnitude = -0.20f, MaxMagnitude = -0.05f, IsPercentage = true, IsPositiveEffect = false },
			new ToolBonusDefinition { Name = ToolBonusName.Corrupted, Effect = ToolBonusEffect.QualityPenalty, MinMagnitude = -0.20f, MaxMagnitude = -0.05f, IsPercentage = false, IsPositiveEffect = false }, // Flat Quality penalty
            new ToolBonusDefinition { Name = ToolBonusName.Weighted, Effect = ToolBonusEffect.GatherSpeedPenalty, MinMagnitude = -0.10f, MaxMagnitude = -0.03f, IsPercentage = true, IsPositiveEffect = false },

            // Mixed/Neutral Examples (Can have multiple effects per name if needed)
            new ToolBonusDefinition { Name = ToolBonusName.Ancient, Effect = ToolBonusEffect.GatherAmountMultiplier, MinMagnitude = 0.05f, MaxMagnitude = 0.10f, IsPercentage = true, IsPositiveEffect = true },
			new ToolBonusDefinition { Name = ToolBonusName.Ancient, Effect = ToolBonusEffect.DurabilityPenalty, MinMagnitude = -0.15f, MaxMagnitude = -0.05f, IsPercentage = true, IsPositiveEffect = false },
			new ToolBonusDefinition { Name = ToolBonusName.Eldritch, Effect = ToolBonusEffect.GatherCritChance, MinMagnitude = 0.02f, MaxMagnitude = 0.06f, IsPercentage = true, IsPositiveEffect = true },
			new ToolBonusDefinition { Name = ToolBonusName.Eldritch, Effect = ToolBonusEffect.IncreasedStaminaCost, MinMagnitude = 0.1f, MaxMagnitude = 0.25f, IsPercentage = true, IsPositiveEffect = false }, // If stamina exists

            // ... Add more definitions to reach ~20 unique Names ...
		};

		// Returns list of definitions for a given name (useful if a name has multiple effects)
		public static IEnumerable<ToolBonusDefinition> GetDefinitions( ToolBonusName name )
		{
			return AllBonuses.Where( b => b.Name == name );
		}

		public static IEnumerable<ToolBonusDefinition> GetDefinitions( string name )
		{
			return AllBonuses.Where( b => b.Name.ToString() == name );
		}

		public static ToolBonusDefinition? GetDefinition( ToolBonusName name )
		{
			return AllBonuses.FirstOrDefault( b => b.Name == name );
		}

		public static ToolBonusDefinition? GetEffectFromName( ToolBonusEffect effect )
		{
			return AllBonuses.FirstOrDefault( b => b.Effect == effect );
		}

		// Get a single representative definition (e.g., for display formatting)
		public static ToolBonusDefinition? GetPrimaryDefinition( ToolBonusName name )
		{
			return AllBonuses.FirstOrDefault( b => b.Name == name );
		}


		// Updated method to return AppliedBonusInstance with rolled magnitudes
		public static List<AppliedBonusInstance> GetRandomBonuses( int minCount = 1, int maxCount = 3 )
		{
			var selectedBonuses = new List<AppliedBonusInstance>();
			var availableBonusNames = AllBonuses.Select( b => b.Name ).Distinct().Where( n => n != ToolBonusName.None ).ToList();

			if ( !availableBonusNames.Any() ) return selectedBonuses; // No bonuses defined

			int numBonuses = Game.Random.Int( minCount, maxCount );

			for ( int i = 0; i < numBonuses && availableBonusNames.Any(); i++ )
			{
				int randomIndex = Game.Random.Int( 0, availableBonusNames.Count - 1 );
				var chosenName = availableBonusNames[randomIndex];

				// Find ALL definitions for this name to roll magnitude correctly
				// (We only store one instance per name, but use definition range)
				var definitionsForName = GetDefinitions( chosenName ).ToList();
				if ( !definitionsForName.Any() ) continue; // Should not happen if availableBonusNames is correct

				// For simplicity, use the range from the *first* definition found for this name
				// More complex logic could average ranges or pick one based on effect type
				var primaryDef = definitionsForName.First();
				float rolledMagnitude = Game.Random.Float( primaryDef.MinMagnitude, primaryDef.MaxMagnitude );

				selectedBonuses.Add( new AppliedBonusInstance { Name = chosenName, ActualMagnitude = rolledMagnitude } );

				// Remove the chosen name to prevent duplicates in one roll
				availableBonusNames.RemoveAt( randomIndex );
			}
			return selectedBonuses;
		}
	}
}
