using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using ResourceFarmer.Resources;

namespace ResourceFarmer.PlayerBase;

/// <summary>
/// Enhanced inventory component for managing player resources with capacity limits,
/// organization features, and interaction capabilities.
/// </summary>
public sealed class PlayerInventoryComponent : Component
{
	/// <summary>
	/// Reference to the owner player
	/// </summary>
	public Player OwnerPlayer { get; set; }

	/// <summary>
	/// Maximum inventory capacity (total number of items)
	/// </summary>
	[Property, Net] public float MaxCapacity { get; set; } = 1000f;

	/// <summary>
	/// Current total items in inventory
	/// </summary>
	public float CurrentCapacity => OwnerPlayer?.Inventory?.Sum(kvp => kvp.Value) ?? 0f;

	/// <summary>
	/// Available space in inventory
	/// </summary>
	public float AvailableCapacity => MaxCapacity - CurrentCapacity;

	/// <summary>
	/// Whether inventory is full
	/// </summary>
	public bool IsFull => CurrentCapacity >= MaxCapacity;

	/// <summary>
	/// Event triggered when inventory changes
	/// </summary>
	public event Action<IDictionary<ResourceType, float>> OnInventoryChanged;

	/// <summary>
	/// Event triggered when inventory becomes full or has space
	/// </summary>
	public event Action<bool> OnInventoryFullStatusChanged;

	private bool _wasFull = false;

	protected override void OnStart()
	{
		if (OwnerPlayer == null)
			OwnerPlayer = Components.Get<Player>();
		
		if (OwnerPlayer == null)
		{
			Log.Error("[PlayerInventoryComponent] Could not find owner Player component!");
			return;
		}

		// Subscribe to existing inventory updates from ResourceManager
		if (ResourceManager.Instance != null)
		{
			ResourceManager.Instance.OnResourceChanged += OnResourceManagerUpdate;
		}
	}

	protected override void OnDestroy()
	{
		if (ResourceManager.Instance != null)
		{
			ResourceManager.Instance.OnResourceChanged -= OnResourceManagerUpdate;
		}
	}

	private void OnResourceManagerUpdate(IDictionary<ResourceType, float> inventory)
	{
		CheckCapacityStatus();
		OnInventoryChanged?.Invoke(inventory);
	}

	/// <summary>
	/// Adds resources to inventory, respecting capacity limits
	/// </summary>
	/// <param name="resourceType">Type of resource to add</param>
	/// <param name="amount">Amount to add</param>
	/// <returns>Amount actually added</returns>
	public float AddResource(ResourceType resourceType, float amount)
	{
		if (OwnerPlayer?.Inventory == null || resourceType == ResourceType.None || amount <= 0)
			return 0f;

		float availableSpace = AvailableCapacity;
		float actualAmount = Math.Min(amount, availableSpace);

		if (actualAmount <= 0)
		{
			Log.Info($"[PlayerInventoryComponent] Inventory full! Cannot add {amount} {resourceType}");
			return 0f;
		}

		OwnerPlayer.Inventory.TryGetValue(resourceType, out var currentAmount);
		OwnerPlayer.Inventory[resourceType] = currentAmount + actualAmount;

		Log.Info($"[PlayerInventoryComponent] Added {actualAmount:F2} {resourceType}. Total: {OwnerPlayer.Inventory[resourceType]:F2}");

		// Update ResourceManager to trigger UI updates
		ResourceManager.Instance?.UpdateInventory(OwnerPlayer.Inventory);
		CheckCapacityStatus();
		
		return actualAmount;
	}

	/// <summary>
	/// Removes resources from inventory
	/// </summary>
	/// <param name="resourceType">Type of resource to remove</param>
	/// <param name="amount">Amount to remove</param>
	/// <returns>Amount actually removed</returns>
	public float RemoveResource(ResourceType resourceType, float amount)
	{
		if (OwnerPlayer?.Inventory == null || resourceType == ResourceType.None || amount <= 0)
			return 0f;

		if (!OwnerPlayer.Inventory.TryGetValue(resourceType, out var currentAmount))
			return 0f;

		float actualAmount = Math.Min(amount, currentAmount);
		OwnerPlayer.Inventory[resourceType] = currentAmount - actualAmount;

		// Remove entry if amount becomes zero or negative
		if (OwnerPlayer.Inventory[resourceType] <= 0.01f)
		{
			OwnerPlayer.Inventory.Remove(resourceType);
		}

		Log.Info($"[PlayerInventoryComponent] Removed {actualAmount:F2} {resourceType}. Remaining: {OwnerPlayer.Inventory.GetValueOrDefault(resourceType, 0f):F2}");

		// Update ResourceManager to trigger UI updates
		ResourceManager.Instance?.UpdateInventory(OwnerPlayer.Inventory);
		CheckCapacityStatus();

		return actualAmount;
	}

