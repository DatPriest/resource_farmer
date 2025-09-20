# Modular World Panel System

## Overview

The World Panel System provides a flexible, reusable architecture for displaying UI panels in world space that are visible to nearby players. This system replaces the old tightly-coupled `WorldPanelManager` + `ResourceNodePanel` approach with a modular, component-based design.

## Architecture

### Core Components

```
WorldPanelComponent (Abstract Base Class)
├── ResourceWorldPanel (Resource-specific implementation)
├── GenericInfoWorldPanel (Flexible info display)
└── Custom Implementations (extend WorldPanelComponent)

WorldPanelVisibilityManager (Attached to Player)
├── Manages visibility for all WorldPanelComponent instances
├── Distance-based show/hide logic
└── Performance optimization with caching
```

## Key Features

- **Universal**: Works with any GameObject, not just ResourceNodes
- **Modular**: Separate positioning, visibility, and content concerns
- **Performance Optimized**: Efficient distance calculations and update batching
- **Flexible**: Configurable ranges, scaling, animations, and styling
- **Backward Compatible**: Existing code continues to work

## Component Reference

### WorldPanelComponent (Base Class)

Abstract base class for all world panels.

```csharp
public abstract class WorldPanelComponent : PanelComponent
{
    [Property] public float VisibilityRange { get; set; } = 150f;
    [Property] public float HeightOffset { get; set; } = 50f;
    [Property] public bool FaceCamera { get; set; } = true;
    [Property] public bool UseDistanceScaling { get; set; } = true;
    [Property] public float MinScale { get; set; } = 0.5f;
    [Property] public float MaxScale { get; set; } = 1.2f;
    [Property] public GameObject TargetObject { get; set; }

    // Override in subclasses to define panel content updates
    protected abstract void UpdateContent();
}
```

### WorldPanelVisibilityManager

Manages visibility for all world panels based on player proximity. Attach to Player GameObjects.

```csharp
public sealed class WorldPanelVisibilityManager : Component
{
    [Property] public float UpdateInterval { get; set; } = 0.1f;
    [Property] public float MaxScanRange { get; set; } = 500f;
    [Property] public bool EnableDebugLogging { get; set; } = false;
}
```

### ResourceWorldPanel

Specialized world panel for displaying resource node information.

```csharp
// Automatically finds ResourceNode in parent/self
// Displays: Resource type, amount, difficulty, tool requirements
```

### GenericInfoWorldPanel

Flexible world panel for displaying custom information.

```csharp
[Property] public string Title { get; set; }
[Property] public string MainText { get; set; }
[Property] public string SubText { get; set; }
[Property] public bool ShowDistance { get; set; }
[Property] public Action<GenericInfoWorldPanel> UpdateCallback { get; set; }
```

## Usage Examples

### Example 1: Resource Node with Modular Panel

```csharp
// Old approach (still works but deprecated)
var resourceNodePanel = resourceNode.Components.Create<ResourceNodePanel>();

// New modular approach
var worldPanel = resourceNode.Components.Create<ResourceWorldPanel>();
// Panel automatically configures itself based on the ResourceNode
```

### Example 2: Custom Info Panel for NPCs

```csharp
public class NPCInfoPanel : WorldPanelComponent
{
    [Property] public NPC TargetNPC { get; set; }

    protected override void UpdateContent()
    {
        if (TargetNPC == null) return;

        // Update panel content based on NPC state
        // Access UI elements and update them
    }
}
```

### Example 3: Generic Information Display

```csharp
// Create a generic info panel for any GameObject
var infoPanel = someGameObject.Components.Create<GenericInfoWorldPanel>();
infoPanel.Title = "Mystical Fountain";
infoPanel.MainText = "Restores Health";
infoPanel.SubText = "Cost: 10 Gold";
infoPanel.ShowDistance = true;
infoPanel.VisibilityRange = 200f;

// Dynamic updates via callback
infoPanel.UpdateCallback = (panel) => {
    panel.SubText = $"Cost: {currentPrice} Gold";
};
```

### Example 4: Player Setup for Visibility Management

```csharp
// In Player component or Player prefab setup
var visibilityManager = Components.Create<WorldPanelVisibilityManager>();
visibilityManager.UpdateInterval = 0.1f;  // Check 10 times per second
visibilityManager.MaxScanRange = 500f;    // Optimize performance
visibilityManager.EnableDebugLogging = false;
```

