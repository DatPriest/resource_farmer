# s&box.instructions.md

## Introduction: The Paradigm Shift from Entities to Scenes

This document serves as the authoritative technical guide to modern S&box development. It provides a detailed examination of the current C# Application Programming Interface (API), architectural patterns, and a comprehensive reference of obsolete features and their contemporary replacements. A foundational understanding of the engine's core architectural evolution—from a legacy entity-based model to a modern scene-based system—is critical for all developers aiming to build robust and forward-compatible projects on the platform.

The S&box engine represents a significant departure from the traditional architecture of the Source Engine. Development has pivoted away from the classic entity system, which has been entirely superseded by a modern, component-based scene system. This new architecture is deliberately designed to be familiar to developers experienced with contemporary engines such as Unity and Godot, aiming to provide a more transparent and rapid iteration workflow.

This strategic alignment with dominant industry standards is a cornerstone of the S&box philosophy. By adopting a familiar GameObject/Component model, the engine significantly lowers the barrier to entry for a large pool of developers, allowing them to leverage existing knowledge and accelerate the development process. This decision positions S&box not merely as a spiritual successor to Garry's Mod, but as a competitive, general-purpose game engine. The explicit deprecation of the legacy entity system and the planned future removal of the Hammer editor are necessary consequences of this strategic pivot. Facepunch is systematically shedding legacy paradigms to fully embrace a more flexible, powerful, and widely understood architecture, signaling a long-term ambition that extends far beyond the scope of its predecessor.

The core components of this architecture are as follows:

- Scene: The top-level container for a game world. A Scene is analogous to a map or a level and holds all the GameObjects that exist within it at a given time. Scenes are saved as .scene files (which are JSON on disk) and are designed to be loaded and switched quickly.
- GameObject: The fundamental object within a Scene. A GameObject is essentially a container that has a position, rotation, and scale (a Transform) in the world. GameObjects can be arranged in hierarchies, where child objects move relative to their parents, and they serve as the hosts for Components. A GameObject with a ModelRenderer component will render a model, while adding a BoxCollider component will give it physical collision. Game development in S&box is primarily the process of creating new Components in C# and assembling them on GameObjects within a Scene.
- Component: The modular building block of all functionality in S&box. All game logic, rendering behaviors, physics interactions, audio sources, and other features are implemented as Components that are attached to GameObjects.

## Section 1: Core Scene and GameObject API

### 1.1. Creating and Managing GameObjects

The programmatic creation and manipulation of GameObjects and their Components form the basis of dynamic gameplay in S&box. GameObjects can be instantiated at runtime, either as empty containers or by cloning existing prefabs.

To create a new, empty GameObject:

```csharp
var myGameObject = new GameObject();
myGameObject.Name = "Dynamic Object";
myGameObject.Transform.Position = new Vector3( 0, 0, 100 );
```

Components are added to GameObjects to grant them behavior. This is accomplished using AddComponent<T>(), where T is the type of the Component to add.

```csharp
// Assuming 'MyCustomLogic' is a class that inherits from Component
var logicComponent = myGameObject.AddComponent<MyCustomLogic>();
```

Existing Components can be retrieved using GetComponent<T>() or Components.Get<T>(). If a GameObject may or may not have a component, Components.TryGet<T>(out var component) is the preferred method to avoid exceptions.

```csharp
// Get the ModelRenderer component, if it exists
if ( myGameObject.Components.TryGet<ModelRenderer>( out var renderer ) )
{
    renderer.SetModel( "models/my_model.vmdl" );
}
```

Hierarchies are managed by setting the Parent property of a GameObject's Transform.

```csharp
var childObject = new GameObject();
childObject.Transform.Parent = myGameObject.Transform; // childObject now moves with myGameObject
```

### 1.2. Finding and Querying GameObjects

While it is best practice to maintain direct references to critical GameObjects, it is sometimes necessary to find objects within the scene. The engine provides several methods for this, though developers should be mindful of their performance implications, especially when called frequently (e.g., in an OnUpdate loop).

