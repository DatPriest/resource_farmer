// File: Code/Test/ResourceCollectionTest.cs
// Simple test/verification script for the resource collection system
using Sandbox;
using ResourceFarmer.Resources;
using ResourceFarmer.PlayerBase;
using System.Linq;

namespace ResourceFarmer.Testing;

/// <summary>
/// Test component to verify resource collection system functionality.
/// Attach this to a test object in the scene to verify the system works.
/// </summary>
public sealed class ResourceCollectionTest : Component
{
[Property] public bool RunTest { get; set; } = false;
[Property] public GameObject TestResourceNode { get; set; }
[Property] public Player TestPlayer { get; set; }

protected override void OnUpdate()
{
if ( !RunTest ) return;
if ( !Networking.IsHost ) return; // Only run on server

RunTest = false; // Run once

Log.Info( "=== Resource Collection System Test ===" );

// Test 1: Check if player components exist
if ( TestPlayer != null )
{
var gathering = TestPlayer.Components.Get<PlayerGatheringComponent>();
var interaction = TestPlayer.Components.Get<PlayerInteractionComponent>();

Log.Info( $"Player has GatheringComponent: {gathering != null}" );
Log.Info( $"Player has InteractionComponent: {interaction != null}" );
Log.Info( $"Player initial inventory count: {TestPlayer.Inventory.Count}" );
}

// Test 2: Check if resource node is properly configured
if ( TestResourceNode != null )
{
var resourceNode = TestResourceNode.Components.Get<ResourceNode>();
if ( resourceNode != null )
{
Log.Info( $"ResourceNode Type: {resourceNode.ResourceType}" );
Log.Info( $"ResourceNode Amount: {resourceNode.Amount}" );
Log.Info( $"ResourceNode Difficulty: {resourceNode.Difficulty}" );
Log.Info( $"ResourceNode Required Tool: {resourceNode.RequiredToolType}" );
Log.Info( $"ResourceNode Required Level: {resourceNode.RequiredToolLevel}" );
}
}

// Test 3: Simulate gathering (if both components exist)
if ( TestPlayer != null && TestResourceNode != null )
{
var resourceNode = TestResourceNode.Components.Get<ResourceNode>();
var gathering = TestPlayer.Components.Get<PlayerGatheringComponent>();

if ( resourceNode != null && gathering != null )
{
Log.Info( "=== Simulating Resource Gathering ===" );
var initialAmount = resourceNode.Amount;
var initialInventory = TestPlayer.Inventory.ToDictionary( kvp => kvp.Key, kvp => kvp.Value );

// Simulate a hit
gathering.ProcessHit( resourceNode );

Log.Info( $"ResourceNode amount before: {initialAmount}, after: {resourceNode.Amount}" );
Log.Info( $"Player inventory changes:" );

foreach ( var kvp in TestPlayer.Inventory )
{
var beforeAmount = initialInventory.ContainsKey( kvp.Key ) ? initialInventory[kvp.Key] : 0f;
if ( beforeAmount != kvp.Value )
{
Log.Info( $"  {kvp.Key}: {beforeAmount} -> {kvp.Value} (+{kvp.Value - beforeAmount})" );
}
}
}
}

Log.Info( "=== Resource Collection System Test Complete ===" );
}
}
