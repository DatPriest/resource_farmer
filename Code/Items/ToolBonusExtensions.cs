// File: Code/Items/ToolBonusExtensions.cs (New File)
using System;

namespace ResourceFarmer.Items
{
	public enum BonusTier
	{
		Neutral = 0,
		Positive_Low = 1,
		Positive_Medium = 2,
		Positive_High = 3,
		Negative_Low = -1,
		Negative_Medium = -2,
		Negative_High = -3
	}

	public static class ToolBonusExtensions
	{
		/// <summary>
		/// Classifies the 'power' or tier of an applied bonus based on its effect and magnitude.
		/// </summary>
		public static BonusTier GetPowerTier( this AppliedBonusInstance bonusInstance )
		{
			// Get the primary definition to understand the effect type
			var definition = ToolBonusRegistry.GetPrimaryDefinition( bonusInstance.Name );
			if ( definition == null ) return BonusTier.Neutral;

			bool isPositive = definition.Value.IsPositiveEffect; // Check if the effect is generally good
			float magnitude = bonusInstance.ActualMagnitude;
			float range = definition.Value.MaxMagnitude - definition.Value.MinMagnitude;
			float midPoint = definition.Value.MinMagnitude + range / 2f;

			// Simple thresholding - NEEDS ADJUSTMENT BASED ON YOUR BALANCE PREFERENCES
			// Compare the actual magnitude relative to the potential range midpoint
			float relativeMagnitude = range > 0.001f ? (magnitude - midPoint) / (range / 2f) : 0f; // Normalized -1 to +1 within range

			if ( isPositive )
			{
				if ( relativeMagnitude > 0.6f ) return BonusTier.Positive_High;    // Top 20% of positive range
				if ( relativeMagnitude > -0.2f ) return BonusTier.Positive_Medium; // Mid 60% of positive range
				if ( magnitude > 0.001f ) return BonusTier.Positive_Low;     // Lower 20% of positive range
			}
			else // Negative effect
			{
				// For negative effects, a magnitude closer to zero is "better" (less negative)
				// A magnitude closer to MinMagnitude is "worse" (more negative)
				if ( relativeMagnitude < -0.6f ) return BonusTier.Negative_High; // Bottom 20% of negative range (most negative)
				if ( relativeMagnitude < 0.2f ) return BonusTier.Negative_Medium;// Mid 60% of negative range
				if ( magnitude < -0.001f ) return BonusTier.Negative_Low;   // Top 20% of negative range (least negative)
			}

			return BonusTier.Neutral; // If magnitude is near zero or effect is neutral
		}

		/// <summary>
		/// Formats the bonus name and its magnitude for display.
		/// </summary>
		public static string GetFormattedDisplayName( this AppliedBonusInstance bonusInstance )
		{
			var definition = ToolBonusRegistry.GetPrimaryDefinition( bonusInstance.Name );
			string sign = bonusInstance.ActualMagnitude >= 0 ? "+" : "";

			if ( definition?.IsPercentage == true )
			{
				// Format as percentage (e.g., "+12.3%", "-5.0%")
				return $"{bonusInstance.Name} ({sign}{(bonusInstance.ActualMagnitude * 100f):F1}%)";
			}
			else
			{
				// Format as flat value (e.g., "+0.1", "-0.05")
				return $"{bonusInstance.Name} ({sign}{bonusInstance.ActualMagnitude:F2})";
			}
		}
	}
}
