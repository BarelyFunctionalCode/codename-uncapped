using System.Collections.Generic;
using System.Reflection;
using Steamworks;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct InputState
{
    public double timestamp;
    public Vector3 movementDirection;
    public Vector3 rotationDeltaYaw;
    public bool isJumping;
    public bool isSkiing;
    public bool isUpJetting;
    public bool isDownJetting;
    public bool isJetting;
    public bool isRunning;
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(PlayerLoadout))]
[RequireComponent(typeof(AnticipatedNetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody))]
public class PlayerController : Entity
{
    [Space(20)]
    // Debug
    [SerializeField] private DevVectorRenderer devVectorRenderer;
    public PlayerTelemetry playerTelemetry;

    // ID
    private SteamId localId;
    public SteamId PlayerSteamId { get { return localId; } }

    // Animation
    private Animator animator;
    private Vector3 animMovementDirection = Vector3.zero;

    // Camera
    [SerializeField] private GameObject playerCameraPrefabObj;
    private GameObject playerCameraObj;
    private CinemachineCamera cineCam;
    [SerializeField] private Transform freeLookTargetTransform;
    [PauseMenuOption("Horizontal Look", 0f, 100f)]
    public float horizontalRotationSpeed = 20f;
    private readonly float horizontalRotationLimit = 100f;
    [PauseMenuOption("Vertical Look", 0f, 100f)]
    public float verticalRotationSpeed = 24f;
    public float verticalRotationLimit = 100f;
    private AudioListener audioListener;

    // UI
    [SerializeField] private GameObject playerUIPrefabObj;
    public GameObject playerUIObj;

    // Inputs
    public PlayerControls playerControls;
    Vector3 movementInput = Vector3.zero;
    Vector3 movementDirection = Vector3.zero;
    float rotationInputX = 0f;
    float rotationInputY = 0f;
    Vector3 rotationDeltaYaw = Vector3.zero;
    Vector3 rotationDeltaPitch = Vector3.zero;
    bool isJumping = false;
    bool isSkiing = false;
    bool upJettingInput = false;
    bool isUpJetting = false;
    bool downJettingInput = false;
    bool isDownJetting = false;
    bool isJetting = false;
    bool isMoving = false;
    bool isRunning = false;

    // Physics
    [Header("Physics")]
    [SerializeField] private PhysicsMaterial skiMaterial;
    [SerializeField] private PhysicsMaterial normalMaterial;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private AnticipatedNetworkTransform anticipatedNetworkTransform;
    private List<InputState> inputBuffer = new();
    Vector3 surfaceNormal = Vector3.up;
    Vector3 surfacePoint = Vector3.zero;
    float distanceToSurface = Mathf.Infinity;
    float lastGroundedTime = 0;

    // Weapons and Gear
    [Header("Weapons and Gear")]
    [SerializeField] public Transform weaponMountPoint;
    [SerializeField] public Transform throwableMountPoint;
    private PlayerLoadout playerLoadout;

    // Movement Parameters
    private readonly float hoverHeightMax = 0.4f;

    private readonly float upJetForce = 7031.25f; // TODO: This force value need to be moved to the elsewhere since they differ by class
    private readonly float downJetForce = 5156.25f; // TODO: This force value need to be moved to the elsewhere since they differ by class
    private readonly float jetAirMoveMinSpeed = 5f;
    private readonly float jetAirMoveMaxSpeed = 1000f;
    private readonly float jetAirMoveMaxAccelFactor = 1.5f;
    private readonly float jetDirectionalForceXY = 3125f; 
    private readonly float upJettingEnergyDrain = 22.5f;
    private readonly float downJettingEnergyDrain = 21.875f;
    private readonly float jetSkateEnergyDrain = 4f;

    private readonly float runForce = 10000f;
    private readonly float maxRunSpeed = 20f;
    private readonly float jumpForce = 2000f;
    private readonly float minJumpSpeed = 10f;
    private readonly float maxJumpSpeed = 15f;
    private readonly float jumpSurfaceAngle = 80f;
    private readonly float airControl = 1f;

