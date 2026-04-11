using Unity.Netcode;

public class Entity : NetworkBehaviour
{
    #region Virtual Overrides
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        foreach (var component in GetComponentsInChildren<EntityComponent>())
        {
            component.Initialize(NetworkObjectId);
        }
    }
    #endregion
}
