using Sandbox;
using System;
using System.Collections.Generic;
using ResourceFarmer.Resources;
using ResourceFarmer.PlayerBase;
namespace ResourceFarmer.UI;
public sealed class WorldPanelManager : Component
{
	// How often to check distances (in seconds)
	[Property] public float UpdateInterval { get; set; } = 0.1f;
	private TimeSince _timeSinceLastUpdate = 0f;


	// Track GameObjects with active panels controlled by this client
	private HashSet<GameObject> _nodesWithActivePanels = new();

	[Property] public int CountActivePanels => _nodesWithActivePanels.Count; // Expose count for debugging


	protected override void OnUpdate()
	{
		// Run only for the client that owns the Player pawn this is attached to
		// or if this is a global client-side manager. Adjust condition if needed.
		if (IsProxy) return; // Ensure this runs on the correct client

		// Throttle the update frequency
		if (_timeSinceLastUpdate < UpdateInterval) return;
		_timeSinceLastUpdate = 0f;
		var localPlayer = GetComponent<Player>(); // Get the Player component

		var playerPos = localPlayer.Transform.Position;

		// Keep track of nodes processed this frame to remove stale entries later
		var processedNodeIds = new HashSet<Guid>();

		var allNodes = Scene.GetAllComponents<ResourceNode>();

		foreach (var node in allNodes)
		{
			if (node == null || !node.IsValid() || !node.Enabled) continue; // Skip invalid nodes

			processedNodeIds.Add(node.GameObject.Id); // Mark this node as processed

			float distance = Vector3.DistanceBetween(playerPos, node.Transform.Position);
			float visibilityRange = node.InteractionRange; // Get range from the node instance

			PanelComponent panel = node.GetPanelComponent(); // Use the accessor from ResourceNode

			if (panel != null && panel.IsValid())
			{
				bool shouldBeVisible = distance <= visibilityRange;

				// --- Control ONLY the Enabled state ---
				if (panel.Enabled != shouldBeVisible)
				{
					panel.Enabled = shouldBeVisible;
					// Log.Info($"Set panel for {node.GameObject.Name} Enabled: {shouldBeVisible}");
				}

				// Track which nodes currently have their panel enabled by this client
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
				// Node used to have a valid panel we tracked, but now it doesn't
				_nodesWithActivePanels.Remove(node.GameObject);
				// Log.Warning($"Panel for {node.GameObject.Name} became invalid, untracking.");
			}
		}

		// --- Cleanup Stale Entries ---
		// Find tracked nodes that were NOT processed this frame (likely destroyed)
		var staleNodes = _nodesWithActivePanels.Where(go => go == null || !go.IsValid || !processedNodeIds.Contains(go.Id)).ToList();
		foreach (var staleNode in staleNodes)
		{
			// Just remove from tracking - no panel to disable if the node is gone/invalid
			_nodesWithActivePanels.Remove(staleNode);
			// Log.Info($"Stopped tracking panel for stale/destroyed node (ID: {staleNode?.Id})");
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
