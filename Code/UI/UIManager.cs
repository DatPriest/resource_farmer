using Sandbox;
using Sandbox.UI;
using ResourceFarmer.UI;

public sealed class UIManager : Component
{
	[Property] public string CraftingToggleAction { get; set; } = "CraftingMenu";
	[Property] public string SettingsToggleAction { get; set; } = "SettingsMenu";
	[Property] public string InventoryToggleAction { get; set; } = "InventoryMenu"; // New input action

	private CraftingRootPanel _craftingPanelInstance;
	private Settings _settingsPanelInstance;
	private InventoryPanel _inventoryPanelInstance; // Add instance variable for inventory

	protected override void OnUpdate()
	{
		if (Network.IsProxy) return;

		if (Input.Pressed(CraftingToggleAction))
		{
			Log.Info("[UIManager] Crafting toggle action pressed.");
			ToggleCraftingPanel();
			// Close other panels if crafting is opened
			if (_craftingPanelInstance != null) 
			{
				CloseSettingsPanel();
				CloseInventoryPanel();
			}
		}

		if (Input.Pressed(SettingsToggleAction)) // Check for settings toggle
		{
			Log.Info("[UIManager] Settings toggle action pressed.");
			ToggleSettingsPanel();
			// Close other panels if settings is opened
			if (_settingsPanelInstance != null) 
			{
				CloseCraftingPanel();
				CloseInventoryPanel();
			}
		}

		if (Input.Pressed(InventoryToggleAction)) // Check for inventory toggle
		{
			Log.Info("[UIManager] Inventory toggle action pressed.");
			ToggleInventoryPanel();
			// Close other panels if inventory is opened
			if (_inventoryPanelInstance != null)
			{
				CloseCraftingPanel();
				CloseSettingsPanel();
			}
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

	public void ToggleInventoryPanel() // New method for inventory
	{
		if (_inventoryPanelInstance != null && _inventoryPanelInstance.IsValid())
		{
			CloseInventoryPanel();
		}
		else
		{
			_inventoryPanelInstance = Components.Create<InventoryPanel>();
			_inventoryPanelInstance.ShowInventory();
			Log.Info("[UIManager] Inventory panel opened.");
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

	private void CloseInventoryPanel()
	{
		if (_inventoryPanelInstance != null && _inventoryPanelInstance.IsValid())
		{
			_inventoryPanelInstance.CloseInventory();
			_inventoryPanelInstance.Destroy();
			_inventoryPanelInstance = null;
			Log.Info("[UIManager] Inventory panel closed.");
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
		else if (panel == _inventoryPanelInstance) // Handle inventory panel closing  
		{
			_inventoryPanelInstance = null;
			Log.Info("[UIManager] Inventory panel self-closed.");
		}
	}
}
