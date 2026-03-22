using Unity.Netcode;
using UnityEngine;

public class PlayerType : NetworkBehaviour
{
    [SerializeField] private GameObject modelObj;
    [SerializeField] public CapsuleCollider playerCollider;
    [SerializeField] public Transform freeLookTargetTransform;
    [SerializeField] public Animator playerAnimator;
    [SerializeField] public Transform weaponMountPoint;
    [SerializeField] public Transform throwableMountPoint;
    [SerializeField] public AudioSource hoverAudioSource;
    [SerializeField] public AudioSource windAudioSource;

    [SerializeField] public float mass = 1f;

    public sealed override void OnNetworkObjectParentChanged(NetworkObject networkObject = null)
    {
        base.OnNetworkObjectParentChanged(networkObject);

        if (networkObject != null && networkObject.TryGetComponent(out PlayerController playerController))
        {
            playerController.OnPlayerTypeObjectSpawned(this);
        }
    }

    public void OnDie()
    {
        modelObj.SetActive(false);
    }

    public void OnRespawn()
    {
        modelObj.SetActive(true);
    }
}
