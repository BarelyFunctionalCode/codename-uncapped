using Unity.Netcode;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, NetworkBehaviourReference attackerRef = default, NetworkBehaviourReference weaponRef = default);
}
