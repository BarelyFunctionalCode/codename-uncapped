using Unity.Netcode.Components;
using UnityEngine;
using static Unity.Netcode.Components.NetworkTransform;

public class PlayerPuppet : MonoBehaviour
{
    private GameObject playerTypeObj;
    private PlayerType playerTypeData;

    public Transform freeLookTargetTransform;
    public Transform weaponMountPoint;
    public Transform throwableMountPoint;
    public Animator playerAnimator;
    public AudioSource hoverAudioSource;
    public AudioSource windAudioSource;
    public Rigidbody rb;
    public CapsuleCollider playerCollider;
    private PlayerController playerController;

    Vector3 lastReceivedPosition;

    private float smoothThreshold = 0.1f;
    private float smoothAmount = 100f;
    private float snapThreshold = 10f;

    bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0.0f;
    }

    private void FixedUpdate()
    {
        if (playerController != null)
        {
            // Smoothly interpolate towards the last received position from the authoritative player, if it's not too far away
            float syncDistance = Vector3.Distance(rb.position, lastReceivedPosition);
            if (syncDistance > smoothThreshold)
            {
                rb.position = Vector3.Lerp(rb.position, lastReceivedPosition, Time.fixedDeltaTime * smoothAmount * syncDistance / snapThreshold);
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
        SetPlayerType(playerController.playerTypePrefabObj);
        playerController.GetComponent<PlayerNetworkTransform>().onNewLocalTransformState.AddListener(OnNewLocalTransformState);
        isInitialized = true;
    }

    private void SetPlayerType(GameObject playerTypePrefabObj)
    {
        if (playerTypeObj != null) Destroy(playerTypeObj);
        playerTypeObj = Instantiate(playerTypePrefabObj, transform.position, transform.rotation, transform);
        playerTypeData = playerTypeObj.GetComponent<PlayerType>();
        playerCollider = playerTypeData.playerCollider;
        playerAnimator = playerTypeData.playerAnimator;
        freeLookTargetTransform = playerTypeData.freeLookTargetTransform;
        weaponMountPoint = playerTypeData.weaponMountPoint;
        throwableMountPoint = playerTypeData.throwableMountPoint;
        hoverAudioSource = playerTypeData.hoverAudioSource;
        windAudioSource = playerTypeData.windAudioSource;
        rb.mass = playerTypeData.mass;
    }

    private void OnNewLocalTransformState(NetworkTransformState newState)
    {
        if (!newState.HasPositionChange) return;

        Vector3 newPosition = newState.GetPosition();
        float syncDistance = Vector3.Distance(transform.position, newPosition);

        // Desync is either too big or you are teleporting, snap to the new position immediately
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