- Scene.FindAll: Returns an IEnumerable<GameObject> of all GameObjects in the scene that match a given query.
- Scene.GetAllObjects: Returns all GameObjects in the scene.
- Scene.Directory: A more optimized way to find objects, particularly by tags.

Example of finding all GameObjects with a specific tag:

```csharp
// Find all objects tagged as "Enemy"
var enemies = Scene.Directory.Find( "Enemy" );
foreach ( var enemy in enemies )
{
    // ... do something with each enemy
}
```

For performance-critical code, it is highly recommended to find objects once (e.g., in OnStart) and cache the references for later use, rather than repeatedly querying the scene.

### 1.3. The GameObject Lifecycle

Components attached to GameObjects have a defined lifecycle with several key methods that are automatically invoked by the engine at specific points. Understanding this lifecycle is essential for writing predictable and correctly ordered game logic.

- OnStart(): Called once in the lifetime of a Component, on the first frame after it has been enabled. This is the ideal place for initialization logic, such as finding other GameObjects or setting up initial state.
- OnUpdate(): Called every frame. This method is used for most frame-to-frame game logic, such as processing input or updating timers. The time since the last frame can be accessed via Time.Delta.
- OnFixedUpdate(): Called at a fixed time interval, independent of the frame rate. This is the correct place for all physics-related calculations and manipulations, such as applying forces to a RigidBody, to ensure deterministic physical behavior. The fixed time step is available via Time.FixedDelta.
- OnDestroy(): Called just before the Component or its parent GameObject is destroyed. This is used for cleanup logic, such as unsubscribing from events or releasing resources.

Example of a simple Component demonstrating the lifecycle:

```csharp
public class LifecycleExample : Component
{
    protected override void OnStart()
    {
        Log.Info( $"GameObject '{GameObject.Name}' has started." );
    }

    protected override void OnUpdate()
    {
        // This will run every frame
    }

    protected override void OnFixedUpdate()
    {
        // This will run at a fixed physics interval
    }

    protected override void OnDestroy()
    {
        Log.Info( $"GameObject '{GameObject.Name}' is being destroyed." );
    }
}
```

## Section 2: Modern Player Control

### 2.1. Obsolete Feature: Local.Pawn

Developers migrating from older Facepunch titles or legacy engine versions must be aware that the concept of Local.Pawn is entirely obsolete and has been removed from the modern S&box API.

This removal is a direct consequence of the architectural shift to the scene system. Local.Pawn was a global, static property that provided a direct reference to the local player's controllable entity. This pattern is fundamentally incompatible with the new component-based paradigm. In the modern architecture, there is no inherent global "pawn." The player's character is simply a GameObject that has been configured with the necessary components for control, rendering, and networking. This GameObject is typically identified by its network ownership (i.e., the GameObject whose Network.OwnerConnection corresponds to the local client) or by a unique tag. This change decouples player logic from the engine's core, resulting in a more flexible, modular, and scalable system where the definition of a "player" is determined entirely by the developer's code and scene setup.

### 2.2. The PlayerController Component: A Physics-First Approach

The modern, standard solution for player control in S&box is the built-in PlayerController component. This component is designed to be a simple, drag-and-drop solution for first and third-person character movement that can be used with no programming required.

The core design philosophy of the PlayerController is a radical departure from the character controllers found in older Source Engine games. It is not a system that works by tracing a bounding box through the world and "hacking in" physics interactions. Instead, at its core, the PlayerController is a specialized RigidBody that exists fully within the physics system. This "physics-first" approach was a deliberate choice to enable more realistic and emergent physical interactions without the need to constantly emulate a physics engine. As a true physical object, a GameObject with a PlayerController can naturally push other physics objects, be pushed by them, stand on moving platforms, be affected by ground friction (such as on ice), and even be crushed by heavy objects—all without requiring custom code for these specific scenarios.

