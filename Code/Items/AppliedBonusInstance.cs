// File: Code/Items/AppliedBonusInstance.cs (New File)
using Sandbox; // Required for NetRead/Write if used in NetworkTable
using Sandbox.Network;
namespace ResourceFarmer.Items
{
	// Struct to hold the specific instance of a bonus on a tool, including its rolled magnitude.
	// REMOVED INetworkSerializable - Relying on containing object (ToolBase) for sync.
	public struct AppliedBonusInstance
	{
		public ToolBonusName Name { get; set; }
		public float ActualMagnitude { get; set; }

		// Read/Write methods removed as INetworkSerializable was removed.
	}
}
