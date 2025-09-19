using Sandbox;
using ResourceFarmer.UI;

namespace ResourceFarmer.Game;

/// <summary>
/// Main game manager that handles overall game state and systems initialization
/// </summary>
public sealed class GameManager : Component
{
	public static GameManager Instance { get; private set; }

	[Property] public bool ShowMainMenuOnStart { get; set; } = true;
	[Property] public bool IsInGameMode { get; set; } = false;

	private MainMenuManager _mainMenuManager;
	private UIManager _uiManager;

	protected override void OnStart()
	{
		base.OnStart();
		
		Instance = this;
		
		// Initialize main systems
		InitializeSystems();
		
		Log.Info("[GameManager] Game systems initialized.");
	}

	private void InitializeSystems()
	{
		// Get or create main menu manager
		_mainMenuManager = Components.GetOrCreate<MainMenuManager>();
		
		// Get UI manager for in-game UI
		_uiManager = Components.GetOrCreate<UIManager>();
		
		// Set up main menu to show on start if we're not in game mode
		if (_mainMenuManager && ShowMainMenuOnStart && !IsInGameMode)
		{
			_mainMenuManager.ShowOnStart = true;
		}
		else if (_mainMenuManager)
		{
			_mainMenuManager.ShowOnStart = false;
		}
	}

	/// <summary>
	/// Transition from main menu to game mode
	/// </summary>
	public void EnterGameMode()
	{
		IsInGameMode = true;
		
		// Hide main menu if it's showing
		_mainMenuManager?.HideMainMenu();
		
		// Initialize game-specific systems
		InitializeGameSystems();
		
		Log.Info("[GameManager] Entered game mode.");
	}

	/// <summary>
	/// Return to main menu from game
	/// </summary>
	public void ExitToMainMenu()
	{
		IsInGameMode = false;
		
		// Show main menu
		_mainMenuManager?.ShowMainMenu();
		
		// Clean up game-specific systems if needed
		CleanupGameSystems();
		
		Log.Info("[GameManager] Exited to main menu.");
	}

	private void InitializeGameSystems()
	{
		// Initialize systems that should only run during gameplay
		// This could include resource spawning, NPC management, etc.
		
		// Example: Enable resource spawning
		var resourceSpawner = Game.ActiveScene?.GetAllComponents<Resources.ResourceSpawner>().FirstOrDefault();
		resourceSpawner?.Enable();
	}

	private void CleanupGameSystems()
	{
		// Clean up or disable gameplay-specific systems
		// This could include stopping resource spawning, clearing temporary data, etc.
		
		// Example: Disable resource spawning
		var resourceSpawner = Game.ActiveScene?.GetAllComponents<Resources.ResourceSpawner>().FirstOrDefault();
		resourceSpawner?.Disable();
	}

	/// <summary>
	/// Quit the entire game
	/// </summary>
	public void QuitGame()
	{
		Log.Info("[GameManager] Quitting game...");
		Game.Close();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		
		// Handle global input that should work in both menu and game modes
		// For example, F1 to toggle main menu overlay in game
		if (Input.Pressed("F1") && IsInGameMode)
		{
			_mainMenuManager?.ToggleMainMenu();
		}
	}
}