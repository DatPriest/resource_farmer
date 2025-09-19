// File: Code/Player/PlayerContextualControlsComponent.cs
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using ResourceFarmer.Resources;

namespace ResourceFarmer.PlayerBase;

/// <summary>
/// Component that tracks what the player is currently looking at and provides contextual control hints.
/// Used by the HUD to display dynamic control prompts like "Press [E] to gather".
/// </summary>
public sealed class PlayerContextualControlsComponent : Component
{
	[Property] public Player OwnerPlayer { get; set; }
	[Property] public float ScanDistance { get; set; } = 100f;
	[Property] public float ScanRadius { get; set; } = 8f;

	// Current interaction context
	[Net] public string CurrentPrompt { get; private set; } = "";
	[Net] public bool HasInteractable { get; private set; } = false;

	// Cached raycast results to avoid excessive calculations
	private TimeSince _lastScan = 0f;
	private const float ScanInterval = 0.1f; // Scan 10 times per second

	protected override void OnAwake()
	{
		if (OwnerPlayer == null)
		{
			OwnerPlayer = Components.GetInParentOrSelf<Player>();
		}
	}

	protected override void OnUpdate()
	{
		// Only scan for the local player to avoid unnecessary network traffic
		if (!Network.IsOwner || OwnerPlayer == null || !OwnerPlayer.IsValid)
			return;

		if (_lastScan > ScanInterval)
		{
			ScanForInteractables();
			_lastScan = 0f;
		}
	}

	/// <summary>
	/// Performs a raycast to detect what the player is looking at and updates the contextual prompt.
	/// </summary>
	private void ScanForInteractables()
	{
		// Get the interaction component to use its distance calculation
		var interactionComp = OwnerPlayer.Components.Get<PlayerInteractionComponent>();
		var scanDistance = interactionComp?.GetLocalInteractionDistance() ?? ScanDistance;

		// Perform raycast from camera or eye position
		Ray ray;
		var camera = Scene.Camera;
		if (camera != null)
		{
			ray = camera.Transform.World.ForwardRay;
		}
		else
		{
			// Fallback to player's eye position
			var eyePos = OwnerPlayer.Eye?.Transform.Position ?? OwnerPlayer.Transform.Position + Vector3.Up * OwnerPlayer.EyeHeight;
			var eyeDir = OwnerPlayer.Eye?.Transform.Rotation.Forward ?? OwnerPlayer.Transform.Rotation.Forward;
			ray = new Ray(eyePos, eyeDir);
		}

		var tr = Scene.Trace.Ray(ray, scanDistance)
			.Radius(ScanRadius)
			.WithoutTags("Player")
			.Run();

		string newPrompt = "";
		bool hasTarget = false;

		if (tr.Hit)
		{
			// Check for resource node
			var resourceNode = tr.GameObject.Components.Get<ResourceNode>();
			if (resourceNode != null && resourceNode.IsValid())
			{
				newPrompt = GetResourceNodePrompt(resourceNode);
				hasTarget = true;
			}
			else
			{
				// Check for other interactables
				var interactable = tr.GameObject.Components.Get<IInteractable>();
				if (interactable != null)
				{
					newPrompt = GetInteractablePrompt(tr.GameObject);
					hasTarget = true;
				}
			}
		}

		// Update networked properties if changed
		if (CurrentPrompt != newPrompt)
		{
			CurrentPrompt = newPrompt;
		}

		if (HasInteractable != hasTarget)
		{
			HasInteractable = hasTarget;
		}
	}

	/// <summary>
	/// Generate contextual prompt text for resource nodes.
	/// </summary>
	private string GetResourceNodePrompt(ResourceNode node)
	{
		var tool = OwnerPlayer.EquippedTool;
		
		// Check if player has the required tool
		bool canGather = false;
		string toolRequirement = "";

		if (node.RequiredToolType == ResourceType.None)
		{
			canGather = true;
		}
		else
		{
			canGather = tool != null && 
					   tool.ToolType == node.RequiredToolType && 
					   tool.Level >= node.RequiredToolLevel;

			if (!canGather)
			{
				toolRequirement = $" (Need {node.RequiredToolType} Lvl {node.RequiredToolLevel})";
			}
		}

		string resourceName = node.ResourceType.ToString();
		
		if (canGather)
		{
			// Show different prompts based on tool efficiency
			if (tool != null)
			{
				var efficiency = tool.GetGatherAmountMultiplier(node.ResourceType);
				if (efficiency >= 1.5f)
				{
					return $"Press [LMB] to efficiently gather {resourceName}";
				}
				else if (efficiency >= 1.0f)
				{
					return $"Press [LMB] to gather {resourceName}";
				}
				else
				{
					return $"Press [LMB] to slowly gather {resourceName}";
				}
			}
			else
			{
				return $"Press [LMB] to gather {resourceName} by hand";
			}
		}
		else
		{
			return $"Cannot gather {resourceName}{toolRequirement}";
		}
	}

	/// <summary>
	/// Generate contextual prompt text for generic interactables.
	/// </summary>
	private string GetInteractablePrompt(GameObject target)
	{
		// Check for specific interactable types and provide appropriate prompts
		var interactable = target.Components.Get<IInteractable>();
		if (interactable is Interactable baseInteractable && !string.IsNullOrEmpty(baseInteractable.InteractionPrompt))
		{
			return $"Press [RMB] to {baseInteractable.InteractionPrompt}";
		}

		// Default interaction prompt
		return $"Press [RMB] to interact with {target.Name}";
	}

	/// <summary>
	/// Get the current contextual controls for display in the HUD.
	/// Returns a list of control hints that should be shown.
	/// </summary>
	public List<ContextualControl> GetCurrentControls()
	{
		var controls = new List<ContextualControl>();

		// Primary contextual action
		if (HasInteractable && !string.IsNullOrEmpty(CurrentPrompt))
		{
			controls.Add(new ContextualControl
			{
				Text = CurrentPrompt,
				IsEnabled = true,
				Priority = 1
			});
		}

		// Secondary always-available controls (only show most relevant ones)
		if (controls.Count == 0) // Only show if no primary interaction
		{
			controls.Add(new ContextualControl
			{
				Text = "Press [I] to open inventory",
				IsEnabled = true,
				IsSecondary = true,
				Priority = 10
			});
		}

		// Sort by priority (lower numbers = higher priority)
		return controls.OrderBy(c => c.Priority).ToList();
	}
}

/// <summary>
/// Represents a contextual control hint for the HUD.
/// </summary>
public struct ContextualControl
{
	public string Text { get; set; }
	public bool IsEnabled { get; set; }
	public bool IsSecondary { get; set; } // Less prominent display
	public int Priority { get; set; } // Lower numbers = higher priority (1 = highest)
}