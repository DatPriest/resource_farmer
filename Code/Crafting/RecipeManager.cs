// File: Code/Crafting/RecipeManager.cs (Updated)
using Sandbox;
using System.Collections.Generic;
using System.Linq;
using ResourceFarmer.Resources;
using ResourceFarmer.Items; // <-- Add this

namespace ResourceFarmer.Crafting
{
	public sealed class RecipeManager : Component
	{
		public static RecipeManager Instance { get; private set; }

		public IReadOnlyList<CraftingRecipeResource> Recipes { get; private set; } = new List<CraftingRecipeResource>();

		protected override void OnAwake()
		{
			if ( Instance != null && Instance != this )
			{
				Log.Warning( $"Multiple instances of RecipeManager detected. Destroying duplicate on {GameObject.Name}." );
				Destroy();
				return;
			}
			Instance = this;
			LoadRecipes();
			Log.Info( "[RecipeManager] RecipeManager initialized." );
			Log.Info( $"[RecipeManager] Loaded {Recipes.Count} recipes." );
		}

		protected override void OnDestroy()
		{
			if ( Instance == this ) Instance = null;
		}

		private void LoadRecipes()
		{
			Log.Info( "[RecipeManager] Loading all CraftingRecipeResource assets..." );
			Recipes = ResourceLibrary.GetAll<CraftingRecipeResource>().ToList();

			if ( Recipes == null || Recipes.Count == 0 )
			{
				Log.Error( "[RecipeManager] Failed to load any .recipe files or none exist." );
				Recipes = new List<CraftingRecipeResource>();
			}
			else
			{
				Log.Info( $"[RecipeManager] Successfully loaded {Recipes.Count} recipes." );
				// Log example recipes using the updated Material property
				foreach ( var recipe in Recipes.Take( 3 ) )
				{
					Log.Info( $"  - Loaded: {recipe.Name} (Lvl {recipe.Level}, Mat: '{recipe.Material}', Type: {recipe.ToolType})" );
				}
			}
		}

		/// <summary>
		/// Finds a specific recipe resource based on its output characteristics.
		/// </summary>
		/// <param name="toolType">The ResourceType representing the tool type (e.g., Axe, Pickaxe).</param>
		/// <param name="materialName">The string name of the material.</param>
		/// <param name="level">The desired level of the tool.</param>
		/// <returns>The matching CraftingRecipeResource or null if not found.</returns>
		public CraftingRecipeResource FindRecipe( ResourceType toolType, string materialName, int level )
		{
			// Find based on properties of the loaded resources, comparing the string Material
			return Recipes.FirstOrDefault( r =>
				r.ToolType == toolType &&
				r.Material.Equals( materialName, System.StringComparison.OrdinalIgnoreCase ) && // Use case-insensitive compare
				r.Level == level );
		}
	}
}
