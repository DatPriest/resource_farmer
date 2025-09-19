using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceFarmer.UI.Components;

/// <summary>
/// Universal manager for world panel visibility based on player proximity.
/// Works with any WorldPanelComponent, not just ResourceNode panels.
/// Should be attached to Player GameObjects.
/// </summary>
public sealed class WorldPanelVisibilityManager : Component
{
	/// <summary>
	/// How often to check distances (in seconds)
	/// </summary>
	[Property] public float UpdateInterval { get; set; } = 0.1f;
	
	/// <summary>
	/// Maximum range to consider for panel visibility (performance optimization)
	/// </summary>
	[Property] public float MaxScanRange { get; set; } = 500f;
	
	/// <summary>
	/// Whether to show debug information about panel management
	/// </summary>
	[Property] public bool EnableDebugLogging { get; set; } = false;
	
	private TimeSince _timeSinceLastUpdate = 0f;
	
	// Track panels currently managed by this player
	private HashSet<WorldPanelComponent> _managedPanels = new();
	
	// Cache for efficient lookups
	private List<WorldPanelComponent> _allPanelsCache = new();
	private TimeSince _timeSinceCacheRefresh = 0f;
	private const float CacheRefreshInterval = 1f; // Refresh cache every second
	
	[Property] public int ManagedPanelCount => _managedPanels.Count; // Expose for debugging
	
	protected override void OnUpdate()
	{
		// Only run for local players (not proxies)
		if (IsProxy) return;
		
		// Throttle the update frequency
		if (_timeSinceLastUpdate < UpdateInterval) return;
		_timeSinceLastUpdate = 0f;
		
		// Get the player position
		var playerPos = Transform.Position;
		
		// Refresh panel cache periodically for performance
		RefreshPanelCacheIfNeeded();
		
		// Track panels processed this frame
		var processedPanels = new HashSet<WorldPanelComponent>();
		
		// Check all world panels in the scene
		foreach (var panel in _allPanelsCache)
		{
			if (panel == null || !panel.IsValid() || !panel.GameObject.IsValid())
				continue;
				
			processedPanels.Add(panel);
			
			// Calculate distance to this panel
			float distance = Vector3.DistanceBetween(playerPos, panel.TargetObject.Transform.Position);
			bool shouldBeVisible = distance <= panel.VisibilityRange && distance <= MaxScanRange;
			
			// Update panel visibility state
			bool wasManaged = _managedPanels.Contains(panel);
			
			if (shouldBeVisible)
			{
				panel.UpdateVisibility(true, GameObject, distance);
				_managedPanels.Add(panel);
				
				if (!wasManaged && EnableDebugLogging)
					Log.Info($"[WorldPanelVisibilityManager] Started managing panel on {panel.TargetObject.Name} (distance: {distance:F1})");
			}
			else if (wasManaged)
			{
				panel.UpdateVisibility(false, null, float.MaxValue);
				_managedPanels.Remove(panel);
				
				if (EnableDebugLogging)
					Log.Info($"[WorldPanelVisibilityManager] Stopped managing panel on {panel.TargetObject.Name}");
			}
		}
		
		// Clean up stale references
		CleanupStalePanels(processedPanels);
	}
	
	/// <summary>
	/// Refreshes the cached list of all world panels in the scene
	/// </summary>
	private void RefreshPanelCacheIfNeeded()
	{
		if (_timeSinceCacheRefresh < CacheRefreshInterval) return;
		_timeSinceCacheRefresh = 0f;
		
		var oldCount = _allPanelsCache.Count;
		_allPanelsCache.Clear();
		_allPanelsCache.AddRange(Scene.GetAllComponents<WorldPanelComponent>());
		
		if (EnableDebugLogging && _allPanelsCache.Count != oldCount)
			Log.Info($"[WorldPanelVisibilityManager] Panel cache refreshed: {oldCount} -> {_allPanelsCache.Count} panels");
	}
	
	/// <summary>
	/// Removes invalid panels from our managed set
	/// </summary>
	private void CleanupStalePanels(HashSet<WorldPanelComponent> processedPanels)
	{
		var stalePanels = _managedPanels.Where(p => 
			p == null || !p.IsValid() || !p.GameObject.IsValid() || !processedPanels.Contains(p)
		).ToList();
		
		foreach (var stalePanel in stalePanels)
		{
			_managedPanels.Remove(stalePanel);
			if (EnableDebugLogging)
				Log.Info($"[WorldPanelVisibilityManager] Removed stale panel reference");
		}
	}
	
	protected override void OnDisabled()
	{
		base.OnDisabled();
		
		// Disable all panels we were managing when this manager is disabled
		foreach (var panel in _managedPanels)
		{
			if (panel != null && panel.IsValid())
			{
				panel.UpdateVisibility(false, null, float.MaxValue);
			}
		}
		
		_managedPanels.Clear();
		
		if (EnableDebugLogging)
			Log.Info($"[WorldPanelVisibilityManager] Disabled, cleared all managed panels");
	}
	
	/// <summary>
	/// Manually refresh the panel cache (useful for debugging or when panels are created dynamically)
	/// </summary>
	public void ForceRefreshPanelCache()
	{
		_timeSinceCacheRefresh = CacheRefreshInterval; // Force refresh on next update
	}
	
	/// <summary>
	/// Get all panels currently managed by this visibility manager
	/// </summary>
	public IReadOnlyCollection<WorldPanelComponent> GetManagedPanels()
	{
		return _managedPanels;
	}
}