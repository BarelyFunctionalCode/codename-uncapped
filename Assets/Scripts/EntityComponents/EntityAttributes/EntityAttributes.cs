using Unity.Netcode;

public class EntityAttributes : NetworkBehaviour
{
    public bool IsInitialized { get; private set; } = false;
    public virtual void Initialize(ulong ParentNetworkObjectId) { IsInitialized = true; }
}
