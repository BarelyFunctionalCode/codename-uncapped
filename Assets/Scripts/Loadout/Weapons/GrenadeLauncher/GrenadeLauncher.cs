using UnityEngine;
using Unity.Netcode.Components;
using Unity.Netcode;

[RequireComponent(typeof(NetworkAnimator))]
public class GrenadeLauncher : Weapon
{
    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private GameObject grenadeShellPrefab;

    [Header("Attributes")]
    [SerializeField] private float shellEjectForce = 200;
    [SerializeField] private float shellSpinForce = 1000;

    protected override void PostFiredRpc()
    {
        if (IsServer && animator != null) animator.SetTrigger("Shoot");
    }

    // This is called by an animation event
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
