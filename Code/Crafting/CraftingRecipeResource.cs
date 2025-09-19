// File: Code/Crafting/CraftingRecipeResource.cs (Updated)
using Sandbox;
using System.Collections.Generic;
using ResourceFarmer.Resources;
using ResourceFarmer.Items; // <-- Add this for ToolBonusName if needed later (not directly needed here)

namespace ResourceFarmer.Crafting
{
	/// <summary>
	/// Represents a crafting recipe defined as a game resource (.recipe file).
	/// </summary>
	[GameResource( "Crafting Recipe", "recipe", "Defines an item that can be crafted", Icon = "construction" )]
	public class CraftingRecipeResource : GameResource
	{
		/// <summary>
		/// The user-friendly display name (e.g., "Wooden Axe").
		/// </summary>
		[Property, Category( "Display" )]
		public string Name { get; set; } = "Unnamed Recipe";

		/// <summary>
		/// Optional: Category for UI grouping (e.g., "Axes", "Pickaxes").
		/// </summary>
		[Property, Category( "Display" )]
		public string Category { get; set; } = "Misc";

		// --- Output Tool Definition ---

		/// <summary>
		/// The type of tool being crafted (e.g., Axe, Pickaxe based on ResourceType enum).
		/// </summary>
		[Property, Category( "Output" )]
		public ResourceType ToolType { get; set; } // This maps to ToolBase.ToolType

		/// <summary>
		/// The material name of the tool (e.g., "Wood", "Stone", "Iron").
		/// This maps to ToolBase.Material.
		/// </summary>
		[Property, Category( "Output" )]
		public string Material { get; set; } = "Unknown"; // Changed type to string

		/// <summary>
		/// The level of the tool being crafted.
		/// </summary>
		[Property, Category( "Output" )]
		public int Level { get; set; } = 1;

		/// <summary>
		/// The base quality of the crafted tool (e.g., 0.0 to 10.0).
		/// </summary>
		[Property, Category( "Output" ), Range( 0f, 10f )]
		public float OutputQuality { get; set; } = 0.5f; // Added quality definition

		// --- Bonus Potential Definition ---

		/// <summary>
		/// Can this crafted item potentially receive random bonuses?
		/// </summary>
		[Property, Category( "Bonuses" )]
		public bool CanHaveBonuses { get; set; } = true; // Added bonus flag

		/// <summary>
		/// The minimum number of random bonuses this recipe can roll (if CanHaveBonuses is true).
		/// </summary>
		[Property, Category( "Bonuses" )]
		public int MinBonuses { get; set; } = 0; // Added min bonuses

		/// <summary>
		/// The maximum number of random bonuses this recipe can roll (if CanHaveBonuses is true).
		/// </summary>
		[Property, Category( "Bonuses" )]
		public int MaxBonuses { get; set; } = 1; // Added max bonuses


		[Property]
		public double BasePower { get; set; } = 1.0f; // Base power of the crafted item

		[Property]
		public List<ToolBonusName> PossibleBonuses { get; set; } = new(); // List of bonus instances
																		  // Fix for CA1822: Marking the property as static since it does not access instance data.
																		  // Fix for CS1501: Providing a lambda expression to the Select method to correctly project the data.


		// This property automatically derives the unique effects based on the names in PossibleBonuses.
		// It is NOT static because it depends on the instance property PossibleBonuses.
		public List<ToolBonusEffect> PossibleEffects
		{
			get
			{
				// Check the SOURCE list (PossibleBonuses) for null or empty.
				if ( PossibleBonuses == null || !PossibleBonuses.Any() )
				{
					// Return an empty list if no bonus names are selected.
					return new List<ToolBonusEffect>();
				}

				// Use LINQ to:
				// 1. Go through each 'bonusName' in the PossibleBonuses list.
				// 2. For each 'bonusName', get all its associated 'definitions' from the ToolBonusRegistry using GetDefinitions.
				//    SelectMany is crucial here as one name ("Ancient") might map to multiple definitions/effects.
				//    It flattens the sequence of sequences of definitions into a single sequence of definitions.
				// 3. From each resulting 'definition', select its 'Effect' property.
				// 4. Make the resulting sequence of effects unique using Distinct().
				// 5. Convert the unique effects back into a List<ToolBonusEffect>.
				return PossibleBonuses
					.SelectMany( bonusName => ToolBonusRegistry.GetDefinitions( bonusName ) ) // Get all definitions for each name
					.Select( definition => definition.Effect )                               // Select the Effect from each definition
					.Distinct()                                                              // Ensure uniqueness
					.ToList();                                                               // Convert to List
			}
		}


		// --- Requirements ---

		/// <summary>
		/// The required profession level for the associated ToolType.
		/// </summary>
		[Property, Category( "Requirements" )]
		public int RequiredProfessionLevel { get; set; } = 1;

		/// <summary>
		/// The resources required to craft this item. Key is ResourceType, Value is amount.
		/// </summary>
		[Property, Category( "Requirements" )]
		public Dictionary<ResourceType, float> Costs { get; set; } = new();

	}
}
