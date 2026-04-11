using Steamworks;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerLoadoutManager))]
[RequireComponent(typeof(PlayerNetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(Identification))]
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(Energy))]
[RequireComponent(typeof(Health))]
public class PlayerController : Entity, IGravityModifiable, IIdentifiable
{
    [Space(20)]
    // Debug
    [SerializeField] private DevVectorRenderer devVectorRenderer;
    public PlayerTelemetry playerTelemetry;

    public Transform localTransform;

    [SerializeField] public GameObject playerTypePrefabObj;
    private GameObject playerTypeObj;
    public PlayerType localPlayerType;

    [SerializeField] private GameObject playerPuppetPrefabObj;
    public GameObject playerPuppetObj;

    // ID
    private SteamId _steamId;
    public SteamId SteamId { get { return _steamId; } }

    // Audio
    [SerializeField] private AudioSource respawnAudioSource;

    // Camera
    [SerializeField] private GameObject playerCameraPrefabObj;
    private GameObject playerCameraObj;
    private CinemachineCamera thirdPersonCamera;
    [PauseMenuOption("Horizontal Look", 0f, 100f)]
    public float horizontalRotationSpeed = 20f;
    private readonly float horizontalRotationLimit = 100f;
    private AudioListener audioListener;

    // UI
    [SerializeField] private GameObject playerUIPrefabObj;
    public GameObject playerUIObj;
    public HUD playerHUD;

    // Inputs
    public PlayerControls playerControls;
    private int controlsDisabledCount = 0;
    Vector3 movementInput = Vector3.zero;
    Vector3 newMovementDirection = Vector3.zero;
    Vector3 movementDirection = Vector3.zero;
    float rotationInputX = 0f;
    float rotationInputY = 0f;
    Vector3 rotationDeltaYaw = Vector3.zero;
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
    public Rigidbody localRb;
    public CapsuleCollider localPlayerCollider;
    Vector3 surfaceNormal = Vector3.up;
    Vector3 surfacePoint = Vector3.zero;
    float distanceToSurface = Mathf.Infinity;
    float lastGroundedTime = 0;

    // Weapons and Gear
    [Header("Weapons and Gear")]
    public Transform weaponMountPoint;
    public Transform throwableMountPoint;
    public PlayerLoadoutManager playerLoadout;

    // Movement Parameters
    [SerializeField] private LayerMask groundeDetectionLayerMask;
    private readonly float hoverHeightMax = 0.02f;

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

    private NetworkVariable<float> gravityModifier = new();

    public bool isInitialized = false;


    #region Lifecycle
    private void Awake()
    {
        playerLoadout = GetComponent<PlayerLoadoutManager>();
    }

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SceneManager.activeSceneChanged += ChangedActiveScene;

        localTransform = transform;
        localRb = GetComponent<Rigidbody>();
        localRb.sleepThreshold = 0.0f;
        if (IsServer) gravityModifier.Value = 1f;

