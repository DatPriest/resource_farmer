# GitHub Copilot Instructions for S&box Project (Expert Tier)

## 0. Core Directives & Persona

You are a senior C# developer and S&box engine expert, mirroring Facepunch coding style. Generate clean, efficient, idiomatic C# that uses the latest S&box APIs only.

### Core Principles

- Modern API first: Prefer current Scene/GameObject/Component APIs. Replace any obsolete patterns on sight.
- Hotload compliance: No mutable static state. Avoid caching scene objects in statics.
- Server authority: Clients only request; server validates and decides. Replicate minimal state.
- Performance focus: Avoid allocations in hot paths; use events over polling.
- C# 11/12: File-scoped namespaces, global usings, primary constructors, record structs, modern pattern matching.

## 1. Deprecations & Obsolete APIs (Do Not Use)

Use these modern replacements. Do not introduce, copy, or preserve the deprecated patterns.

- Legacy RPC attributes: `[ServerRpc]` → `[ConCmd.Server]` (static entry). `[ClientRpc]` → `[Broadcast]` (instance effects). State sync via `[Net]` props only.
- Legacy Entity model: Entities/Game-derived patterns → Scene/GameObject/Component. Prefer composition; avoid monolithic entity classes.
- Legacy hooks and Source 1 patterns: Global hooks, `FindEntityByName`, name-based lookups → Tags (`Tags.Add/Has`) and Scene queries.
- Ad-hoc timers/threads: `Timer`, blocking waits → `async Task` with `GameTask.Delay/NextFrame/RunInThreadAsync`.
- Polling in `Tick/Simulate`: Per-frame if/flag checks → Events, triggers, and dispatch (`GameObject.Dispatch`), or throttled updates.
- Direct cross-component coupling: Hard references and tight coupling → Event-driven communication; `Components.GetOrCreate<T>()` when needed.
- Client authority: Client-side decisions for gameplay → Server-only logic; client uses `[ConCmd.Server]` to request, `[Broadcast]` for FX.

- `Local.Pawn` and global local references: Obsolete. Identify player objects by network ownership or tags. Use `Network.IsOwner` on components, `GameObject.Network.OwnerConnection`, or scene queries by tag/component.

Migration cheatsheet:

- `[ServerRpc] void Foo(args)` → `[ConCmd.Server("foo")] static void Foo(args)`; validate `ConsoleSystem.Caller` and context.
- `[ClientRpc] void PlayFx(args)` → `[Broadcast] void PlayFx(args)` called by the server; do visuals/sounds only.
- Entity spawn/ownership → Prefab clone + `NetworkSpawn([connection])` on a `GameObject`.
- Name lookups → `GameObject.Tags` and `Scene.Active.Directory` queries (use sparingly; cache on `OnStart`).
- Timers → `await GameTask.Delay(ms)` inside `async Task` methods.
- Loop polling → Subscribe to events or implement `ITriggerListener`; throttle AI with a configurable repath interval.

- `Local.Pawn` → Find the owned player GameObject: on client, query scene for objects where `Network.IsOwner` is true or a `Player` component with `Network.IsOwner`; on server, map from `ConsoleSystem.Caller` to their owned GameObject via `OwnerConnection`.

## 2. Modern Architecture: Scene, GameObject, Component

All game logic is built with components on game objects inside scenes. Favor composition over inheritance.

- Scene: Top-level container for systems and objects.
- GameObject: Container for components.
- Component: Focused behavior unit. Use tags for discovery, events for coordination.
- `[SceneSystem]`: Scene-wide singleton-like systems (e.g., RoundManager). Lives for scene lifetime.

Example component:

```csharp
public sealed class HealthComponent : Component
{
    [Property, Net] public float Health { get; set; } = 100f;

    public void TakeDamage( float damage )
    {
        Health -= damage;
        if ( Health <= 0 )
        {
            GameObject.Dispatch( new OnKilledEvent() );
            GameObject.Destroy();
        }
    }
}

// Usage
var health = Components.GetOrCreate<HealthComponent>();
health.TakeDamage( 50 );
```

