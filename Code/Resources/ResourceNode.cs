// File: Code/Resources/ResourceNode.cs
using System;
using Sandbox;
using ResourceFarmer.Utility; // Assuming this namespace exists
using Sandbox.UI;
using System.Linq;
using ResourceFarmer.Items;

using ResourceFarmer.PlayerBase;
using static Sandbox.Component;

namespace ResourceFarmer.Resources;

// Ensure the class implements ITriggerListener if not inheriting from a base class that does
public sealed partial class ResourceNode : Interactable, IGatherable, ITriggerListener // Use partial if split
{
	[Property] public ResourceType ResourceType { get; set; }
	[Property] public float MinAmount { get; set; } = 5.0f;
	[Property] public float MaxAmount { get; set; } = 15.0f;
	[Property] public int MinDifficulty { get; set; } = 0; // Keep Min/Max for initialization
	[Property] public int MaxDifficulty { get; set; } = 5;
	[Property] public ResourceType RequiredToolType { get; set; } = ResourceType.None; // Default to None if gatherable by hand
	[Property] public int RequiredToolLevel { get; set; } = 1;
	[Property] public float InteractionRange { get; set; } = 150f; // Used by PlayerInteractionComponent

	[Property] public BoxCollider OutlineCollider { get; set; }

	// --- Effect Properties ---
	[Property, Category( "Effects" )] public GameObject HitEffectPrefab { get; set; }
	[Property, Category( "Effects" )] public SoundEvent HitSound { get; set; }

	[Sync, Property] private float _amount { get; set; }
	[Sync, Property] private double _difficulty { get; set; } // Synced difficulty

	public float Amount => _amount;
	public double Difficulty => _difficulty;

	private PanelComponent _cachedPanelComponent;
	public PanelComponent GetPanelComponent() => _cachedPanelComponent;

	private bool _isLocalPlayerNearby = false;
	private Player _localPlayerReference = null;

	protected override void OnAwake()
	{
		_cachedPanelComponent = Components.Get<PanelComponent>( FindMode.InChildren );
		if ( _cachedPanelComponent != null ) _cachedPanelComponent.Enabled = false;

		if ( Networking.IsClient ) return;

		var rand = Game.Random;
		_amount = rand.Float( MinAmount, MaxAmount );
		_difficulty = rand.Double( MinDifficulty, MaxDifficulty ); // Use double for difficulty calculation consistency
	}

	// --- Trigger Enter/Exit for Outline (Client-Side) ---
	void ITriggerListener.OnTriggerEnter( Collider other )
	{
		if ( !other.GameObject.Network.IsOwner ) return;
		var player = other.GameObject.Components.GetInParentOrSelf<Player>();
		if ( player != null )
		{
			_isLocalPlayerNearby = true;
			_localPlayerReference = player;
		}
	}
	void ITriggerListener.OnTriggerExit( Collider other )
	{
		if ( !other.GameObject.Network.IsOwner ) return;
		var player = other.GameObject.Components.GetInParentOrSelf<Player>();
		if ( player != null && player == _localPlayerReference )
		{
			_isLocalPlayerNearby = false;
			_localPlayerReference = null;
			var highlight = Components.Get<HighlightOutline>();
			if ( highlight != null ) highlight.Enabled = false;
		}
	}

	/// <summary>
	/// Called Server-Side by PlayerGatheringComponent.ProcessHit
	/// </summary>
	public void Gather( Player player, float amountToGather, ToolBase tool = null )
	{
		if ( Networking.IsClient ) return;
		if ( _amount <= 0 ) return;

		float actualGathered = Math.Min( _amount, amountToGather );
		player?.GatherResource( ResourceType, _difficulty, actualGathered ); // Pass difficulty for XP calc
		_amount -= actualGathered;

		if ( _amount <= 0.01f )
		{
			Log.Info( $"[ResourceNode Server Gather] {GameObject.Name} depleted. Destroying." );
			if ( _cachedPanelComponent?.IsValid() ?? false ) DisablePanelClientRpc();
			GameObject.Destroy();
		}
	}

	/// <summary>
	/// Plays hit effects on all clients. Called via RPC from the server (PlayerInteractionComponent).
	/// </summary>
	[Rpc.Broadcast( NetFlags.Reliable )]
	public void PlayHitEffect( Vector3 pos, Vector3 normal )
	{
		if ( HitSound != null ) Sound.Play( HitSound, pos );
		if ( HitEffectPrefab != null )
		{
			var rot = Rotation.LookAt( normal );
			var effectObject = HitEffectPrefab.Clone( pos, rot );
			Task.Delay( 2000 ).ContinueWith( _ =>
			{
				if ( effectObject.IsValid() )
				{
					effectObject.Destroy();
				}
			} );
		}
	}