        if (IsOwner)
        {            
            // Set up the player controls
            playerControls = new PlayerControls();

            // Set up the input callbacks
            playerControls.Enable();
            playerControls.Character.Move.performed += ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Character.Move.canceled += ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Character.Look.performed += ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Character.Look.canceled += ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Character.PrimaryFire.started += ctx => playerLoadout.OnPrimaryFireStartedRpc();
            playerControls.Character.PrimaryFire.canceled += ctx => playerLoadout.OnPrimaryFireCanceledRpc();
            playerControls.Character.Throwable.started += ctx => playerLoadout.OnThrowableStartedRpc();
            playerControls.Character.Throwable.canceled += ctx => playerLoadout.OnThrowableCanceledRpc();
            playerControls.Character.NextWeapon.started += ctx => playerLoadout.NextWeaponRpc();
            playerControls.Character.PreviousWeapon.started += ctx => playerLoadout.PreviousWeaponRpc();
            playerControls.Character.Ski.performed += ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Character.Ski.canceled += ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Character.JumpJet.performed += ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Character.JumpJet.canceled += ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Character.DownJet.performed += ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Character.DownJet.canceled += ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Character.JumpJet.started += ctx => JumpInput();
            playerControls.Character.ToggleCameraView.started += ctx => ToggleCameraView();
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
            playerControls.Character.Move.performed -= ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Character.Move.canceled -= ctx => MoveInput(ctx.ReadValue<Vector2>());
            playerControls.Character.Look.performed -= ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Character.Look.canceled -= ctx => LookInput(ctx.ReadValue<Vector2>());
            playerControls.Character.PrimaryFire.started -= ctx => playerLoadout.OnPrimaryFireStartedRpc();
            playerControls.Character.PrimaryFire.canceled -= ctx => playerLoadout.OnPrimaryFireCanceledRpc();
            playerControls.Character.Throwable.started -= ctx => playerLoadout.OnThrowableStartedRpc();
            playerControls.Character.Throwable.canceled -= ctx => playerLoadout.OnThrowableCanceledRpc();
            playerControls.Character.NextWeapon.started -= ctx => playerLoadout.NextWeaponRpc();
            playerControls.Character.PreviousWeapon.started -= ctx => playerLoadout.PreviousWeaponRpc();
            playerControls.Character.Ski.performed -= ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Character.Ski.canceled -= ctx => SkiInput(ctx.ReadValue<float>());
            playerControls.Character.JumpJet.performed -= ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Character.JumpJet.canceled -= ctx => JetInput(ctx.ReadValue<float>());
            playerControls.Character.DownJet.performed -= ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Character.DownJet.canceled -= ctx => DownJetInput(ctx.ReadValue<float>());
            playerControls.Character.JumpJet.started -= ctx => JumpInput();
            playerControls.Character.ToggleCameraView.started -= ctx => ToggleCameraView();
            // Disable audio listener
            if (audioListener) audioListener.enabled = false;
        }
    }

    private void Update()
    {
        if (IsServer && localTransform.position.y < -1000f) state.Die();

        if (!playerTypeObj && IsServer && GameManager.Instance.isInitialized) SetPlayerType(playerTypePrefabObj);
        if (!isInitialized || !(IsServer || IsOwner)) return;

        if (IsOwner)
        {
            playerTelemetry.Update();
            
            // TODO: Move this to a better place
            Vector3 newMovementDirection = localTransform.TransformDirection(movementInput); // NOT SUPPOSED TO BE NORMALIZED
            bool changed = false;
            if (newMovementDirection != this.newMovementDirection) changed = true;
            this.newMovementDirection = newMovementDirection;
            if (changed) MoveDirectionRpc(newMovementDirection);
        }

        if (state.IsDead) return;

        HandleGroundDetection();

        // Apply Drag and Friction
        localRb.linearDamping = distanceToSurface <= airCushionHeight ? airCushionDrag : drag;
        localPlayerCollider.material = isSkiing ? skiMaterial : normalMaterial;
    }

    [Rpc(SendTo.Server)]
    private void MoveDirectionRpc(Vector3 newMovementDirection)
    {
        this.newMovementDirection = newMovementDirection;
    }

    void LateUpdate()
    {
        if (!isInitialized || !(IsOwner || IsServer)) return;

        if (state.IsDead) return;

        // Handle camera pitch rotation on local client
        localPlayerType.HandleCamera(rotationInputY, controlsDisabledCount);
        rotationInputY = 0f;
        localPlayerType.HandleExtraMotion(movementDirection, isSkiing, surfaceNormal);
    }

    void FixedUpdate()
    {
        if (!isInitialized || !(IsServer || IsOwner)) return;
        if (state.IsDead) return;

        // First, we collect all of the inputs that go into moving the player, and create an input state
        HandleInputs();

        // Finally, we process the inputs to move the player locally and on the server
        HandleMovement();
        if (IsOwner && !IsHost) ClientAuthorityRotationSyncRpc(localRb.rotation);

        if (IsServer)
        {
            if (gravityModifier.Value != 1f)
            {
                gravityModifier.Value = Mathf.Lerp(gravityModifier.Value, 1f, Time.fixedDeltaTime * 5f);
                if (Mathf.Abs(gravityModifier.Value - 1f) < 0.01f) gravityModifier.Value = 1f;
            }
        }

        isJumping = false;
        if (playerTelemetry != null)
        {
            playerTelemetry.position = localTransform.position;
            playerTelemetry.finalVelocity = localRb.linearVelocity;
        }
    }
    #endregion


    #region Initialization
    private void SetPlayerType(GameObject playerTypePrefabObj)
    {
        if (!IsServer) return;
        if (playerTypeObj != null) Destroy(playerTypeObj);
        playerTypeObj = SpawnManager.Instance.Spawn(
            playerTypePrefabObj,
            false,
            transform.position,
            transform.rotation,
            transform,
            OwnerClientId
        );
    }

    public void OnPlayerTypeObjectSpawned(PlayerType playerType, bool isPuppet = false)
    {
        localPlayerType = playerType;
        localPlayerCollider = playerType.playerCollider;
        localPlayerCollider.material = normalMaterial;

        if (!isPuppet)
        {
            localRb.mass = playerType.mass;
            weaponMountPoint = playerType.weaponMountPoint;
            throwableMountPoint = playerType.throwableMountPoint;
        }

        if (IsOwner) InitializeOwner();
    }

    public void InitializeOwner()
    {
        if (!IsOwner) return;
        if (!IsHost && !playerPuppetObj)
        {
            // Hide all visuals on authoritative player object
            foreach (Renderer r in localPlayerType.gameObject.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }

            // Disable audio sources
            foreach (AudioSource a in localPlayerType.gameObject.GetComponentsInChildren<AudioSource>())
            {
                a.enabled = false;
            }

            // Disable the collider on the authoritative player object so it doesn't interfere with the puppet's collider
            localPlayerCollider.enabled = false;

            // Spawn a non-authoritative puppet on local client for predicting the player's position and rotation before the server updates it
            playerPuppetObj = Instantiate(playerPuppetPrefabObj, localTransform.position, localTransform.rotation);
            PlayerPuppet playerPuppet = playerPuppetObj.GetComponent<PlayerPuppet>();
            playerPuppet.Initialize(this);

            // Set the local player's transform, collider, and rigidbody references to the puppet's so that the rest of the
            // player controller code can work as normal regardless of whether it's running on the server or client
            localTransform = playerPuppetObj.transform;
            localRb = playerPuppet.rb;
            return;
        }

        // Initialize Player UI
        if (!playerUIObj) {
            playerUIObj = Instantiate(playerUIPrefabObj);
            playerHUD = playerUIObj.GetComponentInChildren<HUD>();
            playerTelemetry = new PlayerTelemetry(devVectorRenderer);
        }

        // Initialize Player Camera
        if (!playerCameraObj)
        {
            playerCameraObj = Instantiate(playerCameraPrefabObj);
            audioListener = playerCameraObj.GetComponentInChildren<AudioListener>();
            thirdPersonCamera = playerCameraObj.GetComponentInChildren<CinemachineCamera>();
            thirdPersonCamera.Follow = localPlayerType.freeLookTargetTransform;

            Camera UIOverlayCamera = playerUIObj.GetComponentInChildren<Canvas>().worldCamera;
            Camera mainCamera = playerCameraObj.GetComponentInChildren<Camera>();
            var cameraData = mainCamera.GetUniversalAdditionalCameraData();
            cameraData.cameraStack.Add(UIOverlayCamera);

            // Enable audio listener
            audioListener.enabled = true;

            // Enable the camera
            thirdPersonCamera.Priority.Value = 1;
        }

        InitializeServerRpc(GameManager.Instance?.usingSteam == true ? SteamClient.SteamId.Value : 0);
        isInitialized = true;
    }

    [Rpc(SendTo.Server)]
    private void InitializeServerRpc(ulong steamId)
    {
        identification.SetEntityName($"Player {OwnerClientId}");
        identification.SetEntityId(OwnerClientId);
        identification.SetTeamId((uint)OwnerClientId);

        // Get Player's Steam ID
        if (GameManager.Instance?.usingSteam == true)
        {
            _steamId = steamId;
            identification.SetEntityName(new Friend(_steamId).Name);
        }

        playerLoadout.Initialize(true, this);
        PostInitializeRpc();
        isInitialized = true;
        GameManager.Instance.OnClientConnectedEvent.Invoke(OwnerClientId);
    }

    [Rpc(SendTo.Owner)]
    private void PostInitializeRpc()
    {
        playerHUD.Initialize(this);
    }
    #endregion


    #region Cleanup
    [Rpc(SendTo.Server)]
    public void DisconnectCleanupRpc()
    {
        playerLoadout.Deinitialize();
        DisconnectCleanupOwnerRpc();
        GameManager.Instance.OnClientDisconnectedEvent.Invoke(OwnerClientId);
        isInitialized = false;
    }

    [Rpc(SendTo.Owner)]
    private void DisconnectCleanupOwnerRpc()
    {
        // Disable the UI
        if (playerCameraObj)
        {
            Camera mainCamera = playerCameraObj.GetComponentInChildren<Camera>();
            var cameraData = mainCamera.GetUniversalAdditionalCameraData();
            cameraData.cameraStack.Remove(playerUIObj.GetComponentInChildren<Canvas>().worldCamera);
            Destroy(playerCameraObj);
            playerCameraObj = null;
        }
        if (playerUIObj)
        {
            Destroy(playerUIObj);
            playerUIObj = null;
        }
        if (playerPuppetObj)
        {
            Destroy(playerPuppetObj);
            playerPuppetObj = null;
        }

        isInitialized = false;
    }
    #endregion


    #region Inputs
    private void ToggleCameraView()
    {
        bool isThirdPerson = thirdPersonCamera.Priority.Value > 0;
        thirdPersonCamera.Priority.Value = isThirdPerson ? 0 : 1;
        localPlayerType.ToggleFirstPersonCamera(isThirdPerson);
    }

    [Rpc(SendTo.Server)]
    public void SetPlayerControlsRpc(bool enabled)
    {
        controlsDisabledCount += enabled ? -1 : 1;
        controlsDisabledCount = Mathf.Max(0, controlsDisabledCount);
        SetPlayerControlsOwnerRpc(controlsDisabledCount == 0);
    }
    [Rpc(SendTo.Owner)]
    private void SetPlayerControlsOwnerRpc(bool enabled)
    {
        if (enabled) playerControls.Character.Enable();
        else playerControls.Character.Disable();
    }

    [Rpc(SendTo.Owner)]
    public void SetHUDActiveRpc(bool enabled)
    {
        playerHUD.SetHUDActive(enabled);
    }
    [Rpc(SendTo.Owner)]
    public void OpenLoadoutMenuRpc()
    {
        playerHUD.ToggleMenu(HUDMenu.LoadoutMenu, true);
    }
    [Rpc(SendTo.Owner)]
    public void SetCursorStateRpc(bool enabled, bool usingCustomCursor = false)
    {
        playerHUD.SetCursorState(enabled, usingCustomCursor);
    }

    private void MoveInput(Vector2 rawMovementInput)
    {
        movementInput = new(rawMovementInput.x, 0f, rawMovementInput.y);
        MoveInputRpc(movementInput);
    }
    [Rpc(SendTo.Server)]
    private void MoveInputRpc(Vector3 movementInput)
    {
        this.movementInput = movementInput;
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
        Vector3 rotationYaw = new(0f, rotationInputX, 0f);
        rotationInputX = 0f;
        rotationYaw *= horizontalRotationSpeed * Time.fixedDeltaTime;
        rotationDeltaYaw = Vector3.ClampMagnitude(rotationYaw, horizontalRotationLimit);

        // Get direction of movement relative to player rotation
        Vector3 movement = movementInput;
        movementDirection = newMovementDirection;

        // Get input for skiing, jumping, and down jetting
        isUpJetting = upJettingInput && isSkiing;
        isDownJetting = downJettingInput && isSkiing;
        isJetting = isUpJetting || isDownJetting;
        isMoving = movement.magnitude > 0.0f;
        isRunning = state.IsGrounded && isMoving && !isSkiing;

        if (controlsDisabledCount > 0)
        {
            movementDirection = Vector3.zero;
            rotationDeltaYaw = Vector3.zero;
            isSkiing = false;
            isUpJetting = false;
            isDownJetting = false;
            isJetting = false;
            isMoving = false;
            isRunning = false;
        }
        
        if (playerTelemetry != null)
        {
            playerTelemetry.movementDirection = movementDirection;
            playerTelemetry.isSkiing = isSkiing;
            playerTelemetry.isUpJetting = isUpJetting;
            playerTelemetry.isDownJetting = isDownJetting;
        }

        // Set audio values
        localPlayerType.HandleAudio(localRb.linearVelocity, isSkiing);

        // Set animator values
        localPlayerType.UpdateAnimationData(movement, localRb.linearVelocity, state.IsGrounded, isRunning, isSkiing, isDownJetting, isUpJetting);
    }
    #endregion


    #region Movement
    public void SetGravityModifier(float modifier)
    {
        if (!IsSpawned || !IsServer) return;
        gravityModifier.Value = modifier;
    }

    public void Teleport(Vector3 destination, Quaternion rotation = default)
    {
        if (!IsServer) return;

        SetPlayerControlsRpc(false);
        localRb.isKinematic = true;
        localPlayerCollider.enabled = false;

        // localRb.position = destination;
        // if (rotation != default) localRb.rotation = rotation;
        // localRb.PublishTransform();
        GetComponent<NetworkTransform>().Teleport(destination, rotation, localTransform.lossyScale);

        localPlayerCollider.enabled = true;
        localRb.isKinematic = false;
        SetPlayerControlsRpc(true);
    }

    private void HandleGroundDetection()
    {
        lastGroundedTime += Time.deltaTime;
        state.SetIsGrounded(false);

        distanceToSurface = Mathf.Infinity;
        surfaceNormal = Vector3.up;
        surfacePoint = Vector3.zero;

        // Raycast down...
        Vector3 groundCheckPoint = localPlayerCollider.bounds.center;
        RaycastHit hit;
        bool didHit = Physics.Raycast(
            new Ray(
                groundCheckPoint,
                Vector3.down
            ),
            out hit,
            distanceToSurface,
            groundeDetectionLayerMask
        );
        if (didHit)
        {
            if (playerTelemetry != null) playerTelemetry.isGrounded = state.IsGrounded;
            
            // Surface too steep
            float slope = Vector3.Dot(hit.normal, Vector3.up);
            if (slope <= 0.1f) return;

            surfacePoint = hit.point;
            distanceToSurface = Mathf.Max(Vector3.Distance(surfacePoint, groundCheckPoint) - localPlayerCollider.bounds.extents.y - 0.2f, 0.0f);

            if (playerTelemetry != null) playerTelemetry.distanceToSurface = distanceToSurface;
            if (playerTelemetry != null) playerTelemetry.surfacePoint = surfacePoint;

            // Breakaway vertical speed check
            if (localRb.linearVelocity.y > 20.0f) return;

            if (distanceToSurface <= 0.6f)
            {
                state.SetIsGrounded(true);
                lastGroundedTime = 0f;
            }
            else if (lastGroundedTime < 0.2f) state.SetIsGrounded(true);
            else state.SetIsGrounded(false);

            if (state.IsGrounded) surfaceNormal = hit.normal;
        }

        if (playerTelemetry != null) playerTelemetry.isGrounded = state.IsGrounded;
        if (playerTelemetry != null) playerTelemetry.surfaceNormal = surfaceNormal;
        
    }

    [Rpc(SendTo.Server)]
    private void ClientAuthorityRotationSyncRpc(Quaternion ownerRotation)
    {
        localRb.rotation = Quaternion.RotateTowards(localRb.rotation, ownerRotation, horizontalRotationLimit);
    }

    private void HandleMovement()
    {
        Vector3 currentVelocity = localRb.linearVelocity;
        Vector3 desiredAcc = Vector3.zero;
        Vector3 groundImpulse = Vector3.zero;
        float desiredVerticalAcc = 0f;

        float gravityMagnitude = Physics.gravity.magnitude * gravityModifier.Value;

        // Air Control
        if (!state.IsGrounded && !isJetting && !isSkiing)
        {
            Vector3 airDirection = movementDirection.normalized;
            Vector3 airControlAcc = airDirection * airControl;

            float maxAccel = runForce / localRb.mass * Time.fixedDeltaTime * 0.3f;

            if (airControlAcc.magnitude > maxAccel)
            {
                airControlAcc = airControlAcc.normalized * maxAccel;
            }
            desiredAcc.x += airControlAcc.x;
            desiredAcc.z += airControlAcc.z;
        }

        // Jumping
        if (isJumping && state.IsGrounded && currentVelocity.y <= maxJumpSpeed)
        {
            float jumpScale = 1.0f;

            if (currentVelocity.y < minJumpSpeed)
            {
                jumpScale = 1.0f - currentVelocity.y / minJumpSpeed / (maxJumpSpeed / minJumpSpeed);
            }

            Vector3 jumpDirection = movementDirection.normalized;

            float playerScaleFactor = localTransform.localScale.y * 0.25f + 0.75f;
            float jumpForceFinal = jumpForce / localRb.mass;

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
            localPlayerType.HandleJump();
        }
        // Running Movement
        else if (isRunning)
        {
            groundImpulse = new(0f, -gravityMagnitude * Time.fixedDeltaTime, 0f);
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

            float maxRunAccel = runForce / localRb.mass * Time.fixedDeltaTime;
            if (velocityDiff.magnitude > maxRunAccel)
                velocityDiff *= maxRunAccel / velocityDiff.magnitude;

            groundImpulse += velocityDiff;
        }

        // Skiing Movement
        if (isSkiing && energy.CurrentEnergy > 0.0f)
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
                desiredAcc = 2.0f * hoverFactor * gravityMagnitude * Time.fixedDeltaTime * Vector3.ProjectOnPlane(surfaceNormal, Vector3.up);
            }
            else
            {
                // Going Uphill?
                Vector3 surfaceDirection = (surfaceNormal - lateralVelocityDir * surfaceNormalDotLateralVelocityDirection).normalized;
                Vector3 sideDirection = -lateralVelocityDir;
                float sideDot = Vector3.Dot(surfaceDirection, sideDirection);
                
                desiredAcc = 0.5f * hoverFactor * gravityMagnitude * Time.fixedDeltaTime * (surfaceDirection - lateralVelocityDir * sideDot);
            }
            desiredAcc.y = 0.0f;
            Vector3 hoverVertAcc = hoverFactor * gravityMagnitude * Time.fixedDeltaTime * Vector3.up;
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
            if (isSkiing && movementDirection.magnitude > 0.01f && energy.CurrentEnergy > 0.0f)
            {
                float lateralForce = jetDirectionalForceXY / localRb.mass * accelScale * Time.fixedDeltaTime;
                desiredAcc += movementDirection * lateralForce;
                energy.ApplyEnergyDelta(-jetSkateEnergyDrain * accelScale * Time.fixedDeltaTime);
            }

            // Up Jetting
            if (isJetting && energy.CurrentEnergy > 0.01f)
            {
                float force = 0f;
                if (isUpJetting)
                {
                    float cushion = 1.0f;
                    if (distanceToSurface <= airCushionHeight)
                        cushion = (airCushionHeight - distanceToSurface) / airCushionHeight;

                    force = upJetForce / localRb.mass * accelScale * Time.fixedDeltaTime;
                    force += force * cushion * 0.5f;
                }
                else if (isDownJetting) // Down Jetting
                {
                    force = -downJetForce / localRb.mass * accelScale * Time.fixedDeltaTime;
                }

                desiredVerticalAcc = force;
                energy.ApplyEnergyDelta(- (isUpJetting ? upJettingEnergyDrain : downJettingEnergyDrain) * accelScale * Time.fixedDeltaTime);
            }
        }

        if (playerTelemetry != null) playerTelemetry.rawInputVelocity = currentVelocity + desiredAcc + groundImpulse;

        // Apply Jet Resistance
        currentVelocity += CalculateJetResistance(currentVelocity, desiredVerticalAcc, desiredAcc);
        if (playerTelemetry != null) playerTelemetry.jetResistVelocity = currentVelocity;

        // Apply desired acceleration, jetting accelration, walking acceleration, and gravity
        desiredAcc.y += desiredVerticalAcc;
        desiredAcc.y -= gravityMagnitude * Time.fixedDeltaTime;
        desiredAcc += groundImpulse;
        currentVelocity += desiredAcc;

        // Apply velocity caps
        currentVelocity += CalculateVelocityCaps(currentVelocity);
        if (playerTelemetry != null) playerTelemetry.cappedSpeedVelocity = currentVelocity;
        // Debug.Log($"Current Velocity: {rb.linearVelocity:F2}\t Desired Acc: {desiredAcc:F2}\t Jet Resistance: {jetResistance:F2}\t Capped Excess: {velocityCappedExcess:F2}\t Final Velocity: {currentVelocity:F2}");
    
        // Calculate final change in velocity to apply
        Vector3 finalVelocityChange = currentVelocity - localRb.linearVelocity;

        // Calculate rotation to apply
        Quaternion newRot = Quaternion.Euler(localRb.rotation.eulerAngles + rotationDeltaYaw);

        // Apply velocity and rotation updates to rigidbody
        localRb.AddForce(finalVelocityChange, ForceMode.VelocityChange);
        localRb.MoveRotation(newRot);
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
    #endregion

    #region Collision
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Projectile")) return;

        float minimumDamageSpeed = 40f; // Minimum speed for a collision to cause damage
        float minimumOuchyAngle = 15f; // Minimum angle for a collision to cause damage
        float scaleFactor = 0.4f; // Overall scale factor for damage, can be tweaked for balance

        Vector3 relativeVelocity = collision.relativeVelocity;

        Vector3 impactDirection = relativeVelocity.normalized;

        ContactPoint contact = collision.GetContact(0);
        Vector3 surfaceNormal = contact.normal;

        Vector3 damagingVelocity = Vector3.Project(relativeVelocity, surfaceNormal);
        float damagingSpeed = damagingVelocity.magnitude;

        float ouchyThreshold = Vector3.Dot(impactDirection, surfaceNormal);

        if (ouchyThreshold > Mathf.Sin(minimumOuchyAngle * Mathf.Deg2Rad) && damagingSpeed > minimumDamageSpeed)
        {
            float damage = (damagingSpeed - minimumDamageSpeed) * ouchyThreshold * scaleFactor;
            health.TakeDamage(damage);
        }
    }


    #endregion


    #region Player State
    

    public IdentifierData GetIdentifierData()
    {
        return new IdentifierData
        {
            color = IdentifierManager.TempTeamColors[identification.FetchTeamId()],
            topText = identification.FetchEntityName(),
            bottomText = $"{Mathf.CeilToInt(health.HealthPercentage * 100f)}%",
            isActive = health.CurrentHealth > 0
        };
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
        if (playerPuppetObj) SceneManager.MoveGameObjectToScene(playerPuppetObj, SceneManager.GetSceneByName(sceneName));
        if (playerUIObj) SceneManager.MoveGameObjectToScene(playerUIObj, SceneManager.GetSceneByName(sceneName));
        if (playerCameraObj) SceneManager.MoveGameObjectToScene(playerCameraObj, SceneManager.GetSceneByName(sceneName));

        // if (sceneName == "Lobby") InitializeOwner();
    }
    #endregion
}
