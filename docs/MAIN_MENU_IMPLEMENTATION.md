# Modular Main Menu System Implementation

## Overview

This implementation provides a complete, modular main menu system for Resource Farmer, built using S&box's Razor UI framework. The system follows modern UI patterns and integrates seamlessly with the existing codebase.

![Main Menu Demo](https://github.com/user-attachments/assets/066b0b51-43d5-454b-bd49-6ce5ba66c136)

## Architecture

### Component Hierarchy

```
GameManager (Root Controller)
├── MainMenuManager (Menu State Management)
│   └── MainMenuPanel (Main Container)
│       ├── MainMenuSection (Default View)
│       ├── SinglePlayerSection
│       ├── MultiplayerSection
│       └── MenuSettingsSection
└── UIManager (In-Game UI)
```

### Base Components

#### BaseButton

- **Purpose**: Reusable button component with consistent styling
- **Variants**: Primary, Secondary, Danger, Success, Warning
- **Features**: Icon support, tooltips, disabled state, hover effects

#### BasePanel

- **Purpose**: Modal panel container with header/footer
- **Sizes**: Small, Medium, Large, ExtraLarge, FullScreen, Custom
- **Features**: Close button, animations, responsive design

#### BaseSlider

- **Purpose**: Configurable slider for numeric inputs
- **Features**: Custom ranges, step values, value formatting, real-time updates

## Key Features

### ✅ Modular Architecture

- Easy to extend with new menu sections
- Reusable components for consistent UI
- Clear separation of concerns

### ✅ Modern Design

- Solo Leveling inspired aesthetic
- Smooth animations and transitions
- Responsive layout for different screen sizes

### ✅ S&box Integration

- Proper PanelComponent inheritance
- Network-safe implementation
- Component lifecycle management
- Integration with existing UI systems

### ✅ Extensible Settings System

- Volume controls (Master, Music, SFX)
- Gameplay settings (Mouse sensitivity, tooltips)
- Persistent configuration support

### ✅ Multiplayer Support

- Server browser preparation
- Custom server connection
- Host/Join game options

## File Structure

```
Code/
├── Game/
│   ├── GameManager.cs              # Overall game state management
│   └── README.md                   # Usage documentation
├── UI/
│   ├── Components/                 # Reusable UI components
│   │   ├── BaseButton.razor(.scss)
│   │   ├── BasePanel.razor(.scss)
│   │   └── BaseSlider.razor(.scss)
│   ├── MainMenu/                   # Main menu system
│   │   ├── MainMenuManager.cs      # Menu controller
│   │   ├── MainMenuPanel.razor(.scss)
│   │   ├── MainMenuSection.razor
│   │   ├── SinglePlayerSection.razor
│   │   ├── MultiplayerSection.razor(.scss)
│   │   └── MenuSettingsSection.razor(.scss)
│   └── Styles/
│       ├── Colors.scss             # Extended color palette
│       └── Theme.scss              # Updated imports
└── Test/
    └── MainMenuTest.cs             # Testing utilities
```

## Usage Examples

### Basic Setup (Automatic)

```csharp
// Add to your main scene GameObject
var gameManager = Components.Create<GameManager>();
gameManager.ShowMainMenuOnStart = true;
```

### Manual Control

```csharp
// Show/hide menu programmatically
var mainMenuManager = Components.Get<MainMenuManager>();
mainMenuManager?.ShowMainMenu();
mainMenuManager?.HideMainMenu();

// Game mode transitions
var gameManager = Components.Get<GameManager>();
gameManager?.EnterGameMode();
gameManager?.ExitToMainMenu();
```

### Creating Custom Menu Sections

```razor
@using ResourceFarmer.UI.Components
@inherits PanelComponent
@namespace ResourceFarmer.UI.MainMenu

<div class="menu-section">
    <h3 class="section-title">My Custom Section</h3>
    <div class="menu-buttons">
        <BaseButton
            Text="Custom Action"
            CssClass="menu-primary"
            OnClick="@HandleCustomAction" />
        <BaseButton
            Text="↩️ Back"
            CssClass="menu-secondary"
            OnClick="@(() => OnBack?.Invoke())" />
    </div>
</div>

@code {
    [Property] public Action OnBack { get; set; }

    private void HandleCustomAction()
    {
        // Your custom logic here
        Log.Info("Custom action executed!");
    }
}
```

## Integration Points

### With Existing Systems

- **UIManager**: Continues to handle in-game UI panels
- **Settings**: Integrates with game settings persistence
- **Networking**: Prepared for multiplayer connection handling
- **Resource System**: Compatible with existing resource management

### SCSS Integration

- Extends existing color palette (`Colors.scss`)
- Uses established layout variables (`Layout.scss`)
- Maintains consistent typography (`Text.scss`)
- Follows BEM naming conventions

## Testing

### Manual Testing

1. Add `GameManager` to a scene GameObject
2. Set `ShowMainMenuOnStart = true`
3. Run the scene - menu should appear
4. Test navigation between sections
5. Verify settings sliders and inputs work
6. Test game mode transitions

### Console Commands

```
test_main_menu    # Initialize test components
show_main_menu    # Force show main menu
```

### Demo

- Open `Code/UI/MainMenu/demo.html` in a browser
- Interactive demo showing visual design and button states
- Demonstrates responsive behavior and animations

## Technical Details

### Performance Considerations

- Efficient BuildHash implementations
- Minimal state updates through proper change detection
- Lazy component instantiation
- Proper component cleanup and disposal

### Accessibility

- Keyboard navigation support (ESC to close)
- Tooltip support for better UX
- High contrast design for readability
- Responsive design for different screen sizes

### Network Safety

- All menu logic runs client-side
- Server commands only for game state changes
- Proper validation of user inputs
- Network.IsProxy checks where appropriate

## Future Enhancements

### Planned Features

- Save/Load game functionality
- Server browser with real server discovery
- Achievement display integration
- Player profile management
- Advanced graphics settings
- Keybinding customization

### Extension Points

- Custom menu sections can be easily added
- Settings system can be extended with new categories
- Component variants can be created for specialized use cases
- Animation system can be enhanced with more complex transitions

## Conclusion

This implementation provides a solid foundation for Resource Farmer's main menu system. It balances modern UI principles with S&box best practices, creating a maintainable and extensible solution that can grow with the game's needs.

The modular architecture ensures that new features can be added without disrupting existing functionality, while the comprehensive styling system maintains visual consistency throughout the application.
