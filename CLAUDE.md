# Claude AI Assistant Instructions - Resource Farmer

## Project Context

Resource Farmer is a multiplayer sandbox game built with S&box, focusing on resource gathering, crafting, and procedural world exploration. The game uses S&box's modern component-based architecture with C# backend and Razor UI.

## S&box Framework Understanding

### Component Architecture

S&box uses a Component-based entity system rather than traditional OOP inheritance:

- `Component` base class (not Unity's MonoBehaviour)
- `GameObject` containers hold multiple components
- Components communicate via `Components.Get<T>()` and `Components.GetOrCreate<T>()`
- Lifecycle methods: `OnStart()`, `OnUpdate()`, `OnDestroy()`

### Networking Model

S&box provides built-in multiplayer networking:

- `[Sync]` attribute automatically replicates properties across clients
- `Networking.IsHost` distinguishes server from clients
- `IsProxy` property indicates client-side component instances
- Server authority model with client prediction

## Resource Farmer Architecture Deep Dive

### Resource Management System

The game's core revolves around a centralized resource system:

**ResourceType Enum** (`Code/Resources/ResourceType.cs`)

- Central registry of all gatherable/craftable materials
- Used consistently across inventory, crafting, and spawning systems
- Examples: Wood, Stone, CopperOre, Quartz, DragonScale

**Inventory System**

- `Dictionary<ResourceType, float>` with [Sync] for network replication
- Floating-point quantities support partial resource collection
- Managed by Player class with automatic persistence

**Resource Nodes & Gathering**

- `IGatherable` interface for all harvestable objects
- `ResourceNode` components spawn procedurally via `ResourceSpawner`
- Terrain-based placement using raycasting for realistic positioning

### Crafting System Architecture

The crafting system uses S&box's GameResource system:

**Recipe Definition**

- `.recipe` files in `Assets/Crafting/` define craftable items
- `CraftingRecipeResource` classes loaded via `ResourceLibrary.GetAll<T>()`
- JSON-based format with ingredients, outputs, and tool requirements

**Tool System**

- `ToolBase` class hierarchy with material/quality/bonus modifiers
- Gathering efficiency calculated via `GetGatherAmountMultiplier()`
- `ToolBonusRegistry` provides configurable bonus effects

### Player Component System

Player functionality is modularized into focused components:

- `PlayerInteractionComponent` - Object interaction logic
- `PlayerGatheringComponent` - Resource collection mechanics
- `PlayerToolComponent` - Equipment management
- Each component requires `OwnerPlayer` reference for functionality

### UI Architecture

Razor-based UI system with component lifecycle management:

- `.razor` + `.razor.scss` component pairs in `Code/UI/`
- `UIManager` handles input-driven panel toggling
- Dynamic instantiation via `Component.Create<T>()`
- Proper disposal prevents memory leaks

## Development Workflows

### Adding New Content

**New Resource Type:**

1. Extend `ResourceType` enum
2. Create gatherable prefab with `ResourceNode` component
3. Add to `ResourceSpawner.ResourcePrefabs` list
4. Update relevant crafting recipes

**New Crafting Recipe:**

1. Create `.recipe` file in `Assets/Crafting/`
2. Define ingredients, output, and tool requirements
3. `RecipeManager` automatically loads via `ResourceLibrary`
4. Test via in-game crafting UI

**New Player Functionality:**

1. Create component inheriting from `Component`
2. Add to Player in `OnStart()` with `Components.GetOrCreate<T>()`
3. Set `OwnerPlayer` reference for context
4. Implement networking with `[Sync]` as needed

## Critical S&box Patterns

### Asset Loading

```csharp
// Correct: Use ResourceLibrary for GameResource assets
var recipes = ResourceLibrary.GetAll<CraftingRecipeResource>();

// Incorrect: Don't use file I/O for game assets
var json = File.ReadAllText("recipes.json"); // ❌
```

### Network-Safe Logic

```csharp
protected override void OnStart()
{
    // Server-only logic
    if (Networking.IsHost)
    {
        SpawnResources();
        LoadPlayerData();
    }

    // Client-safe logic
    if (!IsProxy)
    {
        SetupUI();
    }
}
```

### Component Communication

```csharp
// Preferred: Direct component access
var gathering = Components.Get<PlayerGatheringComponent>();
gathering?.StartGathering(resourceNode);

// Alternative: Component creation with setup
var interaction = Components.GetOrCreate<PlayerInteractionComponent>();
if (interaction != null) interaction.OwnerPlayer = this;
```

## Common Issues & Solutions

### Multiplayer Desync

- Always check `IsProxy` before executing logic that should only run on authoritative instances
- Use `[Sync]` for properties that need network replication
- Server/host should handle spawning and persistence logic

### Performance Considerations

- Terrain bounds must be calculated before resource spawning
- UI panels should be properly disposed to prevent memory leaks
- Use efficient component lookup patterns in Update() loops

### S&box Specific Gotchas

- `.sbproj` files configure S&box projects (not `.csproj` for game logic)
- Scene system requires `scenes/minimal.scene` as startup scene
- `sturnus.terraingenerationtool` dependency for procedural terrain

## Code Style Conventions

### Namespace Organization

```csharp
using ResourceFarmer.PlayerBase;    // Player-related functionality
using ResourceFarmer.Resources;     // Resource and inventory systems
using ResourceFarmer.Crafting;      // Recipe and crafting logic
using ResourceFarmer.Items;         // Tools and equipment
```

### Property Attribute Patterns

```csharp
[Property, Category("Display")] public string Name { get; set; }
[Sync] public float Money { get; set; }                    // Network replicated
[Property, Group("Interaction")] public bool Enabled { get; set; }
```

### Global Usings (Assembly.cs)

```csharp
global using Sandbox;                    // S&box framework
global using System.Collections.Generic; // Common collections
global using System.Linq;               // LINQ operations
```

## Testing & Debugging

- Use S&box's built-in logging: `Log.Info()`, `Log.Warning()`, `Log.Error()`
- Multiplayer testing requires host/client separation awareness
- Scene-based testing via `scenes/minimal.scene`
- Component inspection through S&box editor tools

When working on Resource Farmer, prioritize understanding the component relationships and networking implications of any changes. The modular architecture allows for clean feature additions while maintaining multiplayer stability.
