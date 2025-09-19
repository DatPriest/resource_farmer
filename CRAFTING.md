# Crafting System Documentation

## Overview

The Resource Farmer crafting system allows players to create tools and equipment from gathered resources. The system follows modern S&box architecture patterns and supports quality variations, random bonuses, and profession level requirements.

## System Components

### CraftingRecipeResource (.recipe files)
Recipe files are GameResource assets located in `Assets/Crafting/` that define craftable items:

```json
{
  "Name": "Wooden Axe",
  "Category": "Axes", 
  "ToolType": "Axe",
  "Material": "Wood",
  "Level": 1,
  "OutputQuality": 0.09,
  "CanHaveBonuses": true,
  "MinBonuses": 0,
  "MaxBonuses": 10,
  "RequiredProfessionLevel": 1,
  "Costs": {
    "Wood": 1
  }
}
```

### RecipeManager Component
- Singleton component that loads all .recipe files at startup
- Provides `FindRecipe(toolType, material, level)` method for recipe lookup
- Automatically loads via `ResourceLibrary.GetAll<CraftingRecipeResource>()`

### Player Crafting Methods
- `GetProfessionLevel(ResourceType toolType)` - Returns profession level for tool type
- `GetMaxProfessionLevel()` - Returns maximum possible profession level (100)
- `ProcessCraftingRequest(recipe)` - Server-side crafting logic with validation

## Modern API Usage

The system uses modern S&box networking patterns:

### Client-Server Communication
```csharp
// Client side (UI)
ConsoleSystem.Run("craft_item", recipe.ToolType.ToString(), recipe.Material, recipe.Level.ToString());

// Server side (Player.Crafting.cs)
[ConCmd.Server("craft_item")]
public static void CraftItem(string toolType, string material, int level)
```

### Server-Side Validation
- All crafting requests are validated on the server
- Checks resource availability, profession level requirements
- Deducts resources only after validation succeeds

## Quality and Bonuses System

### Quality Calculation
- Base quality from recipe + skill-based variance
- Higher profession levels reduce negative variance
- Final quality affects tool effectiveness

### Random Bonuses
- Recipes can specify min/max bonus counts
- Bonuses are rolled from `PossibleBonuses` list
- Each bonus has magnitude variation for power scaling

## Recipe Creation Workflow

1. Create new .recipe file in `Assets/Crafting/`
2. Define recipe properties (name, costs, requirements)
3. Set output tool characteristics (quality, bonuses)
4. RecipeManager automatically loads on startup
5. Players can craft via in-game UI

## Example Recipes

**Basic Tool:**
```json
{
  "Name": "Stone Axe",
  "ToolType": "Axe",
  "Material": "Stone", 
  "RequiredProfessionLevel": 1,
  "Costs": { "Stone": 5, "Wood": 2 }
}
```

**Advanced Tool:**
```json
{
  "Name": "Iron Axe",
  "ToolType": "Axe",
  "Material": "Iron",
  "Level": 2,
  "RequiredProfessionLevel": 5,
  "Costs": { "IronOre": 15, "Coal": 5, "Wood": 3 }
}
```

## UI Integration

The crafting UI (`Code/UI/Crafting/`) provides:
- Recipe browsing by category
- Resource requirement checking
- Real-time affordability validation
- Tool upgrade interface

## Extending the System

### Adding New Tool Types
1. Add to `ResourceType` enum
2. Create recipe files with new `ToolType`
3. Update `ToolBase` if special behavior needed

### Adding New Materials
1. Add resource to `ResourceType` enum if not exists
2. Create recipes with new `Material` name
3. Update upgrade cost formulas in `GetCurrentToolUpgradeCost()`

### Adding New Bonuses
1. Define in `ToolBonusRegistry`
2. Add to recipe `PossibleBonuses` lists
3. Implement effects in `ToolBase` calculations