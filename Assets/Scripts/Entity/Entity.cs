using Unity.Netcode;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    [HideInInspector] public State state = null;
    [HideInInspector] public Identification identification = null;
    [HideInInspector] public Health health = null;
    [HideInInspector] public Energy energy = null;

    protected bool baseEntityInitialized = false;

    public override void OnNetworkSpawn()
    {
        TryGetComponent(out state);
        TryGetComponent(out identification);
        TryGetComponent(out health);
        TryGetComponent(out energy);

        base.OnNetworkSpawn();

        foreach (var component in GetComponentsInChildren<EntityComponent>())
        {
            component.Initialize(NetworkObjectId, IsServer);
        }
        baseEntityInitialized = true;
    }
}