    private readonly float horizontalJetResistance = 0.0017f;
    private readonly float horizontalJetResistanceFactor = 1.8f;
    private readonly float verticalJetResistance = 0.0006f;
    private readonly float verticalJetResistanceFactor = 1.8f;
    private readonly float horizResistFactor = 0.3f;
    private readonly float horizResistSpeed = 100f;
    private readonly float horizMaxSpeed = 120f;
    private readonly float upResistFactor = 0.3f;
    private readonly float upResistSpeed = 85f;
    private readonly float upMaxSpeed = 115f;
    private readonly float downResistFactor = 0.1f;
    private readonly float downResistSpeed = 1000f;
    private readonly float downMaxSpeed = 1000f;
    private readonly float drag = 0.004f;                        
    private readonly float airCushionDrag = 0.00275f;             
    private readonly float airCushionHeight = 10f;    
    private readonly float mass = 75f; // TODO: This mass value need to be moved to the elsewhere since they differ by class

    public bool isInitialized = false;


    #region Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();
        playerLoadout = GetComponent<PlayerLoadout>();

        anticipatedNetworkTransform = GetComponent<AnticipatedNetworkTransform>();
        anticipatedNetworkTransform.StaleDataHandling = StaleDataHandling.Reanticipate;
    }

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb.sleepThreshold = 0.0f;
        rb.mass = mass;
        playerCollider.material = normalMaterial;

        if (!IsServer && !IsOwner) 
        {
            GetComponent<NetworkRigidbody>().UseRigidBodyForMotion = false;
            rb.isKinematic = true;
        }

        SceneManager.activeSceneChanged += ChangedActiveScene;

        if (IsOwner)
        {
            // Capture the mouse cursor
            Cursor.lockState = CursorLockMode.Locked;

            // Set up the player controls
            playerControls = new PlayerControls();

            // Set up the input callbacks
            playerControls.Enable();
            playerControls.Movement.Move.performed += ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Move.canceled += ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Look.performed += ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Look.canceled += ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Ski.performed += ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Movement.Ski.canceled += ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Movement.JumpJet.performed += ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Movement.JumpJet.canceled += ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Movement.DownJet.performed += ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Movement.DownJet.canceled += ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Movement.JumpJet.started += ctx => JumpInput();

            playerControls.Equipment.NextWeapon.started += ctx => playerLoadout.NextWeaponRpc();
            playerControls.Equipment.PreviousWeapon.started += ctx => playerLoadout.PreviousWeaponRpc();
            playerControls.Equipment.PrimaryFire.started += ctx => playerLoadout.OnPrimaryFireStartedRpc();
            playerControls.Equipment.PrimaryFire.canceled += ctx => playerLoadout.OnPrimaryFireCanceledRpc();

            if (!IsHost) Initialize();
        }
    }

    public sealed override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SceneManager.activeSceneChanged -= ChangedActiveScene;

        if (IsOwner)
        {
            // If the player is despawned, disable the inputs
            playerControls.Disable();
            playerControls.Movement.Move.performed -= ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Move.canceled -= ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Look.performed -= ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Look.canceled -= ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Movement.Ski.performed -= ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Movement.Ski.canceled -= ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Movement.JumpJet.performed -= ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Movement.JumpJet.canceled -= ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Movement.DownJet.performed -= ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Movement.DownJet.canceled -= ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Movement.JumpJet.started -= ctx => JumpInput();

            playerControls.Equipment.NextWeapon.started -= ctx => playerLoadout.NextWeaponRpc();
            playerControls.Equipment.PreviousWeapon.started -= ctx => playerLoadout.PreviousWeaponRpc();
            playerControls.Equipment.PrimaryFire.started -= ctx => playerLoadout.OnPrimaryFireStartedRpc();
            playerControls.Equipment.PrimaryFire.canceled -= ctx => playerLoadout.OnPrimaryFireCanceledRpc();

            // Disable audio listener
            if (audioListener) audioListener.enabled = false;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!isInitialized || !(IsServer || IsOwner)) return;

        HandleGroundDetection();

        // Apply Drag and Friction
        rb.linearDamping = distanceToSurface <= airCushionHeight ? airCushionDrag : drag;
        playerCollider.material = isSkiing ? skiMaterial : normalMaterial;

        if (IsOwner)
        {
            if (playerControls.UI.Pause.WasPressedThisFrame())
            {
                bool newMenuState = !playerUIObj.transform.Find("PauseMenu").gameObject.activeSelf;
                Cursor.lockState = newMenuState ? CursorLockMode.Confined : CursorLockMode.Locked;
                if (newMenuState) {
                    playerControls.Disable();
                    playerControls.UI.Enable();
                }
                else playerControls.Enable();
                playerUIObj.transform.Find("PauseMenu").gameObject.SetActive(newMenuState);
            }

            playerTelemetry.Update();
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || !IsOwner) return;

        // Handle camera pitch rotation on local client
        HandleCamera();
    }

    void FixedUpdate()
    {
        if (!isInitialized || !(IsServer || IsOwner)) return;

        // First, we collect all of the inputs that go into moving the player, and create an input state
        HandleInputs();
        InputState frameInputs = new()
        {
            timestamp = NetworkManager.LocalTime.Time,
            movementDirection = movementDirection,
            rotationDeltaYaw = rotationDeltaYaw,
            isJumping = isJumping,
            isSkiing = isSkiing,
            isUpJetting = isUpJetting,
            isDownJetting = isDownJetting,
            isJetting = isJetting,
            isRunning = isRunning
        };

        // Next, we add the inputs to the input buffer for client-side prediction
        if (!IsServer)
        {
            inputBuffer.Add(frameInputs);
        }

        // Finally, we process the inputs to move the player locally and on the server
        HandleMovement(frameInputs);

        isJumping = false;
        if (playerTelemetry != null)
        {
            playerTelemetry.position = transform.position;
            playerTelemetry.velocity = rb.linearVelocity;
        }
    }
    #endregion


    #region Initialization
    public void Initialize()
    {
        if (IsOwner)
        {
            // Initialize Player UI
            if (!playerUIObj) {
                playerUIObj = Instantiate(playerUIPrefabObj);
                playerUIObj.GetComponentInChildren<HUD>().Initialize(this);
                playerTelemetry = new PlayerTelemetry(devVectorRenderer);
                InitializePauseMenuElements();
            }

            // Initialize Player Camera
            if (!playerCameraObj)
            {
                playerCameraObj = Instantiate(playerCameraPrefabObj);
                audioListener = playerCameraObj.GetComponentInChildren<AudioListener>();
                cineCam = playerCameraObj.GetComponentInChildren<CinemachineCamera>();
                cineCam.Follow = freeLookTargetTransform;

                // Enable audio listener
                audioListener.enabled = true;

                // Enable the camera
                cineCam.Priority.Value = 1;
            }

            // Get Player's Steam ID
            if (GameManager.Instance?.usingSteam == true)
            {
                localId = SteamClient.SteamId.Value;
            }
            InitializeRpc();
        }
        isInitialized = true;
    }

    [Rpc(SendTo.Server)]
    private void InitializeRpc()
    {
        playerLoadout.Initialize(this);

        isInitialized = true;
    }
    #endregion


    #region Cleanup
    [Rpc(SendTo.Server)]
    public void DisconnectCleanupRpc()
    {
        playerLoadout.Deinitialize();
        DisconnectCleanupOwnerRpc();

        isInitialized = false;
    }

    [Rpc(SendTo.Owner)]
    private void DisconnectCleanupOwnerRpc()
    {
        // Disable the UI
        if (playerUIObj)
        {
            Destroy(playerUIObj);
            playerUIObj = null;
        }

        isInitialized = false;
    }
    #endregion


    #region Inputs
    private void MoveInput(Vector2 movementInput)
    {
        this.movementInput = new Vector3(movementInput.x, 0f, movementInput.y);
        MoveInputRpc(movementInput);
    }
    [Rpc(SendTo.Server)]
    private void MoveInputRpc(Vector2 movementInput)
    {
        this.movementInput = new Vector3(movementInput.x, 0f, movementInput.y);
    }
    private void LookInput(Vector2 lookInput)
    {
        rotationInputX += lookInput.x;
        rotationInputY -= lookInput.y;
        LookInputRpc(lookInput);
    }
    [Rpc(SendTo.Server)]
    private void LookInputRpc(Vector2 lookInput)
    {
        rotationInputX += lookInput.x;
        rotationInputY -= lookInput.y;
    }
    private void SkiInput(float skiInput)
    {
        isSkiing = skiInput > 0.0f;
        SkiInputRpc(skiInput);
    }
    [Rpc(SendTo.Server)]
    private void SkiInputRpc(float skiInput)
    {
        isSkiing = skiInput > 0.0f;
    }
    private void JetInput(float jetInput)
    {
        upJettingInput = jetInput > 0.0f;
        JetInputRpc(jetInput);
    }
    [Rpc(SendTo.Server)]
    private void JetInputRpc(float jetInput)
    {
        upJettingInput = jetInput > 0.0f;
    }
    private void DownJetInput(float downJetInput)
    {
        downJettingInput = downJetInput > 0.0f;
        DownJetInputRpc(downJetInput);
    }
    [Rpc(SendTo.Server)]
    private void DownJetInputRpc(float downJetInput)
    {
        downJettingInput = downJetInput > 0.0f;
    }
    private void JumpInput()
    {
        isJumping = true;
        JumpInputRpc();
    }
    [Rpc(SendTo.Server)]
    private void JumpInputRpc()
    {
        isJumping = true;
    }

    private void HandleInputs()
    {
        // Get rotation input
        Vector3 rotationYaw = new(0f, rotationInputX, 0f);;
        rotationInputX = 0f;
        rotationYaw *= horizontalRotationSpeed * Time.fixedDeltaTime;
        rotationDeltaYaw = Vector3.ClampMagnitude(rotationYaw, horizontalRotationLimit);

        // Get direction of movement relative to player rotation
        Vector3 movement = movementInput;
        movementDirection = transform.TransformDirection(movement); // NOT SUPPOSED TO BE NORMALIZED

        // Get input for skiing, jumping, and down jetting
        isUpJetting = upJettingInput && isSkiing;
        isDownJetting = downJettingInput && isSkiing;
        isJetting = isUpJetting || isDownJetting;
        isMoving = movement.magnitude > 0.0f;
        isRunning = isGrounded && isMoving && !isSkiing;
        
        if (playerTelemetry != null)
        {
            playerTelemetry.movementDirection = movementDirection;
            playerTelemetry.isSkiing = isSkiing;
            playerTelemetry.isUpJetting = isUpJetting;
            playerTelemetry.isDownJetting = isDownJetting;
        }

        // Set animator values
        Vector3 animMovementDirectionNewY = Vector3.up * (isDownJetting ? -1f : (isUpJetting ? 1f : 0f));
        animMovementDirection = Vector3.Lerp(animMovementDirection, movement.normalized + animMovementDirectionNewY, Time.fixedDeltaTime * 10f);
        animator.SetFloat("xDir", animMovementDirection.x);
        animator.SetFloat("yDir", animMovementDirection.y);
        animator.SetFloat("zDir", animMovementDirection.z);
        animator.SetFloat("yVel", rb.linearVelocity.normalized.y);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isSkiing", isSkiing && !isUpJetting && !isDownJetting);
        animator.SetBool("isJetting", isUpJetting || isDownJetting);
    }
    #endregion


    #region Movement
    private void HandleCamera()
    {
        // Get pitch rotation from inputs and rotate the camera look target
        Vector3 rotationPitch = new(rotationInputY, 0f, 0f);
        rotationPitch *= verticalRotationSpeed * Time.deltaTime;
        rotationDeltaPitch = Vector3.ClampMagnitude(rotationPitch, verticalRotationLimit);
        rotationInputY = 0f;
        float currentXRotation = freeLookTargetTransform.eulerAngles.x < 180f ? freeLookTargetTransform.eulerAngles.x : freeLookTargetTransform.eulerAngles.x - 360f;
        rotationDeltaPitch.x = Mathf.Clamp(currentXRotation + rotationDeltaPitch.x, -89.0f, 89.0f) - currentXRotation;
        freeLookTargetTransform.Rotate(rotationDeltaPitch);
    }

    private void HandleGroundDetection()
    {
        lastGroundedTime += Time.deltaTime;
        isGrounded = false;
        distanceToSurface = Mathf.Infinity;
        surfaceNormal = Vector3.up;
        surfacePoint = Vector3.zero;

        // Raycast down...
        Vector3 groundCheckPoint = playerCollider.bounds.center;
        RaycastHit hit;
        bool didHit = Physics.Raycast(
            new Ray(
                groundCheckPoint,
                Vector3.down
            ),
            out hit,
            distanceToSurface
        );
        if (didHit)
        {
            if (playerTelemetry != null) playerTelemetry.isGrounded = isGrounded;
            
            // Surface too steep
            float slope = Vector3.Dot(hit.normal, Vector3.up);
            if (slope <= 0.1f) return;

            surfacePoint = hit.point;
            distanceToSurface = Mathf.Max(Vector3.Distance(surfacePoint, groundCheckPoint) - playerCollider.bounds.extents.y, 0.0f);

            if (playerTelemetry != null) playerTelemetry.distanceToSurface = distanceToSurface;
            if (playerTelemetry != null) playerTelemetry.surfacePoint = surfacePoint;

            // Breakaway vertical speed check
            if (rb.linearVelocity.y > 20.0f) return;

            if (distanceToSurface <= 0.25f)
            {
                isGrounded = true;
                lastGroundedTime = 0f;
            }
            else if (lastGroundedTime < 0.2f) isGrounded = true;
            else isGrounded = false;

            if (isGrounded) surfaceNormal = hit.normal;
        }

        if (playerTelemetry != null) playerTelemetry.isGrounded = isGrounded;
        if (playerTelemetry != null) playerTelemetry.surfaceNormal = surfaceNormal;
        
    }

    private void HandleMovement(InputState frameInputs)
    {
        Vector3 movementDirection = frameInputs.movementDirection;
        Vector3 rotationDeltaYaw = frameInputs.rotationDeltaYaw;
        bool isJumping = frameInputs.isJumping;
        bool isSkiing = frameInputs.isSkiing;
        bool isUpJetting = frameInputs.isUpJetting;
        bool isDownJetting = frameInputs.isDownJetting;
        bool isJetting = frameInputs.isJetting;
        bool isRunning = frameInputs.isRunning;
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 desiredAcc = Vector3.zero;
        Vector3 groundImpulse = Vector3.zero;
        float desiredVerticalAcc = 0f;

        // Air Control
        if (!isGrounded && !isJetting && !isSkiing)
        {
            Vector3 airDirection = movementDirection.normalized;
            Vector3 airControlAcc = airDirection * airControl;

            float maxAccel = runForce / mass * Time.fixedDeltaTime * 0.3f;

            if (airControlAcc.magnitude > maxAccel)
            {
                airControlAcc = airControlAcc.normalized * maxAccel;
            }
            desiredAcc.x += airControlAcc.x;
            desiredAcc.z += airControlAcc.z;
        }

        // Jumping
        if (isJumping && isGrounded && currentVelocity.y <= maxJumpSpeed)
        {
            float jumpScale = 1.0f;

            if (currentVelocity.y < minJumpSpeed)
            {
                jumpScale = 1.0f - currentVelocity.y / minJumpSpeed / (maxJumpSpeed / minJumpSpeed);
            }

            Vector3 jumpDirection = movementDirection.normalized;

            float playerScaleFactor = transform.localScale.y * 0.25f + 0.75f;
            float jumpForceFinal = jumpForce / rb.mass;

            float surfaceNormalDotJumpDirection = Vector3.Dot(jumpDirection, surfaceNormal);

            if (surfaceNormalDotJumpDirection > 0.0f)
            {
                desiredAcc.x += surfaceNormal.x * playerScaleFactor * jumpForceFinal;
                desiredAcc.z += surfaceNormal.z * playerScaleFactor * jumpForceFinal;
            }

            Vector3 jumpSurfaceNormal = Vector3.Angle(surfaceNormal, Vector3.up) <= jumpSurfaceAngle ?
                surfaceNormal :
                Vector3.zero;
            desiredVerticalAcc = jumpSurfaceNormal.y * playerScaleFactor * jumpForceFinal * jumpScale;
            lastGroundedTime = 1f;
            animator.SetTrigger("triggerJump");
        }
        // Running Movement
        else if (isRunning)
        {
            groundImpulse = new(0f, -Physics.gravity.magnitude * Time.fixedDeltaTime, 0f);
            float slopeDot = -Vector3.Dot(groundImpulse, surfaceNormal);

            if (slopeDot > 0.0f)
            {
                float modifiedSlopeDot = slopeDot + 0.002f;
                groundImpulse.y += surfaceNormal.y * modifiedSlopeDot;
                groundImpulse.z += surfaceNormal.z * modifiedSlopeDot;
                if (groundImpulse.magnitude < 0.0f) groundImpulse = Vector3.zero;
            }

            Vector3 targetVelocity = Vector3.zero;
            if (movementDirection.magnitude > 0.01f)
            {
                Vector3 runDirection = movementDirection;
                Vector3 forwardDirection = surfaceNormal;
                Vector3 sideDirection = new(-runDirection.z * runDirection.magnitude, 0f, runDirection.x * runDirection.magnitude);
                float sideDot = Vector3.Dot(sideDirection, forwardDirection);
                forwardDirection -= sideDirection * sideDot;
                float moveDot = Vector3.Dot(runDirection, forwardDirection);
                runDirection -= forwardDirection * moveDot;
                targetVelocity = runDirection * (maxRunSpeed / runDirection.magnitude);
            }

            Vector3 velocityDiff = targetVelocity - (currentVelocity + groundImpulse);

            float maxRunAccel = runForce / mass * Time.fixedDeltaTime;
            if (velocityDiff.magnitude > maxRunAccel)
                velocityDiff *= maxRunAccel / velocityDiff.magnitude;

            groundImpulse += velocityDiff;
        }

        // Skiing Movement
        if (isSkiing && GetEnergy() > 0.0f)
        {
            // Hovering
            // More force the closer to the surface...
            float hoverFactor = Mathf.Clamp01(1.0f - (distanceToSurface - hoverHeightMax) / hoverHeightMax) * 1.1f;

            Vector3 lateralVelocityDir = Vector3.ProjectOnPlane(currentVelocity, Vector3.up).normalized;
            float surfaceNormalDotLateralVelocityDirection = Vector3.Dot(surfaceNormal, lateralVelocityDir);

            if (surfaceNormalDotLateralVelocityDirection > 0.0f)
            {
                // Going Downhill?
                // player is pushed fast downhill... easy
                desiredAcc = 2.0f * hoverFactor * Physics.gravity.magnitude * Time.fixedDeltaTime * Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);
            }
            else
            {
                // Going Uphill?
                Vector3 surfaceDirection = (surfaceNormal - lateralVelocityDir * surfaceNormalDotLateralVelocityDirection).normalized;
                Vector3 sideDirection = -lateralVelocityDir;
                float sideDot = Vector3.Dot(surfaceDirection, sideDirection);
                
                desiredAcc = 0.5f * hoverFactor * Physics.gravity.magnitude * Time.fixedDeltaTime * (surfaceDirection - lateralVelocityDir * sideDot);
            }
            desiredAcc.y = 0.0f;
            Vector3 hoverVertAcc = hoverFactor * Physics.gravity.magnitude * Time.fixedDeltaTime * Vector3.up;
            currentVelocity += hoverVertAcc;
        }

        // Jetting Movement
        // TODO: I think there is suppose to be some kind of "Jet Activation Timeout" for when energy depletes to prevent immediate re-jetting
        float speed = currentVelocity.magnitude;
        if (speed < jetAirMoveMaxSpeed)
        {
            float accelScale = 1.0f;
            if (speed < jetAirMoveMinSpeed && speed > 0.01f)
            {
                accelScale = Mathf.Min(
                    jetAirMoveMinSpeed / speed,
                    jetAirMoveMaxAccelFactor);
            }

            // Directional Control while Jetting/Skiing
            if (isSkiing && movementDirection.magnitude > 0.01f && GetEnergy() > 0.0f)
            {
                float lateralForce = jetDirectionalForceXY / mass * accelScale * Time.fixedDeltaTime;
                desiredAcc += movementDirection * lateralForce;
                ApplyEnergyDelta(-jetSkateEnergyDrain * accelScale * Time.fixedDeltaTime);
            }

            // Up Jetting
            if (isJetting && GetEnergy() > 0.01f)
            {
                float force = 0f;
                if (isUpJetting)
                {
                    float cushion = 1.0f;
                    if (distanceToSurface <= airCushionHeight)
                        cushion = (airCushionHeight - distanceToSurface) / airCushionHeight;

                    force = upJetForce / mass * accelScale * Time.fixedDeltaTime;
                    force += force * cushion * 0.5f;
                }
                else if (isDownJetting) // Down Jetting
                {
                    force = -downJetForce / mass * accelScale * Time.fixedDeltaTime;
                }

                desiredVerticalAcc = force;
                ApplyEnergyDelta(- (isUpJetting ? upJettingEnergyDrain : downJettingEnergyDrain) * accelScale * Time.fixedDeltaTime);
            }
        }

        // Apply Jet Resistance
        currentVelocity += CalculateJetResistance(currentVelocity, desiredVerticalAcc, desiredAcc);

        // Apply desired acceleration, jetting accelration, walking acceleration, and gravity
        desiredAcc.y += desiredVerticalAcc;
        desiredAcc.y -= Physics.gravity.magnitude * Time.fixedDeltaTime;
        desiredAcc += groundImpulse;
        currentVelocity += desiredAcc;

        // Apply velocity caps
        currentVelocity += CalculateVelocityCaps(currentVelocity);
        // Debug.Log($"Current Velocity: {rb.linearVelocity:F2}\t Desired Acc: {desiredAcc:F2}\t Jet Resistance: {jetResistance:F2}\t Capped Excess: {velocityCappedExcess:F2}\t Final Velocity: {currentVelocity:F2}");
    
        // Calculate final change in velocity to apply
        Vector3 finalVelocityChange = currentVelocity - rb.linearVelocity;

        // Calculate rotation to apply
        Quaternion newRot = Quaternion.Euler(rb.rotation.eulerAngles + rotationDeltaYaw);

        // Apply velocity and rotation updates to rigidbody
        rb.AddForce(finalVelocityChange, ForceMode.VelocityChange);
        rb.MoveRotation(newRot);

        // Update Anticipated Network Transform for client-side prediction and reconciliation
        anticipatedNetworkTransform.AnticipateState(new AnticipatedNetworkTransform.TransformState
        {
            Position = rb.position,
            Rotation = rb.rotation,
            Scale = transform.localScale
        });
        // Debug.Log($"{(IsServer ? "Server" : "Client")} {OwnerClientId} Authoritative State: {anticipatedNetworkTransform.AuthoritativeState.Position}, {anticipatedNetworkTransform.AuthoritativeState.Rotation.eulerAngles} Should Reanticipate: {anticipatedNetworkTransform.ShouldReanticipate}");
    }

    private Vector3 CalculateJetResistance(Vector3 currentVelocity, float desiredVerticalAcc, Vector3 desiredAcc)
    {
        Vector3 newVelocity = currentVelocity;

        // Vertical
        float currentVerticalSpeed = Mathf.Abs(currentVelocity.y);
        float currentDesiredVerticalAccel = Mathf.Abs(desiredVerticalAcc);

        if (currentVerticalSpeed > 0.0f && currentDesiredVerticalAccel > 0.0f)
        {
            float resist = Mathf.Clamp(Mathf.Pow(currentVerticalSpeed, verticalJetResistanceFactor) * verticalJetResistance, 0.0f, 0.25f);
            newVelocity.y *= (currentVerticalSpeed - resist * currentDesiredVerticalAccel) / currentVerticalSpeed;
        }

        // Horizontal
        Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        float currentHorizontalSpeed = currentHorizontalVelocity.magnitude;
        float currentDesiredAccel = desiredAcc.magnitude;

        if (currentHorizontalSpeed > 0.0f && currentDesiredAccel > 0.0f)
        {
            float resist = Mathf.Clamp(Mathf.Pow(currentHorizontalSpeed, horizontalJetResistanceFactor) * horizontalJetResistance, 0.0f, 0.925f);
            float scale = (currentHorizontalSpeed - resist * currentDesiredAccel) / currentHorizontalSpeed;
            newVelocity.x *= scale;
            newVelocity.z *= scale;
        }

        return newVelocity - currentVelocity;
    }

    private Vector3 CalculateVelocityCaps(Vector3 currentVelocity)
    {
        // Horizontal Velocity Cap
        Vector3 cappedVelocity = currentVelocity;
        Vector3 horizVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        float horizSpeed = horizVelocity.magnitude;
        if (horizSpeed > horizResistSpeed * 3.0f)
        {
            float maxSpeed = horizMaxSpeed * 3.0f;
            float targetSpeed = Mathf.Min(horizSpeed, maxSpeed);

            float scale = (targetSpeed - Time.fixedDeltaTime * horizResistFactor * (targetSpeed - horizResistSpeed)) / horizSpeed;
            cappedVelocity.x *= scale;
            cappedVelocity.z *= scale;
        }

        // Upward Velocity Cap
        if (cappedVelocity.y > upResistSpeed)
        {
            if (cappedVelocity.y > upMaxSpeed)
                cappedVelocity.y = upMaxSpeed;

            cappedVelocity.y -= Time.fixedDeltaTime * upResistFactor * (cappedVelocity.y - upResistSpeed);
        }

        // Downward Velocity Cap
        if (cappedVelocity.y < -downResistSpeed)
        {
            if (cappedVelocity.y < -downMaxSpeed)
                cappedVelocity.y = -downMaxSpeed;

            cappedVelocity.y += Time.fixedDeltaTime * downResistFactor * (cappedVelocity.y + downResistSpeed);
        }

        return cappedVelocity - currentVelocity;
    }

    // https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize/blob/022594f453adf5bd26f7cc40dd3ee27b06002738/Experimental/Anticipation%20Sample/Assets/Scripts/PlayerMovableObject.cs#L109
    public override void OnReanticipate(double lastRoundTripTime)
    {
        // Debug.Log("Reanticipating");
        // Get previous client-side state and the time that the server sent this authoritative state
        var previousState = anticipatedNetworkTransform.PreviousAnticipatedState;
        var authorityTime = NetworkManager.LocalTime.Time - lastRoundTripTime;

        // Sync physics after server overwrites the transform
        Physics.SyncTransforms();

        // Replay inputs between last round trip time and now
        var now = NetworkManager.LocalTime.Time;
        var lastInputTime = authorityTime;
        int count = 0;
        foreach (var input in inputBuffer)
        {
            if (inputBuffer.Count > count + 1 && input.timestamp == inputBuffer[count + 1].timestamp) continue;
            if (input.timestamp > authorityTime)
            {
                if ((float)(input.timestamp - lastInputTime) > 0.0f)
                {
                    Physics.simulationMode = SimulationMode.Script;
                    Physics.Simulate((float)(input.timestamp - lastInputTime));
                    Physics.simulationMode = SimulationMode.FixedUpdate;
                }

                HandleMovement(input);

                lastInputTime = input.timestamp;
                count++;
            }
        }
        if ((float)(now - lastInputTime) > 0.0f)
        {
            Physics.simulationMode = SimulationMode.Script;
            Physics.Simulate((float)(now - lastInputTime));
            Physics.simulationMode = SimulationMode.FixedUpdate;
        }

        inputBuffer.RemoveAll(item => item.timestamp < authorityTime);

        // This prevents small amounts of wobble from slight differences.
        var sqDist = Vector3.SqrMagnitude(previousState.Position - anticipatedNetworkTransform.AnticipatedState.Position);
        if (sqDist <= 0.1f)
        {
            anticipatedNetworkTransform.AnticipateState(previousState);
            Physics.SyncTransforms();
        }
        else if (sqDist < 3f * 3f)
        {
            // Server updates are not necessarily smooth, so applying reanticipation can also result in
            // hitchy, unsmooth animations. To compensate for that, we call this to smooth from the previous
            // anticipated state (stored in "anticipatedValue") to the new state (which, because we have used
            // the "Move" method that updates the anticipated state of the transform, is now the current
            // transform anticipated state)
            anticipatedNetworkTransform.Smooth(previousState, anticipatedNetworkTransform.AnticipatedState, 0.1f);
            Physics.SyncTransforms();
        }
    }
    #endregion


    #region Player State
    protected override void OnDie()
    {
        print("Player Died");
    }
    #endregion


    #region UI
    private void InitializePauseMenuElements()
    {
        // Initialize player options in pause menu
        FieldInfo[] fields = this.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuOptionAttribute[] attribute = (PauseMenuOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuOptionAttribute), true);

            if (attribute.Length > 0)
            {
                if (!PauseMenu.Instance.devMode && attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute)) continue;
                PauseMenu.Instance.AddOption(
                    attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute) ? "dev - " + attribute[0].label : attribute[0].label,
                    (float)field.GetValue(this),
                    attribute[0].minValue,
                    attribute[0].maxValue,
                    (float value) => { field.SetValue(this, value); }
                );
            }
        }

        // Initialize player controls in pause menu
        List<string> controlIgnoreList = new List<string> { "Pause","Move", "Look" };
        // InputActionMap movementMap = playerControls.Movement;
        foreach (var actionMap in playerControls.asset.actionMaps)
        {
            foreach (var action in actionMap)
            {
                if (controlIgnoreList.Contains(action.name)) continue;
                PauseMenu.Instance.AddControl(action);
            }
        }

        // Initialize player debug settings in pause menu
        if (!PauseMenu.Instance.devMode) return;
        fields = playerTelemetry.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuDevOptionAttribute[] attribute = (PauseMenuDevOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuDevOptionAttribute), true);

            if (attribute.Length > 0)
            {
                PauseMenu.Instance.AddDebug(
                    field.Name,
                    attribute[0].label,
                    (bool)field.GetValue(playerTelemetry),
                    value => { field.SetValue(playerTelemetry, value); }
                );
            }
        }
    }
    #endregion


    #region SceneManagement
    private void ChangedActiveScene(Scene _, Scene next)
    {
        ChangedActiveSceneRpc(next.name);
    }

    [Rpc(SendTo.Owner)]
    private void ChangedActiveSceneRpc(string sceneName)
    {
        if (GameManager.Instance?.debugMode == true) Debug.Log(GetType() + ": Changed active scene for " + name + " " + NetworkManager.Singleton.LocalClientId);
        if (playerUIObj) SceneManager.MoveGameObjectToScene(playerUIObj, SceneManager.GetSceneByName(sceneName));
        if (playerCameraObj) SceneManager.MoveGameObjectToScene(playerCameraObj, SceneManager.GetSceneByName(sceneName));

        if (sceneName == "Lobby") Initialize();
    }
    #endregion
}