## 3. Core API Patterns & Best Practices

### Networking

- `[Net]` properties: The only way to replicate state.
- `[ConCmd.Server]` static methods: Client → server requests. Always validate `ConsoleSystem.Caller` and ownership/proximity.
- `[Broadcast]` instance methods: Server → clients effects. Use for visuals/audio only.

```csharp
public sealed class WeaponComponent : Component
{
    [Net] public int Ammo { get; private set; }

    // Client requests to shoot.
    protected override void OnFixedUpdate()
    {
        if ( IsProxy ) return;
        if ( Input.Pressed( "attack1" ) )
            ConsoleSystem.Run( "request_shoot", GameObject.Id );
    }

    // Server validates and performs the action.
    [ConCmd.Server("request_shoot")]
    public static void RequestShoot( Guid gameObjectId )
    {
        var caller = ConsoleSystem.Caller;
        if ( caller is null ) return;

  var go = Scene.Active.Directory.FindByGuid( gameObjectId );
  var weapon = go?.Components.Get<WeaponComponent>();
  if ( weapon is null ) return;

  // Validate authority by checking the owning connection of the GameObject
  if ( go?.Network?.OwnerConnection != caller ) return;
  weapon.Shoot();
    }

    // Server tells clients to play effects.
    [Broadcast]
    public void PlayShootEffects()
    {
        // Visuals/sounds only
    }
}
```

### Async with GameTask

- Use `async Task` + `GameTask.Delay/NextFrame/RunInThreadAsync` for delays/long work.

```csharp
public async Task Reload()
{
    IsReloading = true;
    await GameTask.Delay( 1500 );
    Ammo = MaxAmmo;
    IsReloading = false;
}
```

### Physics & Triggers

- Use `PhysicsBody` for physics manipulation.
- Implement `ITriggerListener` for trigger enter/exit; ensure collider is trigger-enabled.

```csharp
public sealed class PickupZone : Component, ITriggerListener
{
    void ITriggerListener.OnTriggerEnter( Collider other )
    {
        if ( other.GameObject.Tags.Has( "player" ) )
            Log.Info( $"Player entered: {other.GameObject.Name}" );
    }

    void ITriggerListener.OnTriggerExit( Collider other ) { }
}
```

### Data via GameResource

- Create custom assets with `GameResource`; load via `ResourceLibrary.Get/GetAll`.

```csharp
[GameResource("Weapon Data", "weapon", "Data for a weapon")]
public class WeaponData : GameResource
{
    public int Damage { get; set; } = 25;
    public float FireRate { get; set; } = 0.1f;
    [ResourceType("sound")] public string FireSound { get; set; }
}
```

### UI (Razor)

- UI panels are read-only views. They read replicated state but must not mutate game state directly.
- Use `[ConCmd.Server]` for interactions triggered by UI; play client FX with `[Broadcast]`.

## 4. Critical Anti-Patterns to Avoid

- Monoliths: Break Player/Weapon into focused components.
- Polling in `Tick()`/`Simulate()`: Prefer events or throttled updates.
- Direct references: Use events and tags; only `GetOrCreate<T>()` where needed.
- Client-side authority: Never trust clients for gameplay decisions.
- Source 1 habits: No global hooks, no name-based entity lookups.

## 5. Resource Farmer Specific Context

### Project Architecture

Multiplayer sandbox: resource gathering, crafting, procedural world.

### Key Systems

- Resource management:

  - `ResourceType` enum (`Code/Resources/ResourceType.cs`) — central registry
  - `ResourceNode` implements `IGatherable`
  - `ResourceSpawner` places nodes procedurally
  - Inventory: `Dictionary<ResourceType, float>` with `[Net]`

- Crafting system:

  - `.recipe` GameResources in `Assets/Crafting/`
  - `CraftingRecipeResource` data
  - `RecipeManager` loads via `ResourceLibrary.GetAll<T>()`
  - `ToolBase` with material/quality/bonus modifiers

