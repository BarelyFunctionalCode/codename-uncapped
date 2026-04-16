using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerType : NetworkBehaviour
{
    public PlayerCamera firstPersonCamera;
    public Transform pickupContainerHoldPoint;
    public CapsuleCollider playerCollider;
    public Transform freeLookTargetTransform;
    public Animator playerAnimator;
    public Transform weaponMountPoint;
    public Transform throwableMountPoint;
    public AudioSource hoverAudioSource;
    public AudioSource windAudioSource;

    [PauseMenuOption("Vertical Look", 0f, 100f)]
    public float verticalRotationSpeed = 24f;
    public float verticalRotationLimit = 100f;
    
    public float mass = 1f;

    [SerializeField] private GameObject modelObj;
    [SerializeField] private ParticleSystem animationFootstepParticleSystem;
    [SerializeField] private GameObject deathEffectPrefab;
    private GameObject deathObj = null;

    [SerializeField] private Transform legsDirectionTransform;
    [SerializeField] private Transform leftFootIKTargetTransform;
    [SerializeField] private Transform rightFootIKTargetTransform;
    [SerializeField] private Rig hoverIKRig;
    private float hoverParticleMaxEmissionRate = 15f;
    [SerializeField] private ParticleSystem hoverEffectLeftFootParticleSystem;
    [SerializeField] private ParticleSystem hoverEffectRightFootParticleSystem;

    private Vector3 animMovementDirection = Vector3.zero;
    private Quaternion desiredLegsDirection;
    private Quaternion currentLegsDirection;
    [SerializeField] private float legsRotateSpeed = 10f;


    public sealed override void OnNetworkObjectParentChanged(NetworkObject networkObject = null)
    {
        base.OnNetworkObjectParentChanged(networkObject);

        if (networkObject != null && networkObject.TryGetComponent(out PlayerController playerController))
        {
            playerController.OnPlayerTypeObjectSpawned(this);
        }
    }

    private void Start()
    {
        if (!NetworkObject.IsSpawned || IsOwner) firstPersonCamera.gameObject.SetActive(true);
    }

    public void HandleCamera(float rotationInputY, int controlsDisabledCount)
    {
        // Get pitch rotation from inputs and rotate the camera look target
        Vector3 rotationPitch = new(rotationInputY, 0f, 0f);
        rotationPitch *= verticalRotationSpeed * Time.deltaTime;
        Vector3 rotationDeltaPitch = Vector3.ClampMagnitude(rotationPitch, verticalRotationLimit);
        float currentXRotation = freeLookTargetTransform.eulerAngles.x < 180f ? freeLookTargetTransform.eulerAngles.x : freeLookTargetTransform.eulerAngles.x - 360f;
        rotationDeltaPitch.x = Mathf.Clamp(currentXRotation + rotationDeltaPitch.x, -83.0f, 83.0f) - currentXRotation;
        if (controlsDisabledCount > 0) rotationDeltaPitch = Vector3.zero;
        
        freeLookTargetTransform.Rotate(rotationDeltaPitch);
    }

    public void HandleAudio(Vector3 velocity, bool isSkiing)
    {
        // Set audio values
        if (hoverAudioSource)
        {
            float maxVolume = 0.3f;
            hoverAudioSource.volume = Mathf.Lerp(hoverAudioSource.volume, isSkiing ? maxVolume : 0f, Time.fixedDeltaTime * 5f);
            hoverAudioSource.pitch = 0.9f + 0.05f * (velocity.magnitude / 20f);
        }
        if (windAudioSource)
        {
            float cappedSpeed = (velocity.magnitude - 20f) / 80f;
            float targetVolume = Mathf.Lerp(0f, 0.02f, cappedSpeed);
            float targetPitch = Mathf.Lerp(0.9f, 1.5f, cappedSpeed);
            windAudioSource.volume = Mathf.Lerp(windAudioSource.volume, targetVolume, Time.fixedDeltaTime * 20f);
            windAudioSource.pitch = Mathf.Lerp(windAudioSource.pitch, targetPitch, Time.fixedDeltaTime * 20f);
        }
    }
    // TODO: Add RPC for hover audio so that other players can hear it

    public void HandleExtraMotion(Vector3 movementDirection, bool isHovering, Vector3 surfaceNormal)
    {
        HandleLegRotation(movementDirection);
        HandleHover(isHovering, surfaceNormal);
    }

    private void HandleLegRotation(Vector3 movementDirection)
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

    private void HandleHover(bool isHovering, Vector3 surfaceNormal)
    {
        float ikRigTargetWeight = isHovering ? 1f : 0f;
        hoverIKRig.weight = Mathf.Lerp(hoverIKRig.weight, ikRigTargetWeight, Time.deltaTime * 5f);

        float emmissionRate = isHovering ? hoverParticleMaxEmissionRate : 0f;
        var leftEmission = hoverEffectLeftFootParticleSystem.emission;
        var rightEmission = hoverEffectRightFootParticleSystem.emission;
        leftEmission.rateOverTime = Mathf.Lerp(leftEmission.rateOverTime.constant, emmissionRate, Time.deltaTime * 5f);
        rightEmission.rateOverTime = Mathf.Lerp(rightEmission.rateOverTime.constant, emmissionRate, Time.deltaTime * 5f);

        if (NetworkObject.IsSpawned) HandleHoverRpc(isHovering, hoverIKRig.weight);

        if (!isHovering) return;
        Vector3 targetDirection = Vector3.Cross(-surfaceNormal, legsDirectionTransform.right).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, -surfaceNormal);
        leftFootIKTargetTransform.rotation = Quaternion.Slerp(leftFootIKTargetTransform.rotation, targetRotation, Time.deltaTime * 5f);
        rightFootIKTargetTransform.rotation = Quaternion.Slerp(rightFootIKTargetTransform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    [Rpc(SendTo.Everyone)]
    private void HandleHoverRpc(bool isHovering, float ikRigWeight)
    {
        if (IsServer) return;
        
        hoverIKRig.weight = ikRigWeight;

        float emmissionRate = isHovering ? hoverParticleMaxEmissionRate : 0f;
        var leftEmission = hoverEffectLeftFootParticleSystem.emission;
        var rightEmission = hoverEffectRightFootParticleSystem.emission;
        leftEmission.rateOverTime = Mathf.Lerp(leftEmission.rateOverTime.constant, emmissionRate, Time.deltaTime * 5f);
        rightEmission.rateOverTime = Mathf.Lerp(rightEmission.rateOverTime.constant, emmissionRate, Time.deltaTime * 5f);
    }

    public void UpdateAnimationData(Vector3 movement, Vector3 velocity, bool isGrounded, bool isSkiing, bool isDownJetting, bool isUpJetting, bool isJumping)
    {
        Vector3 animMovementDirectionNewY = Vector3.up * (isDownJetting ? -1f : (isUpJetting ? 1f : 0f));
        animMovementDirection = Vector3.Lerp(animMovementDirection, movement.normalized + animMovementDirectionNewY, Time.fixedDeltaTime * 10f);
        playerAnimator.SetFloat("xDir", animMovementDirection.x);
        playerAnimator.SetFloat("yDir", animMovementDirection.y);
        playerAnimator.SetFloat("zDir", animMovementDirection.z);
        playerAnimator.SetFloat("yVel", velocity.normalized.y);
        playerAnimator.SetBool("isGrounded", isGrounded);
        playerAnimator.SetBool("isRunning", isGrounded && movement.magnitude > 0.1f && !isSkiing);
        playerAnimator.SetBool("isSkiing", isSkiing && !isUpJetting && !isDownJetting);
        playerAnimator.SetBool("isJetting", isUpJetting || isDownJetting);

        if (isJumping) playerAnimator.SetTrigger("triggerJump");
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

    public void OnDie(Vector3 inheritedVelocity)
    {
        modelObj.SetActive(false);
        if (deathEffectPrefab != null)
        {
            deathObj = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            deathObj.GetComponent<PlayerDeath>().Initialize(!NetworkObject.IsSpawned || IsOwner, inheritedVelocity);
        }
    }

    public void OnRespawn()
    {
        modelObj.SetActive(true);
        if (deathObj != null)
        {
            Destroy(deathObj);
            deathObj = null;
        }
    }
}
