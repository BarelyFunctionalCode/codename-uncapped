using Unity.Netcode;

public class EntityComponent : NetworkBehaviour
{
    public bool IsInitialized { get; private set; } = false;
    public virtual void Initialize(ulong ParentNetworkObjectId, bool isServer) { IsInitialized = true; }
}
