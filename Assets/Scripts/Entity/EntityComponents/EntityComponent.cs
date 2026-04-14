using Unity.Netcode;
using UnityEngine.Events;

public class EntityComponent : NetworkBehaviour
{
    protected Entity entity;
    public bool IsInitialized { get; private set; } = false;

    public virtual void Initialize(Entity entity)
    {
        IsInitialized = true;
        this.entity = entity;
    }
    
    public virtual void Initialize(Entity entity, UnityAction<EntityStates> onStateChangeCallback = null)
    {
        IsInitialized = true;
        this.entity = entity;
        
        if (IsServer && onStateChangeCallback != null && entity.state != null)
        {
            entity.state.onStateChange.AddListener(onStateChangeCallback);
        }
    }
    public virtual void Deinitialize() { IsInitialized = false; }
}
