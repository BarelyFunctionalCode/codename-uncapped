using Unity.Netcode;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    [HideInInspector] public State state = null;
    [HideInInspector] public Identification identification = null;
    [HideInInspector] public Health health = null;
    [HideInInspector] public Energy energy = null;
    [HideInInspector] public PickupContainer pickupContainer = null;
    [HideInInspector] public Pickup pickup = null;

    protected bool baseEntityInitialized = false;

    public override void OnNetworkSpawn()
    {
        TryGetComponent(out state);
        TryGetComponent(out identification);
        TryGetComponent(out health);
        TryGetComponent(out energy);
        TryGetComponent(out pickupContainer);
        TryGetComponent(out pickup);

        base.OnNetworkSpawn();

        foreach (var component in GetComponentsInChildren<EntityComponent>())
        {
            component.Initialize(NetworkObjectId, IsServer);
        }
        baseEntityInitialized = true;
    }
}
