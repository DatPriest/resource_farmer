using Sandbox;
using Sandbox.UI;
using ResourceFarmer.UI;

public sealed class UIManager : Component
{
	[Property] public string CraftingToggleAction { get; set; } = "CraftingMenu";
	[Property] public string SettingsToggleAction { get; set; } = "SettingsMenu"; // New input action

	private CraftingRootPanel _craftingPanelInstance;
	private Settings _settingsPanelInstance; // Add instance variable for settings

	protected override void OnUpdate()
	{
		if (Network.IsProxy) return;

		if (Input.Pressed(CraftingToggleAction))
		{
			Log.Info("[UIManager] Crafting toggle action pressed.");
			ToggleCraftingPanel();
			// Close settings if crafting is opened
			if (_craftingPanelInstance != null) CloseSettingsPanel();
		}

		if (Input.Pressed(SettingsToggleAction)) // Check for settings toggle
		{
			Log.Info("[UIManager] Settings toggle action pressed.");
			ToggleSettingsPanel();
			// Close crafting if settings is opened
			if (_settingsPanelInstance != null) CloseCraftingPanel();
		}
	}

	public void ToggleCraftingPanel()
	{
		if (_craftingPanelInstance != null && _craftingPanelInstance.IsValid())
		{
			CloseCraftingPanel();
		}
		else
		{
			_craftingPanelInstance = Components.Create<CraftingRootPanel>();
			Log.Info("[UIManager] Crafting panel opened.");
		}
	}

	public void ToggleSettingsPanel() // New method for settings
	{
		if (_settingsPanelInstance != null && _settingsPanelInstance.IsValid())
		{
			CloseSettingsPanel();
		}
		else
		{
			_settingsPanelInstance = Components.Create<Settings>();
			Log.Info("[UIManager] Settings panel opened.");
		}
	}

	// Helper methods to explicitly close panels
	private void CloseCraftingPanel()
	{
		if (_craftingPanelInstance != null && _craftingPanelInstance.IsValid())
		{
			_craftingPanelInstance.Destroy();
			_craftingPanelInstance = null;
			Log.Info("[UIManager] Crafting panel closed.");
		}
	}

	private void CloseSettingsPanel()
	{
		if (_settingsPanelInstance != null && _settingsPanelInstance.IsValid())
		{
			_settingsPanelInstance.Destroy();
			_settingsPanelInstance = null;
			Log.Info("[UIManager] Settings panel closed.");
		}
	}

	// Optional: Method for panels to call when they close themselves
	public void NotifyPanelClosed(PanelComponent panel)
	{
		if (panel == _craftingPanelInstance)
		{
			_craftingPanelInstance.Destroy();
			_craftingPanelInstance = null;
			Log.Info("[UIManager] Crafting panel self-closed.");
			
		}
		else if (panel == _settingsPanelInstance) // Handle settings panel closing
		{
			_settingsPanelInstance = null;
			Log.Info("[UIManager] Settings panel self-closed.");
		}
	}
}