### 2.3. Properties and Modular Features

Developers interact with the PlayerController primarily by manipulating its properties, such as WishVelocity (the desired movement direction, typically derived from player input) and EyeAngles (the direction the player is looking).

A key feature of the PlayerController is its modular design. It is composed of several independent sub-systems that can be enabled or disabled directly in the editor inspector by right-clicking the corresponding tab. This allows developers to use only the parts they need and replace others with custom implementations.

- Input: Provides built-in handling for mouse, keyboard, and game controller input to drive movement and looking. This can be disabled to allow a developer to create a custom input scheme, which would typically be implemented in an OnFixedUpdate method on a sibling component that sets the WishVelocity and EyeAngles properties directly.
- Camera: Includes a built-in first and third-person camera controller with features like wall collision avoidance. This can be disabled if a project requires a unique camera system (e.g., a fixed-angle or top-down camera), which would be implemented in a separate Component.
- Animator: Provides a built-in animator that automatically sets variables on a Citizen-compatible AnimGraph and can play footstep sounds. This can be disabled for characters with custom animation requirements or non-standard rigs, allowing a developer to create their own animation logic Component.

### 2.4. Implementation and Event Handling

The PlayerController exposes a rich set of events that allow other Components to react to player actions. To listen to these events, a developer can create a new Component on the same GameObject and implement the PlayerController.IEvents interface. This provides a clean and decoupled way to extend the controller's functionality.

```csharp
using Sandbox;

public class MyPlayerLogic : Component, PlayerController.IEvents
{
    /// <summary>
    /// This event is fired the moment the player jumps.
    /// It can be used to play a jump sound or trigger a particle effect.
    /// </summary>
    public void OnJumped()
    {
        Sound.FromWorld( "player.jump", Transform.Position );
        // Example: Trigger a dust particle effect at the player's feet.
    }

    /// <summary>
    /// This event is called every frame after the PlayerController has positioned the camera.
    /// It is useful for applying custom camera effects, overrides, or UI adjustments.
    /// </summary>
    public void PostCameraSetup( CameraComponent cam )
    {
        // Example: Apply a custom field-of-view effect when sprinting.
        if ( Input.Down( "run" ) )
        {
            cam.FieldOfView = MathX.Lerp( cam.FieldOfView, 100f, Time.Delta * 5.0f );
        }
    }

    /// <summary>
    /// This event allows for modifying the player's view angles before they are applied.
    /// It can be used to alter mouse sensitivity, implement recoil, or clamp view angles.
    /// </summary>
    void OnEyeAngles( ref Angles angles )
    {
        // Example: Reduce mouse sensitivity by half.
        angles *= 0.5f;
    }
}
```

This event-driven approach, combined with the controller's modularity, provides a powerful and flexible foundation for player control that prioritizes emergent, physics-driven gameplay over the deterministic, scripted control of older systems. This design choice has profound implications, as it requires developers to design gameplay and levels that account for physical properties like mass, momentum, and friction, ultimately leading to more dynamic and less predictable gameplay scenarios.

## Section 3: Networking and Multiplayer Systems

### 3.1. Design Philosophy: Simplicity and Accessibility

The networking system in S&box is intentionally designed with simplicity and ease of use as its primary goals. The initial aim of the architecture is not to provide a perfectly secure, bulletproof server-authoritative system, but rather to offer a framework that is easy to understand and allows developers to rapidly prototype and build multiplayer experiences.

### 3.2. Session and Lobby Management

The static Sandbox.Networking class is the entry point for managing multiplayer sessions. It provides straightforward methods for creating, finding, and joining game lobbies.

To create a new public lobby for up to 8 players:

```csharp
using Sandbox.Network;

public void CreateGameLobby()
{
    Networking.CreateLobby( new LobbyConfig()
    {
        MaxPlayers = 8,
        Privacy = LobbyPrivacy.Public,
        Name = "My Awesome Game Server"
    });
}
```

