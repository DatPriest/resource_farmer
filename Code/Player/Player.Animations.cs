#nullable enable
using Sandbox;
using Sandbox.Citizen; // Required for CitizenAnimationHelper and HoldTypes
using System;
using System.Threading.Tasks;

namespace ResourceFarmer.PlayerBase
{
	/// <summary>
	/// Player class for handling animations and interactions.
	/// This is a partial class, so it should be defined in a separate file.
	/// </summary>
	[Library( "player" )]
	[Title( "Player" )]
	[Category( "Player" )]
	[Icon( "person" )]

	public sealed partial class Player : Component // Use 'partial' keyword
	{
		// Animation-related properties remain the same
		[Sync] public CitizenAnimationHelper.HoldTypes CurrentHoldType { get; private set; } = CitizenAnimationHelper.HoldTypes.None;
		[Sync] public Vector3 WishVelocity { get; private set; } // Calculation remains in main Player.cs OnUpdate

		public RealTimeSince LastAttackTime { get; private set; } = 0f;

		/// <summary>
		/// Updates the CitizenAnimationHelper parameters based on player state using the available properties and methods.
		/// Should be called in OnUpdate for the local player.
		/// </summary>
		private void UpdateAnimationParameters()
		{
			if ( IsProxy || AnimationHelper == null ) return;

			// --- Determine Hold Type ---
			// Use Swing instead of Melee, based on the provided CitizenAnimationHelper enum
			var targetHoldType = EquippedTool != null ? CitizenAnimationHelper.HoldTypes.Swing : CitizenAnimationHelper.HoldTypes.Punch;
			if ( CurrentHoldType != targetHoldType )
			{
				CurrentHoldType = targetHoldType;
			}

			if ( LastAttackTime > 0.5f )
			{
				CurrentHoldType = CitizenAnimationHelper.HoldTypes.None; // Reset hold type after attack
			}

			// --- Set Core Animation Parameters ---
			// Use available methods and direct property assignments
			//AnimationHelper.WithVelocity( Controller?.Velocity ?? Vector3.Zero );
			AnimationHelper.WithWishVelocity( WishVelocity ); // Ensure WishVelocity is calculated in Player.cs OnUpdate

			// Use direct property assignments for boolean states
			AnimationHelper.IsGrounded = Controller?.IsOnGround ?? true;
			AnimationHelper.IsSitting = false; // Set based on actual state if sitting is possible
			AnimationHelper.IsSwimming = false; // Set based on actual state if swimming is possible
			AnimationHelper.IsClimbing = false; // Set based on actual state if climbing is possible

			// Use DuckLevel property (0 to 1)
			AnimationHelper.DuckLevel = Input.Down( "Duck" ) ? 1.0f : 0.0f;

			// Set HoldType property directly
			AnimationHelper.HoldType = CurrentHoldType;

			// WithLook handles facing/aiming direction
			var lookDir = Eye.Transform.Rotation.Forward;
			AnimationHelper.WithLook( lookDir, 1.0f, 0.75f, 0.5f ); // Adjust weights as needed
		}

		/// <summary>
		/// Triggers the attack animation parameter. Call this locally for the owner.
		/// </summary>
		private void TriggerAttackAnimation( bool isSecondary = false )
		{
			if ( LastAttackTime < 0.5f ) return; // Prevent spamming
			LastAttackTime = 0f; // Reset attack time
			if ( IsProxy || AnimationHelper?.Target == null ) return; // Run only for owner

			// Use the standard boolean attack parameter - assumes "b_attack" exists in your anim graph
			AnimationHelper.Target.Set( "b_attack", true );

			// Optional: If your graph uses an integer for different attack types/hands:
			// int attackIndex = isSecondary ? 2 : Game.Random.Int(1, 2); // Example: 1=Right, 2=Left
			// AnimationHelper.Target.Set( "attack_index", attackIndex ); // Parameter name might differ!

			Log.Info( $"[Player.Animations] Triggered b_attack" );
		}
	}
}
