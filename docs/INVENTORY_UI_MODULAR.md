# Modular Inventory UI Architecture

## Overview

The inventory UI has been successfully refactored from a monolithic 383-line Razor component into a modular, component-based architecture following S&box and Resource Farmer conventions.

## Component Architecture

```
InventoryPanel (Main Container)
├── InventoryHeader (Title, Capacity, Close Button)
├── FilterButtons (Search, Sort, Action Buttons)
├── Inventory Content
│   ├── InventorySlot × N (Individual Resource Items)
│   └── Empty Inventory State
└── ItemTooltip (Rich Resource Information)
```

## Created Components

### 1. InventorySlot.razor

**Purpose**: Individual resource item display with full interactivity
**Features**:

- Drag-and-drop support with event callbacks
- Rarity-based visual styling (Common → Mythic with rainbow borders)
- Resource icon, name, amount display
- Use/Sell action buttons
- Hover effects and smooth transitions

### 2. ItemTooltip.razor

**Purpose**: Rich tooltip component for resource information
**Features**:

- Detailed resource information (name, amount, category, value)
- Rarity-based border styling
- Position-aware display
- Configurable content (value, help text)
- Smooth fade-in animation

### 3. FilterButtons.razor

**Purpose**: Reusable search, sort, and action controls
**Features**:

- Search input with icon
- Configurable sort options
- Dynamic action buttons with icons
- Event-driven communication with parent
- Responsive design

### 4. InventoryHeader.razor

**Purpose**: Panel header with title and capacity information
**Features**:

- Configurable title and icon
- Visual capacity bar with color-coded fill
- Close button with hover effects
- Responsive layout

## Key Architectural Improvements

### ✅ Separation of Concerns

- **UI Components**: Handle presentation and user interaction
- **Business Logic**: Remains in main InventoryPanel (UseResource, SellResource)
- **Event Communication**: Clean callback-based component interaction

### ✅ Reusability

- Components can be used in other inventory contexts (crafting, trading, etc.)
- Configurable properties enable different use cases
- Consistent API patterns with existing UI components

### ✅ Maintainability

- Single responsibility components (~100-200 lines each vs 383 original)
- Isolated SCSS files prevent style conflicts
- Clear component boundaries and interfaces

### ✅ S&box Compliance

- Follows PanelComponent inheritance pattern
- Uses modern C# features (file-scoped namespaces, primary constructors)
- Compatible with S&box's hot-reloading system
- Network-safe implementation (no mutable static state)

## Preserved Functionality

### ✅ Drag and Drop

- Maintained through `OnDragStart` event callbacks
- `InventorySlot` handles drag initiation
- Main panel manages drag state and logic

### ✅ Resource Management

- All business logic preserved (use, sell, filtering, sorting)
- Resource value calculation intact
- Inventory capacity management working

### ✅ Visual Design

- Rarity color coding enhanced and modularized
- Hover effects and animations preserved
- Responsive design maintained

### ✅ Tooltips

- Enhanced with modular `ItemTooltip` component
- Richer information display
- Better positioning and styling

## File Structure

```
Code/UI/
├── InventoryPanel.razor          # Main container (reduced from 383 to ~200 lines)
├── InventoryPanel.razor.scss     # Container-specific styles
├── Components/
│   ├── InventorySlot.razor       # Individual resource slots
│   ├── InventorySlot.razor.scss  # Slot styling with rarity colors
│   ├── ItemTooltip.razor         # Rich tooltip component
│   ├── ItemTooltip.razor.scss    # Tooltip styling and animations
│   ├── FilterButtons.razor       # Search/sort/actions
│   ├── FilterButtons.razor.scss  # Filter controls styling
│   ├── InventoryHeader.razor     # Header with capacity bar
│   └── InventoryHeader.razor.scss # Header styling
└── Styles/
    └── Theme.scss                # Updated to import new components
```

## Migration Benefits

1. **Reduced Complexity**: Main component reduced from 383 to ~200 lines
2. **Better Testing**: Individual components can be tested in isolation
3. **Enhanced Reusability**: Components usable across different UIs
4. **Improved Performance**: Smaller components with focused update cycles
5. **Easier Extension**: New inventory features can be added as separate components

## Integration Points

The modular components integrate seamlessly with:

- Existing `BaseButton` and `BasePanel` components
- Resource Farmer's networking layer (`[Net]` properties)
- Player inventory management system
- Theme system and SCSS architecture

## Future Enhancements

With this modular foundation, future improvements become easier:

- Drag-and-drop between different containers
- Advanced filtering (by rarity, category, etc.)
- Bulk operations (select multiple items)
- Customizable layouts and grid sizes
- Context menus and right-click actions
