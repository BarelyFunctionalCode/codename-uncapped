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
    [SerializeField] private ParticleSystem animationFootstepParticleSystem;

    [SerializeField] private Transform legsDirectionTransform;

    private Quaternion desiredLegsDirection;
    private Quaternion currentLegsDirection;
    [SerializeField] private float legsRotateSpeed = 10f;

    [SerializeField] public float mass = 1f;

    public sealed override void OnNetworkObjectParentChanged(NetworkObject networkObject = null)
    {
        base.OnNetworkObjectParentChanged(networkObject);

        if (networkObject != null && networkObject.TryGetComponent(out PlayerController playerController))
        {
            playerController.OnPlayerTypeObjectSpawned(this);
        }
    }

    public void HandleLegRotation(Vector3 movementDirection)
    {
        // Get current movement direction from player controller
        if (movementDirection != Vector3.zero) 
            desiredLegsDirection = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(movementDirection, Vector3.up).normalized,
                Vector3.up
            ).normalized;

        // Determine how closely aligned the current legs direction is with the desired direction
        float angleAlignment = Mathf.Abs(Quaternion.Dot(currentLegsDirection, desiredLegsDirection));

        // Snap-to or smoothly rotate legs direction based on alignment. The closer they are aligned, the faster they rotate.
        if (angleAlignment > 0.999f) currentLegsDirection = desiredLegsDirection;
        else currentLegsDirection = Quaternion.Slerp(
            currentLegsDirection,
            desiredLegsDirection,
            Time.deltaTime * legsRotateSpeed * (angleAlignment + 0.1f)
        );

        // Set transform that constrains the pelvis of the model armature.
        legsDirectionTransform.rotation = Quaternion.LookRotation(currentLegsDirection * Vector3.forward, Vector3.up);
    }

    public void AnimationFootstepEvent(int footIndex)
    {
        var emitParams = new ParticleSystem.EmitParams();
        Vector3 footPosition = animationFootstepParticleSystem.transform.position;
        footPosition += transform.right * (footIndex == 0 ? -0.6f : 0.6f);
        emitParams.rotation = legsDirectionTransform.eulerAngles.y;
        emitParams.position = footPosition;
        emitParams.applyShapeToPosition = true;
        animationFootstepParticleSystem.Emit(emitParams, 1);
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
