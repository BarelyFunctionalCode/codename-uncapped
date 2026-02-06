using UnityEngine;
using Unity.Netcode.Components;
using Unity.Netcode;

[RequireComponent(typeof(NetworkAnimator))]
public class GrenadeLauncher : Weapon
{
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private GameObject grenadeShellPrefab;
    [SerializeField] private float shellEjectForce = 200;
    [SerializeField] private float shellSpinForce = 1000;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem[] ejectParticles;

    protected override void PostFiredRpc()
    {
        foreach (ParticleSystem system in ejectParticles)
        {
            system.Play();
        }

        if (IsServer && animator != null) animator.SetTrigger("Eject");
    }

    private void EjectShell()
    {
        if (!IsServer) return;
        if (grenadeShellPrefab != null)
        {
            GameObject newShell = Instantiate(grenadeShellPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
            NetworkObject newNetworkObject = newShell.GetComponent<NetworkObject>();
            newNetworkObject.Spawn();
            newNetworkObject.TryRemoveParent();
            newShell.GetComponentInChildren<Rigidbody>().AddForce((-newShell.transform.forward + Vector3.up) * shellEjectForce, ForceMode.VelocityChange);
            newShell.GetComponentInChildren<Rigidbody>().AddTorque(newShell.transform.right * shellSpinForce, ForceMode.Force);
            Destroy(newShell, 2.5f);
        }
    }
}
