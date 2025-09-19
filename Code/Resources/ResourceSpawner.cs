using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using ResourceFarmer.Resources; // Make sure this namespace is correct for ResourceType and ResourceNode

namespace ResourceFarmer.Resources;

/// <summary>
/// This component randomly spawns specified resource prefabs onto a target terrain surface.
/// It finds random locations within the terrain bounds and uses raycasting to place
/// the prefabs accurately on the ground. Includes functionality to regenerate resources.
/// Spawned resources are parented under 'Environment/ResourceNodes'.
/// </summary>
public sealed class ResourceSpawner : Component
{
    /// <summary>
    /// List of GameObjects (prefabs) to be randomly spawned as resources.
    /// Assign these prefabs in the editor.
    /// </summary>
    [Property] public List<GameObject> ResourcePrefabs { get; set; } = new();

    /// <summary>
    /// The total number of resource instances to attempt spawning.
    /// </summary>
    [Property] public int NumberToSpawn { get; set; } = 50;

    /// <summary>
    /// Reference to the Terrain component where resources should be spawned.
    /// Assign this in the editor by dragging the Terrain GameObject here.
    /// </summary>
    [Property] public Terrain TargetTerrain { get; set; }

    private BBox _terrainBounds;
    private List<GameObject> _spawnedResources = new(); // Keep track of spawned resources
    private GameObject _resourceNodeParent; // Parent GameObject for spawned resources

    protected override void OnStart()
    {
        // Spawning should typically be handled by the server/host in a networked game.
        if (IsProxy) return;

        // Find the parent GameObject for resources
        FindResourceNodeParent();

        // Initial setup and spawn
        if (SetupBounds())
        {
            RegenerateResources();
        }
    }

    /// <summary>
    /// Finds the GameObject named "ResourceNodes" under "Environment".
    /// </summary>
    private void FindResourceNodeParent()
    {
        var environmentGo = Scene.Children.FirstOrDefault(go => go.Name == "Environment");
        if (environmentGo == null)
        {
            Log.Warning($"[{nameof(ResourceSpawner)}] Could not find 'Environment' GameObject in the scene root.");
            return;
        }

        _resourceNodeParent = environmentGo.Children.FirstOrDefault(go => go.Name == "ResourceNodes");
        if (_resourceNodeParent == null)
        {
            Log.Warning($"[{nameof(ResourceSpawner)}] Could not find 'ResourceNodes' GameObject under 'Environment'. Spawned resources will be placed in the scene root.");
        }
        else
        {
            Log.Info($"[{nameof(ResourceSpawner)}] Found 'Environment/ResourceNodes' parent for spawned resources.");
        }
    }

    /// <summary>
    /// Calculates the terrain bounds. Returns true if successful, false otherwise.
    /// </summary>
    private bool SetupBounds()
    {
        if (TargetTerrain == null)
        {
            Log.Error($"[{nameof(ResourceSpawner)}] Target Terrain is not assigned. Cannot calculate bounds.");
            return false;
        }

        // Get the size and height directly from the Terrain component properties.
        float terrainSize = TargetTerrain.TerrainSize; // Uniform world size (width/length)
        float terrainHeight = TargetTerrain.TerrainHeight; // Maximum world height

        // Define the bounding box based on terrain dimensions. Assumes terrain is centered at its local origin.
        _terrainBounds = new BBox(
            new Vector3(-terrainSize / 2, -terrainSize / 2, 0), // Mins
            new Vector3(terrainSize / 2, terrainSize / 2, terrainHeight) // Maxs
        );
        // Transform bounds to world space if the terrain object itself is moved/rotated
        _terrainBounds = _terrainBounds.Transform(TargetTerrain.Transform.World);

        Log.Info($"[{nameof(ResourceSpawner)}] Calculated Terrain Bounds. World Bounds: {_terrainBounds}");
        return true;
    }

