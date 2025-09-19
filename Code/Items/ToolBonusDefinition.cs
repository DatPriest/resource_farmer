// File: Code/Items/ToolBonusDefinition.cs (New File)
using ResourceFarmer.Resources; // For ResourceType if needed

namespace ResourceFarmer.Items
{
	public struct ToolBonusDefinition
	{
		public ToolBonusName Name { get; set; }
		public ToolBonusEffect Effect { get; set; }
		// public float Magnitude { get; set; } // Replaced by Min/Max
		public float MinMagnitude { get; set; } // Minimum possible roll (e.g., 0.05 for +5%)
		public float MaxMagnitude { get; set; } // Maximum possible roll (e.g., 0.15 for +15%)
		public bool IsPercentage { get; set; } // Is the magnitude typically displayed as a %?
		public bool IsPositiveEffect { get; set; } // Helps classify power tier
		public ResourceType SpecificResource { get; set; } // Used only if Effect is SpecificResourceBoost/Penalty
	}
}