To query for all available lobbies for the current game and join the most suitable one:

```csharp
using Sandbox.Network;
using System.Threading.Tasks;

public async Task JoinGame()
{
    // QueryLobbies is an async method
    var lobbies = await Networking.QueryLobbies();

    // Or, more simply, let the system find the best match
    bool success = await Networking.JoinBestLobby();
    if ( !success )
    {
        Log.Warning( "Failed to find a suitable lobby to join." );
    }
}
```

### 3.3. Networked GameObjects and Ownership

Any GameObject can be converted into a networked object, which synchronizes its existence, state, and events across all clients. This is achieved by calling the NetworkSpawn() method on a GameObject instance that exists on the host.

```csharp
// 'PlayerPrefab' is a GameObject asset defined in the editor
var playerInstance = PlayerPrefab.Clone( spawnPosition );
playerInstance.NetworkSpawn();
```

Every networked GameObject can have an owner, represented by a Connection object. The owner is the client that typically has authority over the object's transform and input. Ownership can be assigned during the spawn call. The synchronization behavior of a GameObject is controlled by its NetworkMode property, which can be set in the editor or via code.

- NetworkMode.Never: The GameObject is local only and is never sent to other clients.
- NetworkMode.Snapshot (Default): The GameObject is sent to clients once as part of the initial scene data when they join. It receives no further updates, making it ideal for static level geometry and other non-interactive world objects.
- NetworkMode.Object: The GameObject is sent to clients and is continuously updated. It can have synchronized properties ([Net]) and receive Remote Procedure Calls (RPCs). This mode is essential for all dynamic objects, such as players, NPCs, projectiles, and interactive items.

### 3.4. State Synchronization with [Net]

The [Net] attribute is the primary mechanism for replicating the state of a Component's properties from the host to all clients. By simply adding this attribute to a property on a Component attached to a networked GameObject, the engine will automatically handle the synchronization of its value.

A recent update also introduced an interpolation flag for [Net] variables, which can be used to smoothly interpolate the value on remote clients over several ticks. This is particularly useful for variables that change rapidly, such as player view angles, preventing jittery visual updates on other clients.

Example of a Component with a synchronized property:

```csharp
public class PlayerHealth : Component
{
    // This property will be automatically synchronized from host to all clients.
    // Any change to Health on the host will be reflected on all clients.
    [Net] public float Health { get; set; } = 100.0f;

    // Example of an interpolated variable for smooth remote updates
    [Net] public Angles ViewAngles { get; set; }
}
```

### 3.5. Remote Procedure Calls (RPCs)

Remote Procedure Calls (RPCs) are methods that, when invoked on one machine, are executed on other machines over the network. This is the fundamental system for triggering networked events and actions. An RPC is defined by adding an attribute to a C# method.

Quick reference for primary RPC attributes:

- [Broadcast]: Called on the host and all clients. Use for global effects or events everyone should see (explosions, chat, global sounds).
- [Owner]: Called only on the machine that owns the GameObject. Use for client-specific feedback like hitmarkers or local UI updates.
- [ConCmd.Server]: Static methods called from clients to request authoritative server actions (damage application, spawning items). Always validate ConsoleSystem.Caller.

Advanced features:

- Arguments: RPC methods accept arguments like normal methods; the engine handles serialization.
- Static RPCs: RPCs can be declared on static methods for global systems.
- Filtering: Broadcast RPCs can be filtered via Rpc.FilterInclude(c => ...) or Rpc.FilterExclude(c => ...) scopes.
- Delivery flags (NetFlags):
  - NetFlags.Reliable (default): Guarantees delivery; use for critical events.
  - NetFlags.Unreliable: Fire-and-forget; use for frequent, non-critical cosmetic updates.

### 3.6. Responding to Network Events

To react to core networking events, such as players joining or leaving a session, a Component can implement the Component.INetworkListener interface. To execute logic when a specific GameObject is first spawned on the network, a Component on that object can implement Component.INetworkSpawn.

