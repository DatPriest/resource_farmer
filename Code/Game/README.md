# Main Menu System Usage Guide

## Overview

The modular main menu system provides a modern, extensible UI framework for Resource Farmer. It consists of reusable components and a centralized management system.

## Components

### Base Components

- **BaseButton**: Reusable button with variants (Primary, Secondary, Danger, Success, Warning)
- **BasePanel**: Modal panel with header/footer, size variants, and animations
- **BaseSlider**: Configurable slider for settings and numeric inputs

### Main Menu Components

- **MainMenuManager**: Central controller for menu state and transitions
- **MainMenuPanel**: Root menu container with view switching
- **MainMenuSection**: Main menu screen with primary navigation
- **SinglePlayerSection**: Single player game options
- **MultiplayerSection**: Multiplayer connection with server input
- **MenuSettingsSection**: In-menu settings (audio, gameplay, controls)

### Game Management

- **GameManager**: Overall game state controller (menu ↔ game transitions)

## Setup Instructions

### In Scene Setup

1. Add a `GameManager` component to a GameObject in your scene
2. The GameManager will automatically create and configure the MainMenuManager
3. Set `ShowMainMenuOnStart = true` to display menu on scene load

```csharp
// Example GameObject setup in scene
var gameManagerObject = new GameObject("GameManager");
var gameManager = gameManagerObject.Components.Create<GameManager>();
gameManager.ShowMainMenuOnStart = true;
```

### Manual Usage

```csharp
// Show main menu programmatically
var mainMenuManager = Components.GetOrCreate<MainMenuManager>();
mainMenuManager.ShowMainMenu();

// Handle game transitions
var gameManager = Components.Get<GameManager>();
gameManager.EnterGameMode(); // Switch to gameplay
gameManager.ExitToMainMenu(); // Return to menu
```

## Extending the System

### Adding New Menu Sections

1. Create a new Razor component in `Code/UI/MainMenu/`
2. Follow the pattern of existing sections (inherit PanelComponent)
3. Add the new view to `MainMenuPanel.MenuView` enum
4. Update `MainMenuPanel.razor` to include your section

### Creating Custom Components

Use the base components as templates:

```razor
@using ResourceFarmer.UI.Components
@inherits PanelComponent

<BasePanel Title="My Custom Panel" Size="BasePanel.PanelSize.Medium">
    <BaseButton Text="Action" 
                Variant="BaseButton.ButtonVariant.Primary" 
                OnClick="@HandleAction" />
</BasePanel>
```

### Custom Styling

Components use the existing SCSS system:
- Extend `Colors.scss` for new color variables
- Follow BEM naming convention
- Use existing layout and typography variables

## Key Features

- **Modular Architecture**: Easy to extend and maintain
- **Responsive Design**: Works on different screen sizes
- **S&box Integration**: Proper component lifecycle and networking
- **Theme Consistency**: Matches existing Solo Leveling aesthetic
- **Accessibility**: Keyboard navigation and tooltips
- **Animation**: Smooth transitions and entrance effects

## Integration Points

The main menu integrates with:
- Existing UIManager for in-game panels
- Settings system for user preferences
- Scene loading and transitions
- Network connection handling
- Game state management

## Testing

To test the main menu system:
1. Start the game with GameManager in scene
2. Main menu should appear on startup
3. Navigate through different sections
4. Test settings sliders and inputs
5. Verify proper transitions between menu and game modes