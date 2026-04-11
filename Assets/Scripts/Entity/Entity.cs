using Unity.Netcode;

public class Entity : NetworkBehaviour
{
    public State state = null;
    public Identification identification = null;
    public Health health = null;
    public Energy energy = null;

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
