// File: Code/Items/ToolBonusEffect.cs (New File)
namespace ResourceFarmer.Items
{
	/// <summary>
	/// Defines the actual statistical effect a bonus provides.
	/// </summary>
	public enum ToolBonusEffect
	{
		None,
		// Positive Effects
		GatherSpeedMultiplier,    // Increases speed (reduces time or increases hits/sec)
		GatherAmountMultiplier,   // Increases resources gathered per hit/cycle
		GatherCritChance,       // Chance to gather extra resources
		GatherCritMultiplier,   // Multiplier for critical gathers
		DurabilityBonus,        // Increases tool lifespan (if durability exists)
		QualityBonus,           // Flat bonus to effective quality
		LevelBonus,             // Flat bonus to effective level
		WeightReduction,        // Reduces tool weight (if weight matters)
		ReducedStaminaCost,     // Reduces stamina cost per swing (if stamina exists)
		SpecificResourceBoost,  // Bonus effectiveness vs a specific ResourceType
								// Negative Effects
		GatherSpeedPenalty,       // Decreases speed
		GatherAmountPenalty,      // Decreases amount gathered
		DurabilityPenalty,        // Decreases tool lifespan
		QualityPenalty,         // Flat penalty to effective quality
		LevelPenalty,           // Flat penalty to effective level
		WeightIncrease,         // Increases tool weight
		IncreasedStaminaCost,   // Increases stamina cost
		SpecificResourcePenalty // Penalty vs a specific ResourceType
								// Add more specific or general effects as needed
	}

	/// <summary>
	/// Obfuscated names for tool bonuses shown to the player.
	/// </summary>
	public enum ToolBonusName
	{
		None,
		// Examples (Add ~20 total, mixing positive/negative implied effects)
		Sturdy,         // Sounds positive (Durability?)
		Heavy,          // Sounds negative (Weight+, Speed-?)
		Keen,           // Sounds positive (Crit?, Amount?)
		Dull,           // Sounds negative (Amount-, Speed-?)
		Efficient,      // Sounds positive (Speed+, Stamina-?)
		Clumsy,         // Sounds negative (Speed-, Crit-?)
		Ancient,        // Neutral (Could be good or bad?)
		Corrupted,      // Sounds negative (Multiple penalties?)
		Blessed,        // Sounds positive (Multiple bonuses?)
		SunKissed,      // Sounds positive (Crit?, Quality?)
		MoonTouched,    // Sounds positive (Speed?, Amount @ night?) - requires time check
		Brittle,        // Sounds negative (Durability-)
		Resourceful,    // Sounds positive (Amount+)
		Weighted,       // Sounds negative (Speed-)
		Swift,          // Sounds positive (Speed+)
		Cracked,        // Sounds negative (Durability-, Quality-)
		Glowing,        // Neutral/Positive? (Maybe minor light source + small stat?)
		Vampiric,       // Negative? (Maybe gathers more but damages player slightly?) - requires complex effect
		Eldritch,       // Neutral/Negative? (Random effects?)
		Clockwork       // Neutral/Positive? (Consistent speed but no crit?)
						// Add more names...
	}
}
