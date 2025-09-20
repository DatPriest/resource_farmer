# S&box Deprecations and Obsolete APIs

Status: living reference for common deprecations and their modern replacements. This reflects the current project guidance and the modern S&box Scene/GameObject/Component APIs.

Note on official docs: Many Facepunch wiki pages require an authenticated session. The high-level topics are listed for reference; you may need to log in to view the specific articles on the Facepunch wiki.

## Summary

- **Local.Pawn**: obsolete. Use ownership-based patterns (`GameObject.Network.OwnerConnection`, `Network.IsOwner`).
- **ServerRpc/ClientRpc attributes**: deprecated. Use `ConCmd.Server` for client→server requests and `[Broadcast]` instance methods for server→client effects; replicate state via `[Net]` properties.
- **Legacy Entity/Game inheritance patterns**: deprecated for gameplay. Prefer Scene/GameObject/Component composition.
- **Global hooks and Source 1 style APIs**: deprecated. Use component lifecycle, events/dispatch, and trigger interfaces.
- **Ad-hoc timers and blocking waits**: discouraged. Use `async Task` with `GameTask.Delay/NextFrame/RunInThreadAsync`.
- **Per-frame polling in `Tick/Simulate`**: discouraged. Prefer events, triggers, and throttled updates.
- **Name-based or global lookups**: discouraged. Use tags (`GameObject.Tags`) and scoped Scene queries; cache in `OnStart`.
- **Direct cross-component coupling**: discouraged. Use events, dispatch, and `Components.GetOrCreate<T>()` when necessary.

## Project-Specific Deprecations

- **ResourceFarmer.UI.WorldPanelManager**: Deprecated in favor of `ResourceFarmer.UI.Components.WorldPanelVisibilityManager`. 
  - Status: ✅ **MIGRATED** - Player prefab updated to use new component
  - The old component serves as a compatibility layer but shows warnings
- **ResourceFarmer.Items.ToolBonusRegistry**: Marked obsolete but cannot be removed yet.
  - Status: ⚠️ **BLOCKED** - Supposed replacement "BonusManager" does not exist in codebase
  - Still actively used in 10+ locations across ToolBase.cs, ToolBonusExtensions.cs, CraftingRecipeResource.cs, and Player.Crafting.cs
  - Action needed: Implement BonusManager before removal

## Details and Migration

1. Local.Pawn (obsolete)

- Problem: The old global/local pawn reference is not part of the modern API.
- Replacement:
  - Client: find the owned player by `Network.IsOwner` on a player component or object.
  - Server: map `ConsoleSystem.Caller` to their owned `GameObject` via `OwnerConnection`.
- Example:
  - Client: `Scene.Active?.GetAllComponents<ResourceFarmer.PlayerBase.Player>().FirstOrDefault( p => p.GameObject?.Network?.IsOwner == true )?.GameObject`
  - Server: `Scene.Active?.Directory.FirstOrDefault( o => o.Network?.OwnerConnection == ConsoleSystem.Caller )`

2. [ServerRpc]/[ClientRpc] (deprecated)

- Problem: Legacy RPC attributes are replaced by explicit command and broadcast patterns.
- Replacement:
  - Client → Server: `[ConCmd.Server("action")]` static method; validate `ConsoleSystem.Caller` and ownership/proximity.
  - Server → Clients: `[Broadcast]` instance method for visuals/audio only; replicate gameplay state via `[Net]`.

3. Entity-derived gameplay (deprecated)

- Problem: Monolithic `Entity`/`Game` inheritance is replaced by composition.
- Replacement: Use `Scene`/`GameObject`/`Component`, tags for discovery, events for coordination.

4. Global hooks, Source 1 style APIs (deprecated)

- Problem: Global hooks and name-based lookups are brittle and not hotload-friendly.
- Replacement: Use component lifecycles, `GameObject.Dispatch(...)`, triggers (`ITriggerListener`), and tags.

5. Timers/blocking waits (discouraged)

- Problem: Blocking or ad-hoc timers hurt hotload and main thread responsiveness.
- Replacement: `async Task` with `GameTask.Delay`, `GameTask.NextFrame`, `GameTask.RunInThreadAsync` as appropriate.

6. Per-frame polling in `Tick/Simulate` (discouraged)

- Problem: Wastes CPU and allocations; hard to scale.
- Replacement: Event-driven logic; throttle AI/navigation (typical repath ≥ 0.05s).

7. Name-based/global lookups (discouraged)

- Problem: Fragile and slow for larger scenes.
- Replacement: `GameObject.Tags`, scoped directory queries; cache references in `OnStart`.

8. Tight cross-component coupling (discouraged)

- Problem: Inhibits reuse and hotload; creates hard dependencies.
- Replacement: Events/dispatch; `Components.GetOrCreate<T>()` as needed for local composition.

## Official Topics (Facepunch Wiki)

These topic pages are commonly used; access may require login.

- Networking overview: state replication (`[Net]`), ownership, and client/server separation
- Console commands: `ConCmd.Server` usage and `ConsoleSystem.Caller`
- Broadcast attributes: `[Broadcast]` for server→client effects
- Scene/GameObject/Component architecture and component lifecycle
- `GameTask` async primitives
- Triggers and physics listeners (e.g., `ITriggerListener`)
- Tags and Scene directory queries

If you cannot access the wiki, prefer the patterns exemplified in this repository (see `.github/copilot-instructions.md`) and S&box samples using the modern API.

## Additional Official Sources

- Release Notes: https://sbox.game/release-notes — Track API changes, deprecations, and breaking updates.
- Developer Docs Hub: https://sbox.game/dev/doc/ — Entry point to official guides and API references.
