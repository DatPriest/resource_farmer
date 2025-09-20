# World Panel System Migration Guide

## Overview

This guide helps you migrate from the old `WorldPanelManager` + `ResourceNodePanel` system to the new modular World Panel System.

## Quick Migration Steps

### 1. Update Player Components

**Old System:**

```csharp
// In Player prefab or setup code
var worldPanelManager = player.Components.Create<WorldPanelManager>();
worldPanelManager.UpdateInterval = 0.1f;
```

**New System:**

```csharp
// Replace with WorldPanelVisibilityManager
var visibilityManager = player.Components.Create<WorldPanelVisibilityManager>();
visibilityManager.UpdateInterval = 0.1f;
visibilityManager.MaxScanRange = 500f;
visibilityManager.EnableDebugLogging = false; // Set to true for debugging
```

### 2. Update Resource Node Panels

**Old System:**

```csharp
// Creating old ResourceNodePanel
var resourcePanel = resourceNode.Components.Create<ResourceNodePanel>();
resourcePanel.Node = resourceNode;
```

**New System:**

```csharp
// Use new ResourceWorldPanel (auto-configures from ResourceNode)
var resourcePanel = resourceNode.Components.Create<ResourceWorldPanel>();
// No manual configuration needed - automatically finds and uses ResourceNode
```

### 3. Prefab Updates

**Old Resource Node Prefab Structure:**

```
ResourceNode GameObject
├── ResourceNode (Component)
├── ResourceNodePanel (PanelComponent)
└── WorldPanelManager (manages visibility)
```

**New Resource Node Prefab Structure:**

```
ResourceNode GameObject
├── ResourceNode (Component)
└── ResourceWorldPanel (WorldPanelComponent) - auto-managed
```

## Migration Checklist

### Phase 1: Preparation

- [ ] Backup your current project
- [ ] Read through the [World Panel System documentation](WORLD_PANEL_SYSTEM.md)
- [ ] Identify all GameObjects using `ResourceNodePanel`
- [ ] Identify all Players using `WorldPanelManager`

### Phase 2: Player Updates

- [ ] Replace `WorldPanelManager` with `WorldPanelVisibilityManager` on all Player GameObjects
- [ ] Test that existing panels still work (backward compatibility)
- [ ] Configure visibility manager settings (UpdateInterval, MaxScanRange)

### Phase 3: Resource Node Updates

- [ ] Replace `ResourceNodePanel` components with `ResourceWorldPanel`
- [ ] Remove manual Node property assignments (auto-detected now)
- [ ] Test resource node panel visibility and content

### Phase 4: Custom Panel Updates (if any)

- [ ] Identify any custom world panels extending the old system
- [ ] Migrate custom panels to extend `WorldPanelComponent`
- [ ] Update custom panel logic to use new lifecycle methods

### Phase 5: Testing and Optimization

- [ ] Test panel visibility ranges and scaling
- [ ] Verify performance with multiple panels and players
- [ ] Enable debug logging temporarily to verify system is working correctly
- [ ] Disable deprecated WorldPanelManager if no longer needed

## Code Examples

### Resource Node Setup

**Before:**

```csharp
public void SetupResourceNode(GameObject resourceNodeObj)
{
    var resourceNode = resourceNodeObj.Components.Get<ResourceNode>();
    var panel = resourceNodeObj.Components.Create<ResourceNodePanel>();
    panel.Node = resourceNode;

    // Panel managed by WorldPanelManager on Player
}
```

**After:**

```csharp
public void SetupResourceNode(GameObject resourceNodeObj)
{
    var resourceNode = resourceNodeObj.Components.Get<ResourceNode>();
    var panel = resourceNodeObj.Components.Create<ResourceWorldPanel>();
    // No manual setup needed - panel auto-configures from ResourceNode
    // Panel managed by WorldPanelVisibilityManager on Player
}
```

### Custom Panel Migration

**Old Custom Panel:**

```csharp
public class MyCustomPanel : PanelComponent
{
    [Property] public SomeComponent Target { get; set; }

    protected override void OnUpdate()
    {
        // Manual visibility and positioning logic
        if (ShouldBeVisible())
        {
            Enabled = true;
            UpdatePosition();
            UpdateContent();
        }
        else
        {
            Enabled = false;
        }
    }
}
```

