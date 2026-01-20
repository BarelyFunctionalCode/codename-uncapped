using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeLauncher : Weapon
{
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private GameObject grenadeShellPrefab;
    [SerializeField] private float shellEjectForce = 200;
    [SerializeField] private float shellSpinForce = 1000;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem[] ejectParticles;

    protected override void PostFired()
    {
        foreach (ParticleSystem system in ejectParticles)
        {
            system.Play();
        }

        if (animator != null) animator.SetTrigger("Eject");
    }

    private void EjectShell()
    {
        if (grenadeShellPrefab != null)
        {
            GameObject newShell = Instantiate(grenadeShellPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
            newShell.transform.SetParent(null);
            newShell.GetComponentInChildren<Rigidbody>().AddForce((-newShell.transform.forward + Vector3.up) * shellEjectForce, ForceMode.VelocityChange);
            newShell.GetComponentInChildren<Rigidbody>().AddTorque(newShell.transform.right * shellSpinForce, ForceMode.Force);
            Destroy(newShell, 2.5f);
        }
    }
}
