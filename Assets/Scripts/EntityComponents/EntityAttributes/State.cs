using Unity.Netcode;
using UnityEngine;

public enum EntityStates
{
    NONE,
    DEAD,
    ALIVE,
    GROUNDED,
    UNGROUNDED,
}

public class State : EntityAttributes
{
    private NetworkVariable<bool> _isDead = new(false);
    protected bool _isGrounded = false;

    public bool IsDead => _isDead.Value;
    public bool IsGrounded => _isGrounded;

    public void ChangeState(EntityStates s)
    {
        SendMessage("OnEntityStateChange", s);
    }

    public void OnEntityRespawn()
    {
        _isDead.Value = false;
        ChangeState(EntityStates.ALIVE);
    }

    public void Died()
    {
        _isDead.Value = true;
        ChangeState(EntityStates.DEAD);
    }

    public void SetIsGrounded(bool b)
    {
        _isGrounded = b;
        if (b)
        {
            ChangeState(EntityStates.GROUNDED);
        }
        else
        {
            ChangeState(EntityStates.UNGROUNDED);
        }
    }
    // TODO Add trigger for IsGrounded to emit ChangeState()
}
