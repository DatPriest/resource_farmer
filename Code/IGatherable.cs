using ResourceFarmer.PlayerBase;
using ResourceFarmer.Items;

namespace ResourceFarmer;

public interface IGatherable
{
    /// <summary>
    /// Called when a player successfully gathers from this object.
    /// </summary>
    /// <param name="player">The player gathering.</param>
    /// <param name="gatherAmount">The amount gathered in this cycle.</param>
    /// <param name="tool">The tool used (can be null).</param>
    void Gather( Player player, float gatherAmount, ToolBase tool = null );
}
