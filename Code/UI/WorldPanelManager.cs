using Sandbox;
using System;
using System.Collections.Generic;
using ResourceFarmer.Resources;
using ResourceFarmer.PlayerBase;
using ResourceFarmer.UI.Components;
namespace ResourceFarmer.UI;

/// <summary>
/// Legacy WorldPanelManager - now serves as a compatibility layer and delegates to WorldPanelVisibilityManager.
/// This maintains backward compatibility while transitioning to the new modular system.
/// DEPRECATED: Use WorldPanelVisibilityManager directly for new implementations.
/// </summary>
[Obsolete("Use WorldPanelVisibilityManager instead - this is kept for backward compatibility")]
public sealed class WorldPanelManager : Component
{
	/// <summary>
	/// How often to check distances (in seconds)
	/// </summary>
	[Property] public float UpdateInterval { get; set; } = 0.1f;
	
	// Legacy functionality for old ResourceNodePanel system
	private TimeSince _timeSinceLastUpdate = 0f;
	private HashSet<GameObject> _nodesWithActivePanels = new();
	
	// New modular system
	private WorldPanelVisibilityManager _visibilityManager;
	
	[Property] public int CountActivePanels => _nodesWithActivePanels.Count; // Maintain API compatibility
	
	protected override void OnAwake()
	{
		base.OnAwake();
		
		// Create the new visibility manager if it doesn't exist
		_visibilityManager = Components.GetOrCreate<WorldPanelVisibilityManager>();
		_visibilityManager.UpdateInterval = UpdateInterval;
		
		Log.Warning("[WorldPanelManager] This component is deprecated. Use WorldPanelVisibilityManager directly for new implementations.");
	}


	protected override void OnUpdate()
	{
		// Run only for the client that owns the Player pawn this is attached to
		if (IsProxy) return;

		// Throttle the update frequency
		if (_timeSinceLastUpdate < UpdateInterval) return;
		_timeSinceLastUpdate = 0f;
		
		var localPlayer = GetComponent<Player>();
		if (localPlayer == null) return;
		
		var playerPos = localPlayer.Transform.Position;

		// Keep track of nodes processed this frame
		var processedNodeIds = new HashSet<Guid>();
		
		// Handle legacy ResourceNode panels that haven't been converted yet
		var allNodes = Scene.GetAllComponents<ResourceNode>();
		
		foreach (var node in allNodes)
		{
			if (node == null || !node.IsValid() || !node.Enabled) continue;
			
			processedNodeIds.Add(node.GameObject.Id);
			
			// Check if this node is using the new modular system
			var worldPanel = node.GetWorldPanelComponent();
			if (worldPanel != null)
			{
				// Skip - handled by WorldPanelVisibilityManager
				continue;
			}
			
			// Handle legacy ResourceNodePanel system
			float distance = Vector3.DistanceBetween(playerPos, node.Transform.Position);
			float visibilityRange = node.InteractionRange;

			PanelComponent panel = node.GetPanelComponent();

			if (panel != null && panel.IsValid())
			{
				bool shouldBeVisible = distance <= visibilityRange;

				if (panel.Enabled != shouldBeVisible)
				{
					panel.Enabled = shouldBeVisible;
				}

				if (shouldBeVisible)
				{
					_nodesWithActivePanels.Add(node.GameObject);
				}
				else
				{
					_nodesWithActivePanels.Remove(node.GameObject);
				}
			}
			else if (_nodesWithActivePanels.Contains(node.GameObject))
			{
				_nodesWithActivePanels.Remove(node.GameObject);
			}
		}

		// Cleanup stale entries
		var staleNodes = _nodesWithActivePanels.Where(go => go == null || !go.IsValid || !processedNodeIds.Contains(go.Id)).ToList();
		foreach (var staleNode in staleNodes)
		{
			_nodesWithActivePanels.Remove(staleNode);
		}
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		// When this manager component is disabled/destroyed,
		// try to disable any panels it was currently managing.
		foreach (var nodeGo in _nodesWithActivePanels)
		{
			// Need to re-get the component as our dictionary is gone
			if (nodeGo != null && nodeGo.IsValid)
			{
				var node = nodeGo.Components.Get<ResourceNode>();
				var panel = node?.GetPanelComponent();
				if (panel != null && panel.IsValid())
				{
					panel.Enabled = false;
				}
			}
		}
		_nodesWithActivePanels.Clear();
	}
}