	/// <summary>
	/// Consumes/uses resources from inventory
	/// </summary>
	/// <param name="resourceType">Type of resource to consume</param>
	/// <param name="amount">Amount to consume</param>
	/// <returns>Whether the consumption was successful</returns>
	public bool ConsumeResource(ResourceType resourceType, float amount)
	{
		if (!HasResource(resourceType, amount))
			return false;

		RemoveResource(resourceType, amount);
		return true;
	}

	/// <summary>
	/// Checks if player has enough of a resource
	/// </summary>
	/// <param name="resourceType">Type of resource to check</param>
	/// <param name="amount">Amount to check for</param>
	/// <returns>Whether player has enough resources</returns>
	public bool HasResource(ResourceType resourceType, float amount)
	{
		if (OwnerPlayer?.Inventory == null)
			return false;

		return OwnerPlayer.Inventory.TryGetValue(resourceType, out var currentAmount) && currentAmount >= amount;
	}

	/// <summary>
	/// Gets the amount of a specific resource
	/// </summary>
	/// <param name="resourceType">Type of resource to check</param>
	/// <returns>Current amount of the resource</returns>
	public float GetResourceAmount(ResourceType resourceType)
	{
		if (OwnerPlayer?.Inventory == null)
			return 0f;

		return OwnerPlayer.Inventory.GetValueOrDefault(resourceType, 0f);
	}

	/// <summary>
	/// Gets all resources sorted by type
	/// </summary>
	/// <returns>Dictionary of resources sorted by name</returns>
	public IDictionary<ResourceType, float> GetSortedResources()
	{
		if (OwnerPlayer?.Inventory == null)
			return new Dictionary<ResourceType, float>();

		return OwnerPlayer.Inventory
			.OrderBy(kvp => kvp.Key.ToString())
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	}

	/// <summary>
	/// Gets resources filtered by a predicate
	/// </summary>
	/// <param name="filter">Filter predicate</param>
	/// <returns>Filtered resources</returns>
	public IDictionary<ResourceType, float> GetFilteredResources(Func<KeyValuePair<ResourceType, float>, bool> filter)
	{
		if (OwnerPlayer?.Inventory == null)
			return new Dictionary<ResourceType, float>();

		return OwnerPlayer.Inventory
			.Where(filter)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
	}

	/// <summary>
	/// Clears all resources from inventory
	/// </summary>
	public void ClearInventory()
	{
		if (OwnerPlayer?.Inventory == null)
			return;

		OwnerPlayer.Inventory.Clear();
		ResourceManager.Instance?.UpdateInventory(OwnerPlayer.Inventory);
		CheckCapacityStatus();
	}

	/// <summary>
	/// Upgrades inventory capacity
	/// </summary>
	/// <param name="additionalCapacity">Additional capacity to add</param>
	public void UpgradeCapacity(float additionalCapacity)
	{
		MaxCapacity += additionalCapacity;
		Log.Info($"[PlayerInventoryComponent] Inventory capacity upgraded by {additionalCapacity}. New capacity: {MaxCapacity}");
		CheckCapacityStatus();
	}

	private void CheckCapacityStatus()
	{
		bool isCurrentlyFull = IsFull;
		if (isCurrentlyFull != _wasFull)
		{
			_wasFull = isCurrentlyFull;
			OnInventoryFullStatusChanged?.Invoke(isCurrentlyFull);
			
			if (isCurrentlyFull)
			{
				Log.Info("[PlayerInventoryComponent] Inventory is now full!");
			}
			else
			{
				Log.Info("[PlayerInventoryComponent] Inventory has space available.");
			}
		}
	}
}