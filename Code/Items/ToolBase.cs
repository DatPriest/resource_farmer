// File: Code/ToolBase.cs (Updated)
using ResourceFarmer.Resources;
using ResourceFarmer.Items;
using System.Collections.Generic;
using System.Linq;
using Sandbox; // Required for NetworkTable if used
using System;
using System.Text; // For StringBuilder

// IMPORTANT: Syncing a List<struct> requires NetworkTable<T> or custom serialization.
// If EquippedTool is [Sync] on Player, changes *might* sync, but replacing the whole
// ToolBase object on change is often needed if it's not an Entity/NetworkComponent itself.
namespace ResourceFarmer.Items
{
	/// <summary>
	/// Base class for all tools in the game.
	/// </summary>
	public class ToolBase
	{
		public virtual ResourceType ToolType { get; set; }
		public virtual string Material { get; set; }
		public virtual int Level { get; set; }
		public virtual float Quality { get; set; } // Base Quality

		// Store list of AppliedBonusInstance
		// Use NetworkTable<AppliedBonusInstance> if this class is a NetworkComponent/Entity
		public virtual IList<AppliedBonusInstance> AppliedBonuses { get; set; } = new List<AppliedBonusInstance>();

		// Constructors
		public ToolBase( ResourceType toolType, string material, int level, float quality, List<AppliedBonusInstance> bonuses = null )
		{
			ToolType = toolType;
			Material = material;
			Level = level;
			Quality = Math.Clamp( quality, 0f, 1f );
			AppliedBonuses = bonuses ?? new List<AppliedBonusInstance>();
		}

		public ToolBase() { AppliedBonuses = new List<AppliedBonusInstance>(); } // Ensure init

		// --- Updated Calculation Methods ---

		public float GetGatherAmountMultiplier( ResourceType targetResourceType )
		{
			float baseMultiplier = 1.0f + Quality * 0.2f; // Base amount from quality
			float bonusMultiplier = 0f;

			if ( AppliedBonuses != null )
			{
				foreach ( var bonus in AppliedBonuses )
				{
					// Find the definitions matching this bonus NAME
					foreach ( var definition in ToolBonusRegistry.GetDefinitions( bonus.Name ) )
					{
						switch ( definition.Effect )
						{
							case ToolBonusEffect.GatherAmountMultiplier:
							case ToolBonusEffect.GatherAmountPenalty:
								bonusMultiplier += bonus.ActualMagnitude; // Use the rolled magnitude
								break;
							case ToolBonusEffect.SpecificResourceBoost:
								if ( definition.SpecificResource == targetResourceType ) bonusMultiplier += bonus.ActualMagnitude;
								break;
							case ToolBonusEffect.SpecificResourcePenalty:
								if ( definition.SpecificResource == targetResourceType ) bonusMultiplier += bonus.ActualMagnitude;
								break;
						}
					}
				}
			}
			return MathF.Max( 0.1f, baseMultiplier + bonusMultiplier );
		}

		public float GetGatherSpeedMultiplier()
		{
			float baseMultiplier = 1.0f + Level * 0.05f; // Base speed from level
			float bonusMultiplier = 0f;

			if ( AppliedBonuses != null )
			{
				foreach ( var bonus in AppliedBonuses )
				{
					foreach ( var definition in ToolBonusRegistry.GetDefinitions( bonus.Name ) )
					{
						switch ( definition.Effect )
						{
							case ToolBonusEffect.GatherSpeedMultiplier:
							case ToolBonusEffect.GatherSpeedPenalty:
							case ToolBonusEffect.WeightIncrease: // Example: Weight affects speed negatively
																 // Need a mapping from weight magnitude to speed penalty magnitude if using this effect
								bonusMultiplier += bonus.ActualMagnitude;
								break;
						}
					}
				}
			}
			return MathF.Max( 0.1f, baseMultiplier + bonusMultiplier );
		}

		public float GetCritChance()
		{
			float baseCritChance = 0.02f + Quality * 0.03f; // Base crit from quality
			float bonusCritChance = 0f;

			if ( AppliedBonuses != null )
			{
				foreach ( var bonus in AppliedBonuses )
				{
					foreach ( var definition in ToolBonusRegistry.GetDefinitions( bonus.Name ) )
					{
						if ( definition.Effect == ToolBonusEffect.GatherCritChance )
						{
							bonusCritChance += bonus.ActualMagnitude; // Use rolled magnitude
						}
					}
				}
			}
			return Math.Clamp( baseCritChance + bonusCritChance, 0f, 1f );
		}

		public float GetCritMultiplier()
		{
			float baseMultiplier = 1.5f; // Base crit multi
			float bonusMultiplier = 0f;

			if ( AppliedBonuses != null )
			{
				foreach ( var bonus in AppliedBonuses )
				{
					foreach ( var definition in ToolBonusRegistry.GetDefinitions( bonus.Name ) )
					{
						if ( definition.Effect == ToolBonusEffect.GatherCritMultiplier )
						{
							bonusMultiplier += bonus.ActualMagnitude; // Use rolled magnitude
						}
					}
				}
			}
			return MathF.Max( 1.0f, baseMultiplier + bonusMultiplier );
		}

		// Add other property calculations if needed (e.g., GetDurabilityMultiplier)
	}

}
