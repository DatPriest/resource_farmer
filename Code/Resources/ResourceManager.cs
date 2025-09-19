using Sandbox;
using System;
using System.Collections.Generic;
namespace ResourceFarmer.Resources;
public sealed class ResourceManager : Component
{
	public static ResourceManager Instance { get; private set; }

	public event Action<IDictionary<ResourceType, float>> OnResourceChanged;

	public IDictionary<ResourceType, float> CurrentInventory { get; private set; } = new Dictionary<ResourceType, float>();

	protected override void OnAwake()
	{
		Instance = this;
	}

	public void UpdateInventory(IDictionary<ResourceType, float> inventory)
	{
		CurrentInventory = new Dictionary<ResourceType, float>(inventory);
		OnResourceChanged?.Invoke(CurrentInventory);
	}
}
