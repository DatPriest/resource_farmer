using Sandbox;
using Sandbox.UI;
using ResourceFarmer.UI.MainMenu;
using ResourceFarmer.Game;

namespace ResourceFarmer.UI;

public sealed class MainMenuManager : Component
{
	[Property] public string ShowMenuAction { get; set; } = "ShowMainMenu";
	[Property] public bool ShowOnStart { get; set; } = true;

	private MainMenuPanel _mainMenuInstance;
	private bool _isMenuVisible = false;

	protected override void OnStart()
	{
		base.OnStart();

		// Show main menu on start if enabled
		if (ShowOnStart && !Network.IsProxy)
		{
			ShowMainMenu();
		}
	}

	protected override void OnUpdate()
	{
		if (Network.IsProxy) return;

		// Toggle main menu with input
		if (Input.Pressed(ShowMenuAction))
		{
			ToggleMainMenu();
		}

		// Allow ESC to close menu
		if (Input.Pressed("escape") && _isMenuVisible)
		{
			HideMainMenu();
		}
	}

	public void ShowMainMenu()
	{
		if (_mainMenuInstance != null && _mainMenuInstance.IsValid)
		{
			Log.Warning("[MainMenuManager] Main menu is already open.");
			return;
		}

		_mainMenuInstance = Components.Create<MainMenuPanel>();
		_mainMenuInstance.OnMenuClosed += OnMainMenuClosed;
		_isMenuVisible = true;

		// Pause game or disable input while menu is open
		Game.TimeScale = 0f;
		
		Log.Info("[MainMenuManager] Main menu opened.");
	}

	public void HideMainMenu()
	{
		if (_mainMenuInstance != null && _mainMenuInstance.IsValid)
		{
			_mainMenuInstance.Destroy();
			_mainMenuInstance = null;
		}

		_isMenuVisible = false;
		
		// Resume game
		Game.TimeScale = 1f;
		
		Log.Info("[MainMenuManager] Main menu closed.");
	}

	public void ToggleMainMenu()
	{
		if (_isMenuVisible)
		{
			HideMainMenu();
		}
		else
		{
			ShowMainMenu();
		}
	}

	private void OnMainMenuClosed()
	{
		_mainMenuInstance = null;
		_isMenuVisible = false;
		Game.TimeScale = 1f;
		Log.Info("[MainMenuManager] Main menu self-closed.");
	}

	/// <summary>
	/// Start a new game - loads the main game scene
	/// </summary>
	public void StartGame()
	{
		Log.Info("[GameManager] Starting new game...");
		HideMainMenu();
		
		// In S&box, scene loading might be handled differently
		// For now, we'll just transition to game mode
		var gameManager = Scene.Active?.GetAllComponents<GameManager>().FirstOrDefault();
		gameManager?.EnterGameMode();
	}

	/// <summary>
	/// Show settings menu
	/// </summary>
	public void ShowSettings()
	{
		Log.Info("[MainMenuManager] Opening settings from main menu...");
		
		// Create settings panel as a child of main menu
		var settingsPanel = Components.Create<Settings>();
		
		// Note: Settings panel should handle its own visibility management
		// In S&box, UI panels manage their own visibility state
	}

	/// <summary>
	/// Exit the game
	/// </summary>
	public void ExitGame()
	{
		Log.Info("[MainMenuManager] Exiting game...");
		Game.Close();
	}

	/// <summary>
	/// Join multiplayer game
	/// </summary>
	public void JoinGame(string serverAddress = "")
	{
		Log.Info($"[MainMenuManager] Joining multiplayer game: {serverAddress}");
		
		// Implementation would depend on S&box networking
		// For now, just start single player
		StartGame();
	}
}