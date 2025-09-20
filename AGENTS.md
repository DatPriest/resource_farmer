# AGENTS

Status: Stable
Version: 1.0.0
Owners: @ResourceFarmer Team
Runtime: S&box (Scene/GameObject/Component), C# 11+
Authority: Server-authoritative (host runs gameplay logic)
Hotload: Required (no mutable static state)

## Purpose

Operational guide for GitHub Copilot or autonomous agents to generate, modify, and validate code for the Resource Farmer S&box project using modern APIs and patterns.

## Scope

- Code generation for Components, Systems, GameResources, and UI.
- Networking (state sync, RPC, spawning, ownership).
- Navigation and movement (NavMesh, NavMeshAgent).
- Crafting/resources pipeline updates.
- Lightweight tests, validation, and performance hygiene.

## Repository Conventions

- Scene-first architecture: Scene/GameObject/Component only (no legacy entities).
- Composition over inheritance; prefer events and Tags over type checks.
- Networking:
  - [Net] for state replication
  - [Broadcast] for server-to-clients events
  - [ConCmd.Server] for client-to-server requests (validate ConsoleSystem.Caller)
  - NetworkSpawn([owner?]) for networked objects
- Async: GameTask for delays; never block main thread.
- Hotload: No mutable static fields; use instance components, events, and readonly statics only.
- C#: Use file-scoped namespaces, init-only/auto-props, pattern matching, records where appropriate.

## Project Layout

- Code/Resources/ResourceType.cs — central enum for resources
- Code/Resources/... — inventory, gatherables, spawners
- Code/Player/... — interaction, gathering, tools
- Assets/Crafting/\*.recipe — CraftingRecipeResource assets
- scenes/minimal.scene — startup scene
- .github/copilot-instructions.md — coding style and rules (must follow)

## Capabilities (What the agent should do)

- Add/modify Components adhering to S&box hotload rules.
- Implement networked behavior:
  - Add [Net] props, [Broadcast] methods
  - Add [ConCmd.Server] entry points with validation
  - Use INetworkListener/INetworkSpawn where needed
- Create AI/navigation scripts using NavMeshAgent with repath throttling.
- Extend crafting/resources:
  - Add ResourceType enum values
  - Generate .recipe GameResources and code links
- Update UI (.razor) to read state only; send interactions via [ConCmd.Server].

## Non-Goals (Do not do)

- No legacy entity APIs, hooks, or Source 1 patterns.
- No client-authoritative gameplay logic.
- No polling when events exist.
- No long-running work on main thread; use GameTask.

## Commands & Patterns

- Client to Server:
  - [ConCmd.Server("action_name")] static void Action(params) { validate ConsoleSystem.Caller; }
- Server to Clients:
  - [Broadcast] void PlayFx(args) { /_ visuals/sounds only _/ }
- Spawn networked:
  - var go = Prefab.Clone(pos); go.NetworkSpawn([connection]);
- Replication:
  - [Net] public T Prop { get; private set; }
- Events & Tags:
  - GameObject.Dispatch(eventStruct); GameObject.Tags.Add("player");

## Validation Checklist (PR Gate)

- Hotload-safe: no mutable static state, no cached scene singletons in static fields.
- Networking:
  - All gameplay decisions guarded by IsHost.
  - [ConCmd.Server] validates ConsoleSystem.Caller ownership/context.
  - [Broadcast] used only for effects or client-side presentation.
  - [Net] properties minimal, necessary, and appropriately private set.
- Performance:
  - No allocations in OnUpdate/OnFixedUpdate hot paths (LINQ ok if not per-frame hot; prefer cached lists).
  - Repath throttled (>= 0.05s typical); time-slice AI loops.
  - Avoid excessive dynamic NavMeshArea carving.
- API correctness:
  - Scene/GameObject/Component used; no legacy RPC attributes.
  - Use ResourceLibrary.Get/ResourceLibrary.GetAll for assets.

## Typical Tasks (Examples)

- Add a gatherable node:
  - Create prefab with ResourceNode component, add to ResourceSpawner.ResourcePrefabs.
  - Extend ResourceType enum.
- Implement a networked tool:
  - [Net] state; [ConCmd.Server] for actions; [Broadcast] effects.
- Navigation follower:
  - NavMeshAgent + FollowTargetController with configurable RepathInterval.
- AI patrol:
  - Server-authoritative component sets Agent.MoveTo(); [Broadcast] turn FX.
- Crafting:
  - Add .recipe file and hook it via RecipeManager (ResourceLibrary.GetAll).

## Performance Budgets

- AI repath: 0.05–0.2s per agent (scale by population).
- Moving NavMeshArea: keep to a few dozens active movers.
- Physics: prefer CharacterController over full Rigidbody unless necessary.
- Avoid per-frame LINQ on large collections; cache references on OnStart.

## Security & Safety

- All damage/inventory/state transitions only on host (IsHost).
- All C2S commands validate ConsoleSystem.Caller ownership or proximity as appropriate.
- Never trust client-provided IDs without scene lookup verification.

## Build/Run/Debug

- S&box editor handles hotload; build-on-save.
- For networking tests, use local host lobby; verify [Net] deltas and [Broadcast] effects logs.
- Use scene Tags and Directory queries sparingly; cache in OnStart.

## Code Templates

- Networked effect call:

```csharp
[Broadcast]
private void PlayImpactFx(Vector3 pos, string sound )
{
    Sound.FromWorld( sound, pos );
}
```

- Client -> Server request with validation:

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

- Replicated state:

```csharp
[Net] public int Charges { get; private set; }
```

## Review Notes for Agents

- Prefer Components.GetOrCreate<T>() to avoid null checks.
- Use GameTask.Delay for cooldowns; avoid timers in Update when possible.
- Prefer events over polling (e.g., OnKilled, trigger listeners).

## Changelog

- 1.0.0 Initial AGENTS manifest specialized for S&box (modern APIs, networking, nav, crafting).
