using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncher : Weapon
{
    [Header("Visuals")]
    [SerializeField] Animator animator;

    protected override void PostFiredRpc()
    {
        if (IsServer && animator != null) animator.SetTrigger("Shoot");
    }
}