    /// <summary>
    /// Cleans up previously spawned resources and spawns a new set.
    /// Can be called manually or via other scripts to regenerate resources.
    /// </summary>
    [Button("Regenerate Now")] // Add this attribute to create a button in the inspector
    public async void RegenerateResources()
    {
        if (!Networking.IsHost) return;

        // --- Cleanup Existing Resources ---
        CleanupResources();

        // --- Validation ---
        if (TargetTerrain == null)
        {
            Log.Error($"[{nameof(ResourceSpawner)}] Target Terrain is not assigned. Cannot spawn resources.");
            return;
        }

        if (ResourcePrefabs == null || ResourcePrefabs.Count == 0)
        {
            Log.Error($"[{nameof(ResourceSpawner)}] Resource Prefabs list is empty. Add prefabs to spawn.");
            return;
        }

        // --- Ensure Parent Exists (might have been deleted or scene changed) ---
        if (_resourceNodeParent == null || !_resourceNodeParent.IsValid)
        {
            FindResourceNodeParent(); // Try to find it again
        }

        // --- Recalculate Bounds (in case terrain moved or properties changed) ---
        if (!SetupBounds()) return; // Stop if bounds calculation fails

        // --- Start Spawning ---
        Log.Info($"[{nameof(ResourceSpawner)}] Attempting to spawn {NumberToSpawn} resources...");

        int spawnedCount = 0;
        int attempts = 0;
        int maxAttempts = NumberToSpawn * 10; // Prevent infinite loops

        while (spawnedCount < NumberToSpawn && attempts < maxAttempts)
        {
            attempts++;

            float randomX = Game.Random.Float(_terrainBounds.Mins.x, _terrainBounds.Maxs.x);
            float randomY = Game.Random.Float(_terrainBounds.Mins.y, _terrainBounds.Maxs.y);

            Vector3 rayStart = new Vector3(randomX, randomY, _terrainBounds.Maxs.z + 50f);
            Vector3 rayEnd = new Vector3(randomX, randomY, _terrainBounds.Mins.z - 1f);

            var tr = Scene.Trace.Ray(rayStart, rayEnd)
                                .WithoutTags("player", "trigger")
                                .Run();

            if (tr.Hit && tr.GameObject == TargetTerrain.GameObject)
            {
                Vector3 spawnPosition = tr.EndPosition;
                int prefabIndex = Game.Random.Int(0, ResourcePrefabs.Count - 1);
                GameObject selectedPrefabSource = ResourcePrefabs[prefabIndex];

                if (selectedPrefabSource == null)
                {
                    Log.Warning($"[{nameof(ResourceSpawner)}] Prefab at index {prefabIndex} is null. Skipping.");
                    continue;
                }

                // Clone the prefab at the spawn position, initially parented to the scene root
                GameObject spawnedObject = selectedPrefabSource.Clone(spawnPosition);

                // Set the parent if found
                if (_resourceNodeParent.IsValid)
                {
                    spawnedObject.SetParent(_resourceNodeParent, false); // worldPositionStays = false to keep world position
                }
                else
                {
                    Log.Trace($"[{nameof(ResourceSpawner)}] ResourceNode parent not found or invalid, spawning '{spawnedObject.Name}' in scene root.");
                }

                spawnedObject.NetworkSpawn(); // Network spawn after setting parent if needed
                spawnedObject.Transform.Rotation = Rotation.FromYaw(Game.Random.Float(0, 360));

                var resourceNode = spawnedObject.Components.GetOrCreate<ResourceNode>();
                if (resourceNode != null)
                {
                    // Determine resource type from prefab name
                    ResourceType resourceType = DetermineResourceTypeFromPrefab(selectedPrefabSource);
                    // Get spawn probability based on resource type
                    float spawnChance = GetSpawnProbabilityForResource(resourceType);
                    // Roll for spawn chance; skip spawning if chance not met
                    if (Game.Random.Float(0, 1) > spawnChance)
                    {
                        spawnedObject.Destroy();
                        continue;
                    }

                    resourceNode.ResourceType = resourceType;
                    resourceNode.RequiredToolType = DetermineRequiredToolType(resourceType);
                }

                _spawnedResources.Add(spawnedObject); // Track the spawned object
                spawnedCount++;
				await Task.Delay( 25 );

			}
        }

        Log.Info($"[{nameof(ResourceSpawner)}] Spawning complete. Successfully spawned {spawnedCount} resources after {attempts} attempts.");
    }

