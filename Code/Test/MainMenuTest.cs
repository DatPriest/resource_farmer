using Sandbox;
using ResourceFarmer.UI;
using ResourceFarmer.UI.MainMenu;
using ResourceFarmer.Game;

namespace ResourceFarmer.Test;

/// <summary>
/// Test class to demonstrate and validate the main menu system
/// This component can be added to a test scene to showcase the main menu
/// </summary>
public sealed class MainMenuTest : Component
{
	[Property] public bool TestMainMenu { get; set; } = true;
	[Property] public bool TestComponents { get; set; } = true;

	protected override void OnStart()
	{
		base.OnStart();
		
		if (TestMainMenu)
		{
			TestMainMenuSystem();
		}
		
		if (TestComponents)
		{
			TestUIComponents();
		}
	}

	private void TestMainMenuSystem()
	{
		Log.Info("[MainMenuTest] Testing main menu system...");
		
		// Create GameManager which will handle the main menu
		var gameManager = Components.GetOrCreate<GameManager>();
		gameManager.ShowMainMenuOnStart = true;
		
		Log.Info("[MainMenuTest] GameManager created and configured.");
	}

	private void TestUIComponents()
	{
		Log.Info("[MainMenuTest] Testing UI components...");
		
		// This would create a test panel with our components
		// In a real test, you might create a separate test UI panel
		
		var mainMenuManager = Components.GetOrCreate<MainMenuManager>();
		Log.Info("[MainMenuTest] MainMenuManager created.");
		
		// Test showing the menu
		if (mainMenuManager != null)
		{
			mainMenuManager.ShowMainMenu();
			Log.Info("[MainMenuTest] Main menu displayed.");
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		
		// Test keyboard shortcuts
		if (Input.Pressed("F9"))
		{
			Log.Info("[MainMenuTest] F9 pressed - toggling main menu...");
			var mainMenuManager = Components.Get<MainMenuManager>();
			mainMenuManager?.ToggleMainMenu();
		}
		
		if (Input.Pressed("F10"))
		{
			Log.Info("[MainMenuTest] F10 pressed - testing game mode transition...");
			var gameManager = Components.Get<GameManager>();
			if (gameManager?.IsInGameMode == true)
			{
				gameManager.ExitToMainMenu();
			}
			else
			{
				gameManager?.EnterGameMode();
			}
		}
	}
}

/// <summary>
/// Console commands for testing the main menu system
/// </summary>
public static class MainMenuTestCommands
{
	[ConCmd.Server("test_main_menu")]
	public static void TestMainMenu()
	{
		var caller = ConsoleSystem.Caller;
		if (caller == null) return;

		Log.Info("[MainMenuTestCommands] Running main menu test...");
		
		var testComponent = caller.GameObject?.Components.Get<MainMenuTest>();
		if (testComponent == null)
		{
			testComponent = caller.GameObject?.Components.Create<MainMenuTest>();
		}
		
		Log.Info("[MainMenuTestCommands] Test component ready.");
	}
	
	[ConCmd.Server("show_main_menu")]
	public static void ShowMainMenu()
	{
		var caller = ConsoleSystem.Caller;
		if (caller == null) return;

		var mainMenuManager = caller.GameObject?.Components.Get<MainMenuManager>();
		if (mainMenuManager == null)
		{
			mainMenuManager = caller.GameObject?.Components.Create<MainMenuManager>();
		}
		
		mainMenuManager.ShowMainMenu();
		Log.Info("[MainMenuTestCommands] Main menu shown via console command.");
	}
}