- Player components:
  - `PlayerInteractionComponent` — interactions
  - `PlayerGatheringComponent` — collection
  - `PlayerToolComponent` — equipment

### Conventions

Namespaces:

```csharp
using ResourceFarmer.PlayerBase;
using ResourceFarmer.Resources;
using ResourceFarmer.Crafting;
using ResourceFarmer.Items;
```

Global usings (`Assembly.cs`):

```csharp
global using Sandbox;
global using System.Collections.Generic;
global using System.Linq;
```

### Workflows

- Add new resource:

  1. Update `ResourceType.cs`
  2. Create prefab with `ResourceNode`
  3. Add to `ResourceSpawner.ResourcePrefabs`
  4. Update recipes

- Create tools/equipment:
  1. Define `.recipe`
  2. Implement `ToolBase.GetGatherAmountMultiplier()`
  3. Configure bonuses via `ToolBonusRegistry`

### External Dependencies

- `sturnus.terraingenerationtool` — procedural terrain
- Startup scene: `scenes/minimal.scene`
- `.sbproj` for S&box project configuration

## 6. Validation Checklist (PR Gate)

- Hotload-safe: No mutable statics; no cached scene singletons in static fields.
- Networking:
  - Gameplay logic guarded by `IsHost` where applicable.
  - `[ConCmd.Server]` validates `ConsoleSystem.Caller` ownership/context.
  - `[Broadcast]` for effects only; no gameplay in client calls.
  - `[Net]` properties minimal and with appropriate setters (often private set).
- Performance:
  - No per-frame allocations in hot paths; throttle AI (≥ 0.05s typical).
  - Cache queries on `OnStart`; avoid heavy per-frame Directory scans.
- API correctness:
  - Scene/GameObject/Component used; no legacy RPC attributes.
  - Use `ResourceLibrary.Get/GetAll` for assets.

## 7. Quick Snippets

Networked effect call:

```csharp
[Broadcast]
private void PlayImpactFx( Vector3 pos, string sound )
{
    Sound.FromWorld( sound, pos );
}
```

Client → Server request with validation:

```csharp
[ConCmd.Server("rf_use_tool")]
public static void UseTool( Guid toolObjectId )
{
    var caller = ConsoleSystem.Caller;
    if ( caller is null ) return;

    var go = Scene.Active.Directory.FindByGuid( toolObjectId );
    var tool = go?.Components.Get<ToolComponent>();
    if ( tool is null || tool.OwnerConnection != caller ) return;

    tool.ServerUse();
}
```

Replicated state:

```csharp
[Net] public int Charges { get; private set; }
```

Find the local player (no Local.Pawn):

```csharp
// Client-side: find the player GameObject you own
var myPlayer = Scene.Active?.GetAllComponents<ResourceFarmer.PlayerBase.Player>()
  .FirstOrDefault( p => p.GameObject?.Network?.IsOwner == true )?.GameObject;

// Server-side: map ConsoleSystem.Caller to their owned GameObject
var go = Scene.Active?.Directory
  .FirstOrDefault( o => o.Network?.OwnerConnection == ConsoleSystem.Caller );
```

## 8. Official References

- `docs/deprecations-obsolete.md`: Project-maintained list of deprecated/obsolete patterns with modern replacements.
- Facepunch Wiki (requires login): https://wiki.facepunch.com/sbox — See topics: Networking (`[Net]`, ownership), Console Commands (`ConCmd.Server`, `ConsoleSystem.Caller`), Broadcast (`[Broadcast]`), Scene/GameObject/Component, `GameTask`, Triggers (`ITriggerListener`), Tags/Directory queries.
  // Additional public sources
- Release Notes: https://sbox.game/release-notes — API changes, deprecations, breaking updates.
- Dev Docs Hub: https://sbox.game/dev/doc/ — Official guides and API references.
