using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System; // Is this include necessary?

public class Entity : NetworkBehaviour, IDamageable
{
    private const float groundEnergyRegenRate = 12.5f;
    
    private NetworkVariable<bool> _isDead = new(false);
    protected bool _isGrounded = false;

    #region Virtual Overrides
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        SendMessage(
            "InitializeComponents",
            NetworkObjectId,
            SendMessageOptions.DontRequireReceiver
        );
    }
    #endregion

    // TODO refactor this `Die` call? emit it via sendmessage?
    public void Suicide() => gameObject.GetComponent<Health>().Die((null, null, true));

    protected virtual void OnDie() {}

    private void Respawn()
    {
        if (!IsServer) return;
        OnRespawn();
        SendMessage("OnEntityRespawn");
    }

    protected virtual void OnRespawn() {}

    public void TakeDamage(
        float damage,
        NetworkBehaviourReference attackerRef = default,
        NetworkBehaviourReference weaponRef = default
    ) {
        gameObject.GetComponent<Health>().OnDamageTaken(damage, attackerRef, weaponRef);
    }
}
