using Unity.Netcode.Components;
using UnityEngine;

public class PlayerPuppet : MonoBehaviour
{
    [SerializeField] public Transform freeLookTargetTransform;
    [SerializeField] public Transform weaponMountPoint;
    [SerializeField] public Transform throwableMountPoint;
    [SerializeField] public AudioSource hoverAudioSource;
    [SerializeField] public AudioSource windAudioSource;
    private PlayerController playerController;
    private Rigidbody authoritativeRb;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (playerController != null)
        {
            // Sync the puppet's position with the player's transform
            rb.MovePosition(Vector3.Lerp(rb.position, authoritativeRb.position, 0.5f));
        }
    }

    public void Initialize(PlayerController playerController)
    {
        this.playerController = playerController;
        authoritativeRb = playerController.GetComponent<Rigidbody>();
    }
}
