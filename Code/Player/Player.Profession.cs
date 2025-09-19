using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResourceFarmer.Resources;

namespace ResourceFarmer.PlayerBase
{
	public sealed partial class Player : Component
	{
		// TODO: Implement actual profession system
		/// <summary>
		/// Placeholder method to get the player's level in a profession relevant to the tool type.
		/// </summary>
		/// <param name="toolType">The type of tool being crafted.</param>
		/// <returns>The player's level in that profession (placeholder value).</returns>
		public int GetProfessionLevel( ResourceType toolType )
		{
			// Placeholder: Return player's main level / 2 for now, or a fixed value
			// Replace with actual profession lookup logic later
			return Math.Max( 1, this.Level / 2 ); // Example: Level 10 player has ~Level 5 profession
		}

		// TODO: Define the maximum possible profession level
		public int GetMaxProfessionLevel()
		{
			return 50; // Example max level
		}
	}
}