	[Rpc.Broadcast( NetFlags.Reliable )]
	private void DisablePanelClientRpc()
	{
		if ( _cachedPanelComponent != null && _cachedPanelComponent.IsValid() )
		{
			_cachedPanelComponent.Enabled = false;
		}
	}

	/// <summary>
	/// Handles interactions not covered by the primary gather hit.
	/// </summary>
	public override void Interact( Player player, bool isPrimary )
	{
		if ( Networking.IsClient ) return;
		Log.Info( $"[ResourceNode Server Interact] Interact called. IsPrimary: {isPrimary}" );
		// Primary is handled by hit checks, maybe secondary does something else?
	}


	/// <summary>
	/// Calculates the outline color based on tool requirements and player capability vs difficulty.
	/// Runs Client-Side within OnUpdate.
	/// </summary>
	public Color GetOutlineColor( Player player, ToolBase tool )
	{
		if ( player == null ) return Color.Transparent; // No player context, no color

		// --- Check Tool Requirements ---
		bool requirementsMet = false;
		if ( RequiredToolType == ResourceType.None )
		{
			// No specific tool needed (maybe still check level?)
			requirementsMet = (tool?.Level ?? 0) >= RequiredToolLevel; // Allow fist gathering if level req is met (Level 0 for fists?)
		}
		else
		{
			// Specific tool required
			requirementsMet = tool != null &&
							  tool.ToolType == RequiredToolType &&
							  tool.Level >= RequiredToolLevel;
		}

		// If requirements are NOT met, show Red
		if ( !requirementsMet ) return Color.Red;

		// --- Requirements Met: Calculate color based on difficulty vs player level ---
		// Using player level as the main indicator of capability against difficulty.
		// Tool bonuses might make gathering *faster* or yield *more*, but the color reflects basic possibility/struggle.
		double baseNodeDifficulty = _difficulty; // Use the node's difficulty
		double playerLevel = player.Level;
		double diff = baseNodeDifficulty - playerLevel; // Simple comparison: Node Difficulty vs Player Level

		// Color Gradient based on difficulty difference
		if ( diff <= -2 ) // Player level significantly higher than difficulty
		{
			double t = Math.Clamp( (diff - (-8)) / (-2 - (-8)), 0d, 1d ); // Normalize between -8 and -2
			return Color.Lerp( Color.Green, Color.Cyan, (float)t ); // Easy (Green -> Cyan)
		}
		else if ( diff <= 1 ) // Player level slightly higher, equal, or slightly lower
		{
			double t = Math.Clamp( (diff - (-2)) / (1 - (-2)), 0d, 1d ); // Normalize between -2 and 1
			return Color.Lerp( Color.Cyan, Color.Yellow, (float)t ); // Moderate (Cyan -> Yellow)
		}
		else if ( diff <= 4 ) // Player level lower than difficulty
		{
			double t = Math.Clamp( (diff - 1) / (4 - 1), 0d, 1d ); // Normalize between 1 and 4
			return Color.Lerp( Color.Yellow, Color.Orange, (float)t ); // Hard (Yellow -> Orange)
		}
		else // Player level significantly lower than difficulty
		{
			return Color.Orange; // Very Hard (Orange) - Changed from Red as Red is for wrong tool
		}
	}

	/// <summary>
	/// Handles client-side outline updates based on proximity.
	/// </summary>
	protected override void OnUpdate()
	{
		if ( IsProxy ) return; // Client only

		var highlight = Components.GetOrCreate<HighlightOutline>();
		highlight.Enabled = _isLocalPlayerNearby; // Enable based on trigger presence

		if ( highlight.Enabled && _localPlayerReference.IsValid() )
		{
			// Calculate and apply color using the cached local player reference
			Color outlineColor = GetOutlineColor( _localPlayerReference, _localPlayerReference.EquippedTool );

			highlight.Color = outlineColor;
			highlight.InsideColor = outlineColor.WithAlpha( 0.1f );
			highlight.Width = 0.1f; // Set desired outline width
			highlight.ObscuredColor = outlineColor.WithAlpha( 0.05f );
			highlight.InsideObscuredColor = outlineColor.WithAlpha( 0.02f );
		}
	}
}
