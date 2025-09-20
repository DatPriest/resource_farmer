using Sandbox;

namespace ResourceFarmer.Game;

/// <summary>
/// Spawns a player prefab for each connecting client and assigns ownership.
/// Attach in the scene and set <see cref="PlayerPrefab"/> to Assets/Prefabs/player.prefab.
/// </summary>
public sealed class PlayerSpawnSystem : Component, Component.INetworkListener
{
    [Property] public GameObject PlayerPrefab { get; set; }

    public void OnActive( Connection connection )
    {
        if ( !Networking.IsHost || connection is null )
            return;

        if ( PlayerPrefab is null )
        {
            Log.Warning( "[PlayerSpawnSystem] PlayerPrefab is not set." );
            return;
        }

        var playerGo = PlayerPrefab.Clone( Transform.Position );
        if ( playerGo is null )
            return;

        playerGo.Name = $"Player - {connection.DisplayName}";
        playerGo.Tags.Add( "player" );
        playerGo.NetworkSpawn( connection );
    }
}
