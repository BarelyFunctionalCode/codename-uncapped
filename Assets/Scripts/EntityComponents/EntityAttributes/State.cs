using Unity.Netcode;
using UnityEngine.Events;

public enum EntityStates
{
    NONE,
    DEAD,
    RESPAWN,
    ALIVE,
    GROUNDED,
    UNGROUNDED,
}

public class State : EntityAttributes
{
    public UnityEvent<EntityStates> onStateChange = new();

    private NetworkVariable<bool> _isDead = new(false);
    protected bool _isGrounded = false;

    public bool IsDead => _isDead.Value;
    public bool IsGrounded => _isGrounded;


    public void ChangeState(EntityStates s) => onStateChange.Invoke(s);

    public void SetIsGrounded(bool b)
    {
        _isGrounded = b;
        if (b) ChangeState(EntityStates.GROUNDED);
        else ChangeState(EntityStates.UNGROUNDED);
    }

    public void Die()
    {
        if (_isDead.Value) return;
        _isDead.Value = true;
        ChangeState(EntityStates.DEAD);
        OnDie();
    }
    protected virtual void OnDie() {}

    public void Respawn()
    {
        if (!IsServer) return;
        OnRespawn();
        ChangeState(EntityStates.RESPAWN);
        _isDead.Value = false;
        ChangeState(EntityStates.ALIVE);
    }
    protected virtual void OnRespawn() {}
}
