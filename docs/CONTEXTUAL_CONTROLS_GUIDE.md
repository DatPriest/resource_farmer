# Contextual Controls HUD Feature

## Overview

The Contextual Controls feature provides dynamic HUD prompts that show players available actions based on what they're currently looking at. This improves user experience by eliminating guesswork about controls and providing clear visual feedback about interactable objects.

## Architecture

### Core Components

1. **PlayerContextualControlsComponent** (`Code/Player/PlayerContextualControlsComponent.cs`)

   - Attached to each Player GameObject
   - Performs raycast scanning to detect interactive objects
   - Generates appropriate contextual prompts
   - Networked component for real-time updates

2. **HUD Integration** (`Code/UI/HUD.razor`)

   - Displays contextual controls in bottom-center of screen
   - Renders prompts with highlighted keybinds
   - Smooth animations for show/hide states

3. **Styling** (`Code/UI/HUD.razor.scss`)
   - Modern S&box theme integration
   - Animated keybind highlighting
   - Smooth fade-in/out transitions

### How It Works

1. **Scanning**: Component scans 10 times per second using raycast from camera
2. **Detection**: Identifies ResourceNodes and IInteractable objects
3. **Prompt Generation**: Creates context-specific text with action hints
4. **Display**: HUD renders prompts with animated keybind highlights
5. **Updates**: Real-time updates as player looks around

## Features

### Dynamic Prompts

- **Resource Gathering**: "Press [LMB] to gather Wood"
- **Tool Efficiency**: Shows efficiency hints ("efficiently gather", "slowly gather")
- **Requirements**: "Cannot gather Stone (Need Pickaxe Lvl 2)"
- **Interactions**: "Press [RMB] to interact with Merchant"

### Visual Enhancements

- **Keybind Highlighting**: Keys shown in gold with special styling
- **Animation Effects**: Smooth slide-up animations
- **Efficiency Indicators**: Different prompts based on tool effectiveness
- **Priority System**: Most relevant actions shown first

### Performance Optimizations

- **Client-Side Only**: Scanning only occurs on local player
- **Throttled Updates**: 10 Hz update rate prevents performance issues
- **Efficient Caching**: Results cached to minimize unnecessary recalculations
- **Smart Networking**: Only updates when prompt actually changes

## Usage Examples

### Basic Resource Gathering

```
Looking at Tree:
"Press [LMB] to gather Wood"

Looking at Stone without tool:
"Cannot gather Stone (Need Pickaxe Lvl 1)"

Looking at Iron Ore with Bronze Pickaxe:
"Press [LMB] to slowly gather Iron"
```

### Interactive Objects

```
Looking at Merchant NPC:
"Press [RMB] to Trade"

Looking at Crafting Station:
"Press [RMB] to Craft Items"
```

## Implementation Details

### Component Integration

The contextual controls component is automatically added to all players in `Player.cs`:

```csharp
// Initialize contextual controls component for HUD prompts
var contextualComp = Components.GetOrCreate<PlayerContextualControlsComponent>();
if (contextualComp != null) contextualComp.OwnerPlayer = this;
```

### HUD Display Logic

The HUD checks for contextual controls in the render loop:

```razor
@if (LocalPlayer?.Components.Get<PlayerContextualControlsComponent>() is PlayerContextualControlsComponent contextComp)
{
    var controls = contextComp.GetCurrentControls();
    @foreach (var control in controls)
    {
        <div class="control_hint @(control.IsSecondary ? "secondary" : "") @(control.IsEnabled ? "enabled" : "disabled")">
            @RenderControlText(control.Text)
        </div>
    }
}
```

### Keybind Parsing

Text is automatically parsed to highlight keybinds in square brackets:

```csharp
// "Press [LMB] to gather" → "Press " + highlighted("[LMB]") + " to gather"
var parts = text.Split('[', ']');
// Even indices = regular text, odd indices = keybinds
```

## Configuration Options

### Scan Settings

- `ScanDistance`: How far to raycast (default: 100 units)
- `ScanRadius`: Raycast radius (default: 8 units)
- `ScanInterval`: Update frequency (default: 0.1s = 10 Hz)

### Visual Customization

- Colors defined in `Colors.scss` using existing theme
- Animation timing adjustable via CSS transitions
- Position and sizing configurable in SCSS

## Testing

Use the `ContextualControlsTest` class to validate functionality:

```csharp
ContextualControlsTest.TestContextualControls();
ContextualControlsTest.TestResourceNodePrompts();
```

## Future Enhancements

1. **Contextual Help**: Tutorial tooltips for new players
2. **Advanced Interactions**: Multi-step interaction prompts
3. **Customizable Keybinds**: Dynamic key display based on user settings
4. **Voice Integration**: Text-to-speech for accessibility
5. **Localization**: Multi-language support for prompts

## Troubleshooting

### Common Issues

1. **No Prompts Showing**: Check if PlayerContextualControlsComponent is attached
2. **Wrong Prompts**: Verify ResourceNode configuration and tool requirements
3. **Performance Issues**: Increase ScanInterval if needed
4. **Styling Problems**: Check SCSS compilation and imports

### Debug Commands

Enable debug logging to troubleshoot:

```csharp
Log.Info($"Current prompt: {contextComp.CurrentPrompt}");
Log.Info($"Has interactable: {contextComp.HasInteractable}");
```

## Contributing

When adding new interactable objects:

1. Implement `IInteractable` interface
2. Set appropriate `InteractionPrompt` property
3. Test with contextual controls system
4. Update this documentation if needed

The contextual controls system will automatically detect and display prompts for new interactables without additional configuration.