The following example demonstrates a network manager that spawns a player prefab for each connecting client and assigns them ownership:

```csharp
public sealed class GameNetworkManager : Component, Component.INetworkListener
{
    [Property] public GameObject PlayerPrefab { get; set; }

    /// <summary>
    /// Called on the host when a client has fully connected and is ready to enter the game.
    /// </summary>
    public void OnActive( Connection connection )
    {
        // Clone the player prefab at a default spawn point.
        var playerGo = PlayerPrefab.Clone( Transform.Position );
        playerGo.Name = $"Player - {connection.DisplayName}";

        // Spawn the GameObject on the network and assign ownership to the newly connected client.
        playerGo.NetworkSpawn( connection );
    }
}
```

### 3.7. Deprecated Networking APIs

The networking API has undergone refinement to promote more robust and modern programming patterns. Several static properties and methods on the Networking class are now considered obsolete. This reflects a broader architectural shift away from global static state management towards a more scalable, event-driven, and object-oriented model. Instead of requiring developers to manually poll or iterate global lists, the modern API encourages the use of event listeners like INetworkListener to react to network state changes.

Obsolete and replacements:

- Networking.Connections (obsolete): Use Component.INetworkListener to react to connection events.
- Networking.HostConnection (obsolete): Use Networking.IsHost to check host status.
- Networking.FindConnection (obsolete): Find a player's GameObject via scene queries; read Network.OwnerConnection from its Network component.

## Section 4: Reference Guide to Obsolete and Overhauled Systems

Facepunch has demonstrated a consistent and pragmatic willingness to aggressively refactor and overhaul major engine systems that are deemed unintuitive, inefficient, or non-standard. The significant changes made to core systems like Prefabs, Decals, and Particles reveal a clear development philosophy: long-term usability and alignment with proven industry design patterns are valued more highly than maintaining backward compatibility with earlier iterations. The developer commentary on the Prefab overhaul is particularly illustrative of this mindset: "Turns out that was all a load of shit. So we overhauled it to work like everyone would expect." This candid admission highlights a commitment to replacing flawed systems with ones that are familiar and powerful. This approach ensures the engine's long-term health and competitiveness but requires developers to remain vigilant and adaptable to breaking changes.

### 4.1. Prefab System Overhaul

- Obsolete: The original "interface-style" prefab system is completely obsolete. This system required developers to explicitly expose certain "prefab variables" that could be edited on instances of the prefab. This workflow was found to be cumbersome and unintuitive.
- Modern: The current prefab system functions in a manner that is standard across the industry, closely resembling the workflow in engines like Unity. When a prefab asset is dragged into a scene, it creates an "instance" that contains a full hierarchy of all the original GameObjects and Components. Developers can freely modify the properties of this instance, creating overrides. These changes can then be applied back to the source prefab asset to update all other instances, or they can be reverted to restore the instance to the prefab's default state. Existing prefabs were automatically converted to this new, more powerful system.

### 4.2. Graphics and Rendering Deprecations

Several legacy graphics and rendering systems have been deprecated in favor of more modern, unified solutions.

- Decals: All previous methods for creating decals, including projected_decals, static_overlay, and decal-specific shaders, are now obsolete. These have been replaced by a single, unified decal renderer that integrates decals directly into the models themselves, providing a more robust and performant solution.
- Particles: The "legacy particle system" is obsolete and will no longer render in-game. Developers must migrate all particle effects to use the modern particle system and editor.
- API Changes: The static method Graphics.RenderToTexture is obsolete. The modern equivalent functionality is now located on the CameraComponent class as CameraComponent.RenderToTexture.

### 4.3. Navigation System

The navigation mesh (Navmesh) system has also received a significant overhaul to improve performance and functionality, especially for large or dynamic environments.

