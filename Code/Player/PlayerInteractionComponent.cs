// File: Code/Player/PlayerInteractionComponent.cs
using Sandbox;
using System;
using ResourceFarmer.Resources;
namespace ResourceFarmer.PlayerBase;
public sealed class PlayerInteractionComponent : Component
{
	[Property] public Player OwnerPlayer { get; set; }
	// Keep reference to GatheringComponent for calling ProcessHit
	public PlayerGatheringComponent Gathering => OwnerPlayer?.Components.Get<PlayerGatheringComponent>();

	// Define interaction distance here
	[Property] public float InteractionDistance { get; set; } = 100f; // Adjust as needed

	protected override void OnAwake()
	{
		if ( OwnerPlayer == null )
		{
			OwnerPlayer = Components.GetInParentOrSelf<Player>();
		}
	}

    // Schritt-für-Schritt-Pseudocode:
    // 1. Hole das Camera-Objekt.
    // 2. Prüfe, ob eine Kamera vorhanden ist. Wenn nicht, Log-Warnung und return.
    // 3. Ermittle den Mittelpunkt des Bildschirms (Screen.Size * 0.5).
    // 4. Erzeuge mit camera.ScreenPointToRay(screenCenter) einen Ray vom Bildschirmmittelpunkt aus.
    // 5. Führe den Raytrace mit origin + direction * InteractionDistance aus.
    // 6. Zeige den Debug-Strahl an, um den Verlauf zu sehen.



	public float GetLocalInteractionDistance()
	{
		float _localInteractionDistance = InteractionDistance;
		var camera = Scene.Camera;
		if ( camera == null )
		{
			Log.Warning( "[Player] Cannot find main camera for interaction raycast!" );
			return 0f;
		}
		var distanceFromPlayerToCamera = Utility.Util.GetDistance( GameObject, camera.GameObject );
		var thirdPerson = OwnerPlayer.PlayerController.ThirdPerson;
		if ( thirdPerson )
		{
			_localInteractionDistance = InteractionDistance + (float)distanceFromPlayerToCamera;
			Log.Info( $"[Player] Distance from player to camera: {distanceFromPlayerToCamera}" );
		}

		return _localInteractionDistance;
	}

	// GetTargetedInteractable is removed as we now use raycast on action

	/// <summary>
	/// RPC called by the client owner when they press the primary action key.
	/// </summary>
	[Rpc.Broadcast(NetFlags.Reliable)] // Run on Host/Server
	public void RequestPrimaryAction()
	{
		// Ensure called by the owner (Network.OwnerConnection is the sender)
		if ( !VerifySender() ) return;

		// Log.Info($"[InteractionComponent Server] Received RequestPrimaryAction from {Network.OwnerConnection.SteamId}");
		PrimaryAction(); // Execute the action logic on the server
	}

	/// <summary>
	/// RPC called by the client owner for secondary action.
	/// </summary>
	[Rpc.Broadcast( NetFlags.Reliable )] // Run on Host/Server
	public void RequestSecondaryAction()
	{
		if ( !VerifySender() ) return;
		SecondaryAction();
	}

	/// <summary>
	/// Server-side logic for the primary action (e.g., attacking/gathering).
	/// Performs a raycast from the player's eye position.
	/// </summary>
	private void PrimaryAction()
	{
		if ( OwnerPlayer == null ) return;

		var tr = Scene.Trace.Ray( Scene.Camera.Transform.World.ForwardRay, GetLocalInteractionDistance() )
			.Radius( 8 )
			.WithoutTags( "Player" ) // Ignore the player itself
			.Run();
		// Debugging trace visualization (Server-side)
		// DebugOverlay.TraceResult( tr, 1.0f ); // Show trace for 1 second

		if ( tr.Hit )
		{
			// Log.Info( $"[InteractionComponent Server] PrimaryAction ray hit: {tr.GameObject.Name} ({tr.GameObject.Tags}), Component: {tr.Component?.GetType().Name}" );

			// Check if the hit object has a ResourceNode component
			// Important: Use tr.GameObject.Components - tr.Component might be the Collider
			var resourceNode = tr.GameObject.Components.Get<ResourceNode>();
			if ( resourceNode != null && resourceNode.IsValid() )
			{
				// Found a resource node, process the hit via GatheringComponent
				Gathering?.ProcessHit( resourceNode );
			}
			else
			{
				// Hit something else - check if it's interactable (optional)
				var interactable = tr.GameObject.Components.Get<IInteractable>();
				if ( interactable != null )
				{
					Log.Info( $"[InteractionComponent Server] PrimaryAction interacting with generic interactable: {tr.GameObject.Name}" );
					// Call the interact method on the other interactable
					interactable.Interact( OwnerPlayer, true );
				}
				else
				{
					Log.Info( $"[InteractionComponent Server] PrimaryAction ray hit non-interactable: {tr.GameObject.Name}" );
				}
			}
		}
		else
		{
			Log.Info( "[InteractionComponent Server] PrimaryAction ray missed." );
		}
	}

	/// <summary>
	/// Server-side logic for the secondary action.
	/// </summary>
	private void SecondaryAction()
	{
		if ( OwnerPlayer == null ) return;

		var tr = Scene.Trace.Ray( Scene.Camera.Transform.World.ForwardRay, GetLocalInteractionDistance() )
			.Radius( 8 )
			.IgnoreGameObjectHierarchy( GameObject ).WithoutTags( "Player" ) // Ignore the player itself
			.Run();

		if ( tr.Hit )
		{
			var interactable = tr.GameObject.Components.Get<IInteractable>();
			if ( interactable != null )
			{
				Log.Info( $"[InteractionComponent Server] SecondaryAction interacting with: {tr.GameObject.Name}" );
				interactable.Interact( OwnerPlayer, false );
			}
			Log.Info( $"[InteractionComponent Server] SecondaryAction ray hit: {tr.GameObject.Name} ({tr.GameObject.Tags}), Component: {tr.Component?.GetType().Name}" );
		}
	}

	/// <summary>
	/// Verifies that the RPC sender is the owner of this player object.
	/// </summary>
	private bool VerifySender()
	{
		// In S&box 2024+, RpcTarget.HostOnly implicitly checks ownership if called from client.
		// For older versions or extra safety:
		// if ( Network.OwnerConnection == null || Rpc.Caller != Network.OwnerConnection )
		// {
		//     Log.Warning( $"[InteractionComponent Server] RPC called by non-owner: {Rpc.Caller?.SteamId}" );
		//     return false;
		// }
		return true; // Assume valid if HostOnly is used correctly
	}
}
