using Unity.Netcode.Components;
using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;

public class PlayerPuppet : MonoBehaviour
{
    [SerializeField] public Transform freeLookTargetTransform;
    [SerializeField] public Transform weaponMountPoint;
    [SerializeField] public Transform throwableMountPoint;
    [SerializeField] public AudioSource hoverAudioSource;
    [SerializeField] public AudioSource windAudioSource;
    private PlayerController playerController;
    // private Transform authoritativeTransform;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;

    Vector3 lastReceivedPosition;

    private float smoothThreshold = 0.1f;
    private float snapThreshold = 10f;

    bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
    }

    private void FixedUpdate()
    {
        if (playerController != null)
        {
            // Sync the puppet's position with the player's transform
            float syncDistance = Vector3.Distance(rb.position, lastReceivedPosition);
            if (syncDistance > smoothThreshold)
            {
                rb.MovePosition(lastReceivedPosition);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw Debug Gizmo showing a capsule of the authoritative player's collider for testing and debugging purposes
        if (isInitialized && lastReceivedPosition != Vector3.zero)
        {
            float syncDistance = Vector3.Distance(rb.position, lastReceivedPosition);
            Gizmos.color = syncDistance > snapThreshold ? Color.red : syncDistance > snapThreshold/2f ? Color.yellow : Color.green;
            Gizmos.DrawWireCube(lastReceivedPosition, playerCollider.bounds.size);
        }    
    }

    public void Initialize(PlayerController playerController)
    {
        this.playerController = playerController;
        playerController.GetComponent<PlayerNetworkTransform>().onNewLocalTransformState.AddListener(OnNewLocalTransformState);
        // authoritativeTransform = playerController.transform;
        isInitialized = true;
    }

    private void OnNewLocalTransformState(NetworkTransformState newState)
    {
        if (!newState.HasPositionChange) return;

        Vector3 newPosition = newState.GetPosition();
        float syncDistance = Vector3.Distance(transform.position, newPosition);
        if (syncDistance > snapThreshold || newState.IsTeleportingNextFrame)
        {
            rb.isKinematic = true;
            playerCollider.enabled = false;

            rb.position = newPosition;
            rb.PublishTransform();

            playerCollider.enabled = true;
            rb.isKinematic = false;
        }
        lastReceivedPosition = newPosition;
    }
}
