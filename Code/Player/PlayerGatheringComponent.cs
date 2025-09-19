// File: Code/Player/PlayerGatheringComponent.cs
using Sandbox;
using System;
using ResourceFarmer.Resources;
namespace ResourceFarmer.PlayerBase;
public sealed class PlayerGatheringComponent : Component
{
	[Property] public Player OwnerPlayer { get; set; }

	// Amount gathered per successful hit (can be adjusted)
	[Property] public float BaseGatherAmountPerHit { get; set; } = 1.0f;

	// No more timer variables needed
	// [Sync] public bool IsGathering { get; private set; } = false;
	// [Sync] public double TotalGatherTime { get; private set; } = 0f;
	// [Sync] public float CurrentGatherTimeElapsed { get; private set; } = 0f;
	// private ResourceNode _gatherTarget = null;
	// private TimeSince _gatherTimeSinceStarted = 0;

	protected override void OnAwake()
	{
		if ( OwnerPlayer == null )
		{
			OwnerPlayer = Components.GetInParentOrSelf<Player>();
		}
	}

	/// <summary>
	/// Processes a successful hit on a resource node.
	/// Called by PlayerInteractionComponent after a raycast confirms a hit.
	/// Calculates the amount to gather and tells the player to gather it.
	/// Runs Server-Side (called from InteractionComponent's RPC).
	/// </summary>
	public void ProcessHit( ResourceNode targetNode )
	{
		if ( OwnerPlayer == null || targetNode == null || !targetNode.IsValid() ) return;

		var tool = OwnerPlayer.EquippedTool; // ToolBase instance

		// Check basic type/level requirements first
		bool requirementsMet = tool != null &&
							   tool.ToolType == targetNode.RequiredToolType &&
							   tool.Level >= targetNode.RequiredToolLevel;

		if ( targetNode.RequiredToolType == ResourceType.None || requirementsMet )
		{
			// --- Calculate Amount ---
			float amountMultiplier = tool?.GetGatherAmountMultiplier( targetNode.ResourceType ) ?? 0.5f; // Use 50% if no tool/wrong type
			float playerLevelBonus = 1.0f + (OwnerPlayer.Level * 0.02f); // Smaller level bonus to amount
			float finalAmount = BaseGatherAmountPerHit * amountMultiplier * playerLevelBonus;

			// --- Check for Critical Hit ---
			float critChance = tool?.GetCritChance() ?? 0f;
			if ( Game.Random.Float( 0f, 1f ) < critChance )
			{
				float critMultiplier = tool?.GetCritMultiplier() ?? 1.5f;
				finalAmount *= critMultiplier;
				Log.Info( $"*** Critical Gather! Amount: {finalAmount:F2} ***" );
				// Optional: Play crit effect/sound
			}

			// --- Call Gather on Node ---
			targetNode.Gather( OwnerPlayer, finalAmount, tool ); ;
		}
		else
		{
			Log.Info( $"[GatheringComponent Server] Hit on {targetNode.GameObject.Name} but tool requirements not met (Need {targetNode.RequiredToolType} Lvl {targetNode.RequiredToolLevel})." );
			// Optional: Play a "thud" sound or give visual feedback for wrong tool
		}
	}

	// No TryStartGathering needed anymore
	// No CancelGathering needed anymore
	// No OnFixedUpdate needed for timer logic anymore
}
