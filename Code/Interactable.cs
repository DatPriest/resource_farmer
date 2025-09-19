using ResourceFarmer.PlayerBase;
using Sandbox; // Add Sandbox namespace for attributes

namespace ResourceFarmer;

public interface IInteractable
{
	void Interact( Player player, bool isPrimary );
}

public class Interactable : Component, IInteractable
{
	[Property, Group( "Interaction" )] public new bool Enabled { get; set; } = true; // Added 'new' keyword
	[Sync, Property, Group( "Interaction" )] public string InteractionPrompt { get; protected set; } = "Interact";

	public virtual void Interact( Player player, bool isPrimary )
	{
		// Default: do nothing
	}


}