- Obsolete: The API method NavMesh.GetSimplePath has been marked as obsolete and should no longer be used.
- Modern: The entire navmesh generation process has been rewritten. Instead of generating one monolithic navmesh for an entire map, the system now generates the navmesh in smaller, discrete tiles. This has several key advantages: it allows for navmeshes to cover much larger maps, and it enables the efficient regeneration of individual tiles at runtime without freezing the game. Furthermore, the new system supports dynamic obstacles, which can be integrated via the standard collider/trigger system. This allows developers to create features like doors or temporary blockades that AI agents can navigate around in real-time without requiring a full rebuild of the navmesh.

## Section 5: Comprehensive Guide to Obsolete and Deprecated APIs

This section provides a detailed list of obsolete code, systems, and changes based on official changelogs and documentation. It is intended to be a quick reference for developers migrating older projects or updating their knowledge base.

### 5.1. Core Architecture and Player Control

- Legacy Entity System: The entire classic Source Engine entity system has been removed and replaced by the modern Scene/GameObject/Component architecture. All game logic should be built using components.
- Local.Pawn: This static property, used to get a reference to the local player's character, is obsolete and has been removed. The modern approach is to identify the player's GameObject through network ownership or by querying the scene for a specific component or tag.
- Hammer Editor: While still currently in use, the Hammer editor is planned for future removal. The long-term vision is for scene and level editing to be fully integrated into the main S&box editor, particularly once scene mesh editing capabilities are mature enough to replace Hammer's workflow.

### 5.2. Asset and Resource Systems

- Prefab System (Original): The initial "interface-style" prefab system and its associated "prefab variables" are obsolete. This has been replaced with an industry-standard system of prefab instances and overrides, similar to Unity.
- **Attribute**: This attribute has been replaced with.22
- Legacy Particle System: All legacy particle systems are obsolete and will no longer render. All particle effects must be created or migrated to the modern particle editor.

### 5.3. Graphics and Rendering

- Decal Systems: All older methods for creating decals are obsolete. This includes projected_decals, static_overlay, and any decal-specific shaders. The modern replacement is a unified decal renderer that integrates decals directly into models.
- Graphics.RenderToTexture: This static method is obsolete. The equivalent functionality is now found on the CameraComponent as CameraComponent.RenderToTexture.
- Camera Hooks: The methods CameraComponent.AddHookBeforeOverlay and CameraComponent.AddHookAfterTransparent are obsolete. The modern approach is to use CommandLists for custom rendering logic.
- Spritecard Shader: This shader is obsolete as it was only used by the now-removed legacy particle system.
- Shader Constants: The shader constants S_SPECULAR and ENABLE_NORMAL_MAPS have been removed.

### 5.4. Networking

As detailed in Section 3.7, the following static methods and properties on the Sandbox.Networking class are obsolete:

- Networking.Connections
- Networking.HostConnection
- Networking.FindConnection

The modern approach is to use event-driven listeners like Component.INetworkListener and to query for networked objects within the scene to find their owners.

### 5.5. Navigation

- NavMesh.GetSimplePath: This method is obsolete and has been removed from the API. Pathfinding should now be handled through the new tiled navmesh system.

### 5.6. UI and Editor

- Panel.Bind: This UI panel method is obsolete.
- [HideInEditor] Attribute: This attribute is marked as obsolete.
- ActionGraph Attributes: The attributes [ExpressionNode] and [ActionNode] are obsolete.

### 5.7. Miscellaneous Code and Libraries

- Sandbox.Game.dll: This library has been removed, and its functionality has been merged into Sandbox.Engine.dll. This simplifies the project structure and dependency management.

## Referenzen

