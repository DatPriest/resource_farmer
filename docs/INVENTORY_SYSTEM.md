# Enhanced Inventory System

## Overview

The enhanced inventory system provides comprehensive resource management capabilities for players in Resource Farmer. It includes capacity limits, sorting, filtering, and interactive resource management.

## Components

### PlayerInventoryComponent

- **Location**: `Code/Player/PlayerInventoryComponent.cs`
- **Purpose**: Enhanced inventory management with capacity limits
- **Features**:
  - Capacity limits (default: 1000 units)
  - Add/Remove/Consume resources with validation
  - Inventory full notifications
  - Resource amount checking
  - Sorting and filtering capabilities

### InventoryPanel

- **Location**: `Code/UI/InventoryPanel.razor`
- **Purpose**: Full-featured inventory UI panel
- **Features**:
  - Grid-based resource display with icons
  - Search and filtering
  - Sorting by name, amount, or category
  - Resource rarity color coding
  - Individual resource actions (Use/Sell)
  - Drag & drop support (placeholder)
  - Detailed resource information

## Key Features

### 1. Capacity Management

- Maximum inventory capacity of 1000 units (configurable)
- Visual capacity bar showing current usage
- Automatic overflow prevention
- Capacity upgrade support

### 2. Resource Organization

- **Sorting Options**: Name, Amount, Category
- **Search**: Filter resources by name
- **Categories**: Basic, Ores, Gems, Magical
- **Rarity System**: Common, Uncommon, Rare, Epic, Legendary, Magical, Mythic

### 3. Resource Interaction

- **Use**: Consume single units of resources
- **Sell**: Sell individual resource types
- **Sell All**: Sell entire inventory at once
- **Visual Feedback**: Hover effects and animations

### 4. UI Integration

- **Hotkey**: Press 'I' to open/close inventory
- **HUD Button**: Click inventory icon in top panel
- **Modal Interface**: Centered overlay with mouse capture

## Usage

### Opening the Inventory

1. Press the 'I' key
2. Click the inventory button (4e6) in the HUD
3. Call `ToggleInventoryPanel()` from UIManager

### Resource Management

1. **Adding Resources**: Use `PlayerInventoryComponent.AddResource()`
2. **Removing Resources**: Use `PlayerInventoryComponent.RemoveResource()`
3. **Checking Resources**: Use `PlayerInventoryComponent.HasResource()`

### Upgrading Capacity

```csharp
player.InventoryComponent.UpgradeCapacity(500f); // Add 500 more capacity
```

## Integration Points

### Player Class

- Added `InventoryComponent` property
- Enhanced `GatherResource()` method to use capacity limits
- Automatic component initialization

### HUD Updates

- Added inventory button to resources panel
- Visual integration with existing UI theme

### Input System

- Added "InventoryMenu" action bound to 'I' key
- Integrated with UIManager for panel management

## Resource Value System

Resources have different base values for selling:

- **Basic Materials**: Wood (0.5), Stone (0.8), Fiber (0.3)
- **Common Ores**: Copper (1.5), Tin (1.8), Iron (3.0), Coal (1.0)
- **Precious Metals**: Silver (5.0), Gold (8.0)
- **Legendary Ores**: Mithril (15.0), Adamantite (25.0)
- **Gems**: Ruby/Sapphire (10.0), Emerald (12.0), Diamond (20.0)
- **Magical Items**: Essence Dust (50.0), Crystal Shard (100.0)
- **Mythic Items**: Dragon Scale (500.0), Phoenix Feather (1000.0)

## Visual Design

- Modern dark theme with glass morphism effects
- Color-coded resource rarity system
- Smooth animations and hover effects
- Responsive grid layout
- Accessibility-friendly design

## Future Enhancements

- Drag & drop functionality
- Resource stacking limits
- Item tooltips with more information
- Bulk operations
- Inventory tabs/categories
- Auto-sorting options
- Resource alerts and notifications