    /// <summary>
    /// Destroys all resources previously spawned by this component.
    /// </summary>
    private void CleanupResources()
    {
        if (IsProxy) return;

        Log.Info($"[{nameof(ResourceSpawner)}] Cleaning up {_spawnedResources.Count} previously spawned resources...");
        foreach (var resource in _spawnedResources)
        {
            if (resource.IsValid) // Check if it hasn't been destroyed already
            {
                resource.Destroy();
            }
        }
        _spawnedResources.Clear(); // Clear the tracking list
    }

    /// <summary>
    /// Example helper function to determine the ResourceType based on the prefab.
    /// Implement your own logic here based on how your prefabs are set up.
    /// </summary>
    /// <param name="prefab">The resource prefab.</param>
    /// <returns>The determined ResourceType.</returns>
    private ResourceType DetermineResourceTypeFromPrefab(GameObject prefab)
    {
        // Example Logic: Check prefab name
        if (prefab.Name.Contains("Tree", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceType.Wood;
        }
        if (prefab.Name.Contains("Rock", StringComparison.OrdinalIgnoreCase) || prefab.Name.Contains("Stone", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceType.Stone;
        }
        if (prefab.Name.Contains("Copper", StringComparison.OrdinalIgnoreCase))
        {
            return ResourceType.CopperOre;
        }

        // Add more checks for other resource types...

        Log.Warning($"[{nameof(ResourceSpawner)}] Could not determine resource type for prefab '{prefab.Name}'. Defaulting to Wood.");
        return ResourceType.Wood; // Default if type cannot be determined
    }

    /// <summary>
    /// Returns a spawn probability for the given resource type.
    /// Low tier materials have a higher chance to spawn.
    /// </summary>
    /// <param name="resourceType">The resource type.</param>
    /// <returns>A float between 0 and 1 representing the spawn probability.</returns>
    private float GetSpawnProbabilityForResource(ResourceType resourceType)
    {
        switch (resourceType)
        {
            case ResourceType.Wood:
            case ResourceType.Stone:
            case ResourceType.Fiber:
                return 0.8f;
            case ResourceType.CopperOre:
            case ResourceType.TinOre:
            case ResourceType.IronOre:
            case ResourceType.Coal:
            case ResourceType.SilverOre:
            case ResourceType.GoldOre:
            case ResourceType.MithrilOre:
                return 0.5f;
            case ResourceType.AdamantiteOre:
            case ResourceType.Quartz:
            case ResourceType.RubyRough:
            case ResourceType.SapphireRough:
            case ResourceType.EmeraldRough:
            case ResourceType.DiamondRough:
            case ResourceType.EssenceDust:
            case ResourceType.CrystalShard:
            case ResourceType.DragonScale:
            case ResourceType.PhoenixFeather:
                return 0.3f;
            default:
                return 0.5f;
        }
    }

	/// <summary>
	/// Determines the required tool type for gathering the resource based on its type.
	/// </summary>
	/// <param name="resourceType">The resource type.</param>
	/// <returns>A ResourceType representing the required tool type.</returns>
	private ResourceType DetermineRequiredToolType( ResourceType resourceType )
	{
		switch ( resourceType )
		{
			case ResourceType.Wood:
			case ResourceType.Fiber:
				return ResourceType.None; 
			case ResourceType.Stone:
			case ResourceType.CopperOre:
				return ResourceType.Stone;
			case ResourceType.TinOre:
			case ResourceType.IronOre:
			case ResourceType.Coal:
			case ResourceType.SilverOre:
			case ResourceType.GoldOre:
			case ResourceType.MithrilOre:
			case ResourceType.AdamantiteOre:
				return ResourceType.CopperOre;


			case ResourceType.Quartz:
			case ResourceType.RubyRough:
			case ResourceType.SapphireRough:
			case ResourceType.EmeraldRough:
			case ResourceType.DiamondRough:
			case ResourceType.EssenceDust:
			case ResourceType.CrystalShard:
			case ResourceType.DragonScale:
			case ResourceType.PhoenixFeather:
				return ResourceType.EssenceDust; 

			default:
				return ResourceType.None; 
		}
	}

	// Optional: Ensure cleanup when the component is destroyed or disabled
	protected override void OnDestroy()
    {
        CleanupResources();
    }

    protected override void OnDisabled()
    {
        CleanupResources();
    }
}