- S&box - Wikipedia, Zugriff am September 20, 2025, https://en.wikipedia.org/wiki/S%26box
- S&box - Valve Developer Community, Zugriff am September 20, 2025, https://developer.valvesoftware.com/wiki/S%26box
- Development - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/about/getting-started/development/
- Documentation - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/
- s&box, Zugriff am September 20, 2025, https://sbox.game/
- Action Graph - s&box, Zugriff am September 20, 2025, https://sbox.game/news/action-graph
- Getting Started - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/about/getting-started/
- User Changes For Alex - S&box Wiki, Zugriff am September 20, 2025, https://wiki.facepunch.com/sbox/~userchanges:696613
- Player Controller - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/scene/components/reference/player-controller/
- news/player-controller-1c29c27d - s&box, Zugriff am September 20, 2025, https://sbox.game/news/player-controller-1c29c27d
- Player Controller - s&box, Zugriff am September 20, 2025, https://sbox.game/news/october-update-133be3ec/player-controller
- Networking & Multiplayer - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/networking-multiplayer/
- Networking & Multiplayer - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/
- Networked Objects - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/networked-objects/
- Network Events - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/network-events/
- January Update - s&box, Zugriff am September 20, 2025, https://sbox.game/news/january-update-6a7d5bb1
- RPC Messages - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/rpc-messages/
- Networking - s&box, Zugriff am September 20, 2025, https://sbox.game/api/all/Sandbox.Networking/
- s&box, Zugriff am September 20, 2025, https://sbox.game/news/june-2025
- s&box, Zugriff am September 20, 2025, https://sbox.game/news/september-2025
- November Update - s&box, Zugriff am September 20, 2025, https://sbox.game/news/november-update-3240bfc2/fixedupdate-fix
- Release Notes - s&box, Zugriff am September 20, 2025, https://sbox.game/release-notes
- Attributes - s&box, Zugriff am September 20, 2025, https://sbox.game/api/i/allattributes
  [HideInEditor] Attribute: This attribute is marked as obsolete.23
  ActionGraph Attributes: The attributes [ExpressionNode] and [ActionNode] are obsolete.23

  5.7. Miscellaneous Code and Libraries

Sandbox.Game.dll: This library has been removed, and its functionality has been merged into Sandbox.Engine.dll.22 This simplifies the project structure and dependency management.
Referenzen
S&box - Wikipedia, Zugriff am September 20, 2025, https://en.wikipedia.org/wiki/S%26box
S&box - Valve Developer Community, Zugriff am September 20, 2025, https://developer.valvesoftware.com/wiki/S%26box
Development - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/about/getting-started/development/
Documentation - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/
s&box, Zugriff am September 20, 2025, https://sbox.game/
Action Graph - s&box, Zugriff am September 20, 2025, https://sbox.game/news/action-graph
Getting Started - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/about/getting-started/
User Changes For Alex - S&box Wiki, Zugriff am September 20, 2025, https://wiki.facepunch.com/sbox/~userchanges:696613
Player Controller - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/scene/components/reference/player-controller/
news/player-controller-1c29c27d - s&box, Zugriff am September 20, 2025, https://sbox.game/news/player-controller-1c29c27d
Player Controller - s&box, Zugriff am September 20, 2025, https://sbox.game/news/october-update-133be3ec/player-controller
Networking & Multiplayer - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/networking-multiplayer/
Networking & Multiplayer - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/
Networked Objects - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/networked-objects/
Network Events - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/network-events/
January Update - s&box, Zugriff am September 20, 2025, https://sbox.game/news/january-update-6a7d5bb1
RPC Messages - s&box, Zugriff am September 20, 2025, https://sbox.game/dev/doc/systems/networking-multiplayer/rpc-messages/
Networking - s&box, Zugriff am September 20, 2025, https://sbox.game/api/all/Sandbox.Networking/
s&box, Zugriff am September 20, 2025, https://sbox.game/news/june-2025
s&box, Zugriff am September 20, 2025, https://sbox.game/news/september-2025
November Update - s&box, Zugriff am September 20, 2025, https://sbox.game/news/november-update-3240bfc2/fixedupdate-fix
Release Notes - s&box, Zugriff am September 20, 2025, https://sbox.game/release-notes
Attributes - s&box, Zugriff am September 20, 2025, https://sbox.game/api/i/allattributes
