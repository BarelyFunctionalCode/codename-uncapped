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
public class PlayerController : Entity, IIdentifiable
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
    public CinemachineCamera thirdPersonCamera;
    private AudioListener audioListener;

    // UI
    [SerializeField] private GameObject playerUIPrefabObj;
    public GameObject playerUIObj;
    public HUD playerHUD;

    // Inputs
    public PlayerInputs playerInputs;

    // Physics
    [Header("Physics")]

    public Rigidbody localRb;

    public CharacterMovement characterMovement;


    // Weapons and Gear
    [Header("Weapons and Gear")]
    public Transform weaponMountPoint;
    public Transform throwableMountPoint;
    public PlayerLoadoutManager playerLoadout;






    public bool isInitialized = false;


    #region Lifecycle
    private void Awake()
    {
        playerLoadout = GetComponent<PlayerLoadoutManager>();
        playerInputs = GetComponent<PlayerInputs>();
        characterMovement = GetComponent<CharacterMovement>();
    }

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SceneManager.activeSceneChanged += ChangedActiveScene;
        localTransform = transform;
        localRb = GetComponent<Rigidbody>();
        localRb.sleepThreshold = 0.0f;
    }

    public sealed override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SceneManager.activeSceneChanged -= ChangedActiveScene;
        if (IsOwner && audioListener) audioListener.enabled = false;
    }

    private void Update()
    {
        if (!playerTypeObj && IsServer && GameManager.Instance.isInitialized) SetPlayerType(playerTypePrefabObj);
        if (!isInitialized || !(IsServer || IsOwner) || state.IsDead) return;

        // Death plane check
        if (IsServer && localTransform.position.y < -1000f) state.Die();

        // Update player telemetry for debugging purposes
        if (IsOwner && playerTelemetry != null) playerTelemetry.Update();

        // Character ground detection and physics material updates
        characterMovement.ProcessUpdate(playerInputs.IsSkiing);
    }

    void LateUpdate()
    {
        if (!isInitialized || !(IsOwner || IsServer) || state.IsDead) return;

        // Handle camera pitch rotation on local client
        playerInputs.HandleCameraInput();
        
        // Additional visual effects based on player state
        localPlayerType.HandleExtraMotion(playerInputs.MovementDirection, playerInputs.IsSkiing, characterMovement.SurfaceNormal);
    }

    void FixedUpdate()
    {
        if (!isInitialized || !(IsServer || IsOwner) || state.IsDead) return;

        // First, we collect all of the inputs that go into moving the player, and create an input state
        playerInputs.HandleInputs();

        // Set audio values
        localPlayerType.HandleAudio(localRb.linearVelocity, playerInputs.IsSkiing);

        // Set animator values
        localPlayerType.UpdateAnimationData(
            playerInputs.MovementInput,
            localRb.linearVelocity,
            state.IsGrounded,
            playerInputs.IsRunning,
            playerInputs.IsSkiing,
            playerInputs.IsDownJetting,
            playerInputs.IsUpJetting
        );

        // Finally, we process the inputs to move the player locally and on the server
        characterMovement.ProcessFixedUpdate(
            playerInputs.IsRunning,
            playerInputs.IsJumping,
            playerInputs.IsSkiing,
            playerInputs.IsJetting,
            playerInputs.IsUpJetting,
            playerInputs.IsDownJetting,
            playerInputs.MovementDirection,
            playerInputs.RotationInputX
        );
        if (IsOwner && !IsHost) ClientAuthorityRotationSyncRpc(localRb.rotation);
        if (playerInputs.IsJumping) 
        {
            localPlayerType.HandleJump();
            playerInputs.ResetJumpInput();
        }

        if (playerTelemetry != null)
        {
            playerTelemetry.movementDirection = playerInputs.MovementDirection;
            playerTelemetry.isSkiing = playerInputs.IsSkiing;
            playerTelemetry.isUpJetting = playerInputs.IsUpJetting;
            playerTelemetry.isDownJetting = playerInputs.IsDownJetting;
            playerTelemetry.position = localTransform.position;
            playerTelemetry.velocity = localRb.linearVelocity;
            playerTelemetry.distanceToSurface = characterMovement.DistanceToSurface;
            playerTelemetry.surfacePoint = characterMovement.SurfacePoint;
            playerTelemetry.isGrounded = state.IsGrounded;
            playerTelemetry.surfaceNormal = characterMovement.SurfaceNormal;
        }
    }

    [Rpc(SendTo.Server)]
    private void ClientAuthorityRotationSyncRpc(Quaternion ownerRotation)
    {
        localRb.rotation = Quaternion.RotateTowards(localRb.rotation, ownerRotation, playerInputs.horizontalRotationLimit);
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

        if (!isPuppet)
        {
            localRb.mass = playerType.mass;
            weaponMountPoint = playerType.weaponMountPoint;
            throwableMountPoint = playerType.throwableMountPoint;
        }

        characterMovement.UpdateCharacterData(null, playerType.playerCollider, null);

        GetComponent<PickupContainer>().pickupHoldPoint = playerType.pickupContainerHoldPoint;

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
            localPlayerType.playerCollider.enabled = false;

            // Spawn a non-authoritative puppet on local client for predicting the player's position and rotation before the server updates it
            playerPuppetObj = Instantiate(playerPuppetPrefabObj, localTransform.position, localTransform.rotation);
            PlayerPuppet playerPuppet = playerPuppetObj.GetComponent<PlayerPuppet>();
            playerPuppet.Initialize(this);

            // Set the local player's transform, collider, and rigidbody references to the puppet's so that the rest of the
            // player controller code can work as normal regardless of whether it's running on the server or client
            localTransform = playerPuppetObj.transform;
            localRb = playerPuppet.rb;
            localRb.mass = localPlayerType.mass;
            characterMovement.UpdateCharacterData(localTransform, null, localRb);
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


    #region Movement
    public void Teleport(Vector3 destination, Quaternion rotation = default)
    {
        if (!IsServer) return;

        playerInputs.SetPlayerControlsRpc(false);
        characterMovement.Teleport(destination, rotation);
        playerInputs.SetPlayerControlsRpc(true);
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


    #region Player Identification
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
