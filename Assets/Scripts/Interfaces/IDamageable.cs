using Unity.Netcode;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, bool directHit = false, NetworkBehaviourReference attackerRef = default, NetworkBehaviourReference weaponRef = default);
}