## Migration Guide

### From Old System to New System

**Old ResourceNodePanel:**

```razor
@inherits PanelComponent
// Tightly coupled to ResourceNode
// Managed by WorldPanelManager
```

**New ResourceWorldPanel:**

```razor
@inherits WorldPanelComponent
// Modular, reusable design
// Managed by WorldPanelVisibilityManager
// Automatic configuration from ResourceNode
```

### Migration Steps

1. **Add WorldPanelVisibilityManager to Player:**

   ```csharp
   // Replace WorldPanelManager with WorldPanelVisibilityManager
   var visibilityManager = player.Components.Create<WorldPanelVisibilityManager>();
   ```

2. **Replace ResourceNodePanel with ResourceWorldPanel:**

   ```csharp
   // Old
   var panel = resourceNode.Components.Create<ResourceNodePanel>();

   // New
   var panel = resourceNode.Components.Create<ResourceWorldPanel>();
   ```

3. **Update Custom Panels:**
   ```csharp
   // Extend WorldPanelComponent instead of PanelComponent
   public class MyCustomPanel : WorldPanelComponent
   {
       protected override void UpdateContent()
       {
           // Implement your content update logic
       }
   }
   ```

## Performance Considerations

- **Update Interval**: Default 0.1s (10Hz) balances responsiveness with performance
- **Visibility Range**: Larger ranges increase computation but improve UX
- **Max Scan Range**: Limits maximum distance for performance optimization
- **Panel Caching**: Visibility manager caches panel lists for efficiency
- **Distance Scaling**: Optional feature that can be disabled for better performance

## Styling and Theming

Panels use the existing theme system:

```scss
// ResourceWorldPanel.razor.scss
@import "../Styles/Colors.scss";
@import "../Styles/Layout.scss";
@import "../Styles/Text.scss";

.resource-world-panel {
  .panel-container {
    background-color: rgba($background-dark, 0.85);
    border: $border-width-thin solid rgba($accent-blue, 0.6);
    // ... more styling
  }
}
```

## Advanced Features

### Custom Animations

```csharp
protected override void OnPlayerEnterRange(GameObject player, float distance)
{
    base.OnPlayerEnterRange(player, distance);

    // Custom entrance animation
    Style.Opacity = 0f;
    Style.Transitions = new() { new() { Property = "opacity", Duration = 0.3f } };
    Style.Opacity = 1f;
}
```

### Dynamic Content Updates

```csharp
protected override void UpdateContent()
{
    // Called every frame while player is in range
    // Update text, colors, or other UI elements based on game state
    if (someCondition)
    {
        Style.SetClass("warning-state", true);
    }
}
```

### Multiple Panel Types per Object

```csharp
// A single GameObject can have multiple world panels
var infoPanel = gameObject.Components.Create<GenericInfoWorldPanel>();
var statusPanel = gameObject.Components.Create<StatusWorldPanel>();

// Configure different ranges and positions
infoPanel.VisibilityRange = 100f;
infoPanel.HeightOffset = 60f;
statusPanel.VisibilityRange = 50f;
statusPanel.HeightOffset = 80f;
```

## Troubleshooting

### Common Issues

1. **Panel not appearing:**

   - Ensure WorldPanelVisibilityManager is attached to Player
   - Check VisibilityRange vs actual distance
   - Verify TargetObject is set correctly

2. **Performance issues:**

   - Increase UpdateInterval
   - Reduce MaxScanRange
   - Disable distance scaling if not needed

3. **Content not updating:**
   - Override UpdateContent() method
   - Check if panel is Enabled
   - Verify BuildHash() includes relevant properties

### Debug Information

```csharp
// Enable debug logging
visibilityManager.EnableDebugLogging = true;

// Check managed panel count
Log.Info($"Managing {visibilityManager.ManagedPanelCount} panels");

// Get list of managed panels
var panels = visibilityManager.GetManagedPanels();
```

## Future Enhancements

- **World-Space Rotation**: 3D panel rotation support
- **Occlusion Testing**: Hide panels behind objects
- **LOD System**: Different detail levels based on distance
- **Panel Pooling**: Reuse panel instances for better performance
- **Clustering**: Group nearby panels for batch operations
