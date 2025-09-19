using Sandbox;
using Sandbox.UI;
using System;

namespace ResourceFarmer.UI.Components;

/// <summary>
/// Base component for any UI panel that should be displayed in world space.
/// Provides common functionality for positioning, visibility range, and lifecycle management.
/// </summary>
public abstract class WorldPanelComponent : PanelComponent
{
	/// <summary>
	/// The maximum distance at which this panel becomes visible to players
	/// </summary>
	[Property] public float VisibilityRange { get; set; } = 150f;
	
	/// <summary>
	/// Height offset from the parent GameObject's position for the panel
	/// </summary>
	[Property] public float HeightOffset { get; set; } = 50f;
	
	/// <summary>
	/// Whether this panel should always face the camera
	/// </summary>
	[Property] public bool FaceCamera { get; set; } = true;
	
	/// <summary>
	/// Scale factor for the panel based on distance (closer = larger)
	/// </summary>
	[Property] public bool UseDistanceScaling { get; set; } = true;
	
	/// <summary>
	/// Minimum scale when at maximum visibility range
	/// </summary>
	[Property] public float MinScale { get; set; } = 0.5f;
	
	/// <summary>
	/// Maximum scale when very close
	/// </summary>
	[Property] public float MaxScale { get; set; } = 1.2f;
	
	/// <summary>
	/// The target GameObject this panel is attached to (defaults to parent)
	/// </summary>
	[Property] public GameObject TargetObject { get; set; }
	
	/// <summary>
	/// Whether the panel is currently within visibility range of any player
	/// </summary>
	public bool IsInRange { get; private set; }
	
	/// <summary>
	/// The closest player currently in range (if any)
	/// </summary>
	public GameObject ClosestPlayer { get; private set; }
	
	/// <summary>
	/// Current distance to the closest player
	/// </summary>
	public float DistanceToClosestPlayer { get; private set; } = float.MaxValue;
	
	protected override void OnAwake()
	{
		base.OnAwake();
		
		if (TargetObject == null)
			TargetObject = GameObject;
			
		// Start disabled - will be enabled by WorldPanelVisibilityManager
		Enabled = false;
	}
	
	/// <summary>
	/// Called by WorldPanelVisibilityManager when a player enters/exits range
	/// </summary>
	public virtual void UpdateVisibility(bool inRange, GameObject closestPlayer, float distance)
	{
		var wasInRange = IsInRange;
		IsInRange = inRange;
		ClosestPlayer = closestPlayer;
		DistanceToClosestPlayer = distance;
		
		if (inRange != wasInRange)
		{
			if (inRange)
				OnPlayerEnterRange(closestPlayer, distance);
			else
				OnPlayerExitRange();
		}
		
		if (inRange)
			OnPlayerInRange(closestPlayer, distance);
	}
	
	/// <summary>
	/// Called when the first player enters visibility range
	/// </summary>
	protected virtual void OnPlayerEnterRange(GameObject player, float distance)
	{
		Enabled = true;
	}
	
	/// <summary>
	/// Called when the last player exits visibility range
	/// </summary>
	protected virtual void OnPlayerExitRange()
	{
		Enabled = false;
	}
	
	/// <summary>
	/// Called every frame while at least one player is in range
	/// </summary>
	protected virtual void OnPlayerInRange(GameObject player, float distance)
	{
		UpdateScaleBasedOnDistance(distance);
		UpdatePositioning();
	}
	
	/// <summary>
	/// Updates the panel's scale based on distance to player
	/// </summary>
	protected virtual void UpdateScaleBasedOnDistance(float distance)
	{
		if (!UseDistanceScaling) return;
		
		float normalizedDistance = Math.Clamp(distance / VisibilityRange, 0f, 1f);
		float scale = MaxScale + (MinScale - MaxScale) * normalizedDistance;
		Style.Scale = scale;
	}
	
	/// <summary>
	/// Updates panel positioning (world-to-screen conversion, camera facing, etc.)
	/// </summary>
	protected virtual void UpdatePositioning()
	{
		if (!IsEnabled || TargetObject == null || !TargetObject.IsValid())
			return;
			
		// Calculate world position with height offset
		var worldPos = TargetObject.Transform.Position + Vector3.Up * HeightOffset;
		
		// Convert to screen position
		var screenPos = Scene.Camera.PointToScreenPixels(worldPos);
		
		// Apply position (S&box UI coordinate system)
		Style.Left = Length.Pixels(screenPos.x);
		Style.Top = Length.Pixels(screenPos.y);
		Style.Position = PositionMode.Absolute;
		
		// Handle camera facing rotation if enabled
		if (FaceCamera && Scene.Camera != null)
		{
			// For UI panels, we typically don't need rotation, but we could add it here if needed
			// The panel will naturally face the camera due to screen-space positioning
		}
	}
	
	/// <summary>
	/// Abstract method for subclasses to implement their content updates
	/// </summary>
	protected abstract void UpdateContent();
	
	protected override void OnUpdate()
	{
		base.OnUpdate();
		
		if (IsInRange && Enabled)
		{
			UpdateContent();
		}
	}
	
	protected override int BuildHash()
	{
		return HashCode.Combine(
			base.BuildHash(),
			IsInRange,
			DistanceToClosestPlayer,
			TargetObject?.Id
		);
	}
}