**New Custom Panel:**

```csharp
public class MyCustomPanel : WorldPanelComponent
{
    [Property] public SomeComponent Target { get; set; }

    protected override void UpdateContent()
    {
        // Only content updates - visibility and positioning handled automatically
        if (Target != null && Target.IsValid())
        {
            // Update panel content based on target state
        }
    }

    // Lifecycle methods available for custom behavior
    protected override void OnPlayerEnterRange(GameObject player, float distance)
    {
        base.OnPlayerEnterRange(player, distance);
        // Custom logic when player enters range
    }
}
```

## Backward Compatibility

The new system maintains backward compatibility:

- **WorldPanelManager** still works but shows deprecation warnings
- **ResourceNodePanel** continues to function with legacy WorldPanelManager
- **Existing prefabs** work without immediate changes
- **Old code** continues to function while you migrate gradually

## Benefits After Migration

### Performance Improvements

- **Efficient Batching**: Single manager handles all panels instead of individual panel updates
- **Smart Caching**: Panel lists cached and refreshed periodically
- **Distance Optimization**: MaxScanRange limits expensive distance calculations
- **Update Throttling**: Configurable update frequency for performance tuning

### Code Quality Improvements

- **Separation of Concerns**: Visibility, positioning, and content separated
- **Reusability**: Same system works for any type of world panel
- **Maintainability**: Modular components easier to modify and extend
- **Testability**: Individual components can be tested in isolation

### Feature Enhancements

- **Distance Scaling**: Panels scale based on distance automatically
- **Smooth Animations**: Built-in support for entrance/exit animations
- **Flexible Positioning**: Configurable height offsets and positioning
- **Multiple Panel Types**: Single GameObject can have multiple different panels

## Troubleshooting Migration Issues

### Panel Not Appearing

1. **Check Player Setup**: Ensure `WorldPanelVisibilityManager` is attached to Player
2. **Verify Range**: Check if `VisibilityRange` is appropriate for distance
3. **Confirm Target**: Verify `TargetObject` is set correctly (auto-set to parent by default)

### Performance Issues

1. **Increase Update Interval**: Change from 0.1f to 0.2f or higher
2. **Reduce Scan Range**: Lower `MaxScanRange` for fewer distance calculations
3. **Disable Distance Scaling**: Turn off `UseDistanceScaling` if not needed

### Content Not Updating

1. **Override UpdateContent()**: Ensure you've implemented content update logic
2. **Check BuildHash()**: Include relevant properties to trigger updates
3. **Verify Panel Enabled**: Confirm panel is enabled and in range

### Migration Validation

```csharp
// Debug helper to validate migration
public void ValidateMigration()
{
    var players = Scene.GetAllComponents<Player>();
    var oldManagers = Scene.GetAllComponents<WorldPanelManager>();
    var newManagers = Scene.GetAllComponents<WorldPanelVisibilityManager>();
    var oldPanels = Scene.GetAllComponents<ResourceNodePanel>();
    var newPanels = Scene.GetAllComponents<ResourceWorldPanel>();

    Log.Info($"Migration Status:");
    Log.Info($"  Players: {players.Count()}");
    Log.Info($"  Old Managers: {oldManagers.Count()} (should be 0 when migration complete)");
    Log.Info($"  New Managers: {newManagers.Count()} (should equal player count)");
    Log.Info($"  Old Panels: {oldPanels.Count()} (should be 0 when migration complete)");
    Log.Info($"  New Panels: {newPanels.Count()}");
}
```

## Support

If you encounter issues during migration:

1. **Enable Debug Logging**: Set `EnableDebugLogging = true` on `WorldPanelVisibilityManager`
2. **Check Console Output**: Look for warnings and error messages
3. **Test Incrementally**: Migrate one component type at a time
4. **Use Backward Compatibility**: Keep old system running alongside new during transition
5. **Refer to Examples**: Check `ExampleWorldPanel.razor` for implementation patterns

The migration can be done gradually - both systems can coexist during the transition period.
