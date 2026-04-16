using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(PlayerInputs))]
[RequireComponent(typeof(PlayerNetworkTransform))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(Identification))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Energy))]
[RequireComponent(typeof(PlayerLoadoutManager))]
[RequireComponent(typeof(DevVectorRenderer))]
[RequireComponent(typeof(PickupContainer))]
public class PlayerController : Entity, IIdentifiable
{
    // Generic Character things
    [HideInInspector] public CharacterMovement characterMovement;

    // Player specific things
    [Header("Prefabs")]
    public GameObject playerTypePrefabObj;
    [SerializeField] private GameObject playerPuppetPrefabObj;
    [SerializeField] private GameObject thirdPersonCameraPrefabObj;
    [SerializeField] private GameObject playerUIPrefabObj;

    // Debug
    private DevVectorRenderer devVectorRenderer;
    [HideInInspector] public PlayerTelemetry playerTelemetry;

    [Header("Main Components")]
    [HideInInspector] public PlayerInputs playerInputs;
    [HideInInspector] public GameObject playerPuppetObj;
    [HideInInspector] public PlayerLoadoutManager playerLoadout;
    public Transform localTransform;
    public PlayerType localPlayerType;
    public Rigidbody localRb;

    // Visuals
    [HideInInspector] public PlayerCamera thirdPersonCamera;
    [HideInInspector] public HUD playerHUD;

    [Header("State")]
    public bool isInitialized = false;


    #region Lifecycle
    private void Awake()
    {
        characterMovement = GetComponent<CharacterMovement>();

        devVectorRenderer = GetComponent<DevVectorRenderer>();
        playerLoadout = GetComponent<PlayerLoadoutManager>();
        playerInputs = GetComponent<PlayerInputs>();
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
    }

    private void Update()
    {
        if (!localPlayerType && IsServer && GameManager.Instance.isInitialized) SetPlayerType(playerTypePrefabObj);
        if (!isInitialized || !(IsServer || IsOwner) || state.IsDead) return;

        // Update player telemetry for debugging purposes
        if (IsOwner && playerTelemetry != null) playerTelemetry.Update();


        // Death plane check
        if (IsServer && localTransform.position.y < -1000f) state.Die();

        // Character ground detection and physics material updates
        characterMovement.ProcessUpdate();
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
        characterMovement.SetMovementInputs(
            playerInputs.MovementDirection,
            playerInputs.RotationInputX,
            playerInputs.IsJumping,
            playerInputs.IsSkiing,
            playerInputs.IsUpJetting,
            playerInputs.IsDownJetting
        );

        // Set animator values
        localPlayerType.UpdateAnimationData(
            playerInputs.MovementInput,
            localRb.linearVelocity,
            state.IsGrounded,
            playerInputs.IsSkiing,
            playerInputs.IsDownJetting,
            playerInputs.IsUpJetting,
            playerInputs.IsJumping
        );
        // Set audio values
        localPlayerType.HandleAudio(localRb.linearVelocity, playerInputs.IsSkiing);

        // Finally, we process the inputs to move the player locally and on the server
        characterMovement.ProcessFixedUpdate();

        // This makes the client's local rotation authoritative
        if (IsOwner && !IsHost) ClientAuthorityRotationSyncRpc(localRb.rotation);

        playerInputs.ResetJumpInput();

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
    // The first thing that happens when the player spawns is that we spawn their PlayerType object,
    // which contains all of the visual and animation data for the player. PlayerType is a separate Network Object
    // that is spawned as a child of the PlayerController, and the PlayerController holds a reference to it.
    // This separation allows us to easily swap out the player's model and animations by simply despawning the
    // current PlayerType and spawning a new one.
    private void SetPlayerType(GameObject playerTypePrefabObj)
    {
        if (!IsServer) return;
        if (localPlayerType != null)
        {
            Destroy(localPlayerType.gameObject);
            localPlayerType = null;
        }
        SpawnManager.Instance.Spawn(
            playerTypePrefabObj,
            false,
            transform.position,
            transform.rotation,
            transform,
            OwnerClientId
        );
    }

    // This is called by the newly spawned PlayerType object after it finishes initializing itself.
    public void OnPlayerTypeObjectSpawned(PlayerType playerType)
    {
        localPlayerType = playerType;
        localRb.mass = playerType.mass;
        characterMovement.UpdateCharacterData(null, playerType.playerCollider, null);
        GetComponent<PickupContainer>().pickupHoldPoint = playerType.pickupContainerHoldPoint;

        if (IsOwner) InitializeOwner();
    }

    // InitializeOwner is called once on the local client after the player's PlayerType object is spawned and initialized.
    // This is to set up any local-only parts of the player object, like the camera and UI.
    public void InitializeOwner()
    {
        if (!IsOwner) return;

        // Initialize Player Puppet for local client prediction of player movement before server updates are received
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
            // Set the local player's transform and, and rigidbody references to the puppet's so that the rest of the
            localTransform = playerPuppetObj.transform;
            localRb = playerPuppet.rb;
            characterMovement.UpdateCharacterData(localTransform, null, localRb);
            playerPuppet.Initialize(this);
            return;
        }

        // Initialize Player UI
        if (!playerHUD) {
            GameObject playerUIObj = Instantiate(playerUIPrefabObj);
            playerHUD = playerUIObj.GetComponentInChildren<HUD>();
            playerTelemetry = new PlayerTelemetry(devVectorRenderer);
        }

        // Initialize Player Camera
        if (!thirdPersonCamera)
        {
            GameObject playerCameraObj = Instantiate(thirdPersonCameraPrefabObj);
            thirdPersonCamera = playerCameraObj.GetComponentInChildren<PlayerCamera>();
            thirdPersonCamera.SetFollowTarget(localPlayerType.freeLookTargetTransform);

            Camera UIOverlayCamera = playerHUD.mainCanvas.worldCamera;
            thirdPersonCamera.AddCameraToStack(UIOverlayCamera);
            thirdPersonCamera.SetState(true);
        }

        InitializeServerRpc(GameManager.Instance?.usingSteam == true ? SteamClient.SteamId.Value : 0);
        isInitialized = true;
    }

    // InitializeServerRpc is called on the server after InitializeOwner is finished.
    // This is to set up any server-authoritative parts of the player object.
    [Rpc(SendTo.Server)]
    private void InitializeServerRpc(ulong steamId)
    {
        identification.SetEntityName($"Player {OwnerClientId}");
        identification.SetEntityId(OwnerClientId);
        identification.SetTeamId((uint)OwnerClientId);

        // Get Player's Steam ID
        if (GameManager.Instance?.usingSteam == true) identification.SetEntityName(new Friend(steamId).Name);

        playerLoadout.Initialize(true, this);
        PostInitializeOwnerRpc();
        isInitialized = true;
        GameManager.Instance.OnClientConnectedEvent.Invoke(OwnerClientId);
    }

    // PostInitializeOwnerRpc is called on the local client after InitializeServerRpc is finished.
    // This is to finalize any local-only configuration that depends on server-authoritative player data.
    [Rpc(SendTo.Owner)]
    private void PostInitializeOwnerRpc()
    {
        playerHUD.Initialize(this);
    }
    #endregion


    #region Cleanup
    // Called when a player disconnects.
    [Rpc(SendTo.Server)]
    public void DisconnectCleanupRpc()
    {
        playerLoadout.Deinitialize();
        DisconnectCleanupOwnerRpc();
        GameManager.Instance.OnClientDisconnectedEvent.Invoke(OwnerClientId);
        isInitialized = false;
    }

    // Called when a player disconnects, but only on the local client to clean up local-only objects like the camera and UI.
    [Rpc(SendTo.Owner)]
    private void DisconnectCleanupOwnerRpc()
    {
        // Disable the UI
        if (thirdPersonCamera)
        {
            Destroy(thirdPersonCamera.gameObject);
            thirdPersonCamera = null;
        }
        if (playerHUD)
        {
            Destroy(playerHUD.gameObject);
            playerHUD = null;
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
    // Wrapper function for CharacterMovement's Teleport function, which toggles player controls while being teleported
    // to prevent any unwanted movement.
    public void Teleport(Vector3 destination, Quaternion rotation = default)
    {
        if (!IsServer) return;

        playerInputs.SetPlayerControlsRpc(false);
        characterMovement.Teleport(destination, rotation);
        playerInputs.SetPlayerControlsRpc(true);
    }
    #endregion


    #region Collision
    // Called when a collision occurs, used to calculate fall/impact damage.
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
    // Used to populate Identifier UI element.
    public IdentifierData GetIdentifierData()
    {
        return new IdentifierData
        {
            color = IdentifierManager.TempTeamColors[identification.FetchTeamId()],
            topText = identification.FetchEntityName(),
            bottomText = $"{Mathf.CeilToInt(health.HealthPercentage * 100f)}%",
            isActive = health.CurrentHealth > 0,
            targetTransform = localPlayerType.FFIdentifierTargetTransform,
            isAlwaysVisible = false
        };
    }
    #endregion


    #region SceneManagement
    // This makes sure that the player-related objects that are not parented to the player object itself are preserved when changing scenes.
    private void ChangedActiveScene(Scene _, Scene next) => ChangedActiveSceneRpc(next.name);
    [Rpc(SendTo.Owner)]
    private void ChangedActiveSceneRpc(string sceneName)
    {
        if (GameManager.Instance?.debugMode == true) Debug.Log(GetType() + ": Changed active scene for " + name + " " + NetworkManager.Singleton.LocalClientId);
        if (playerPuppetObj) SceneManager.MoveGameObjectToScene(playerPuppetObj, SceneManager.GetSceneByName(sceneName));
        if (playerHUD) SceneManager.MoveGameObjectToScene(playerHUD.gameObject, SceneManager.GetSceneByName(sceneName));
        if (thirdPersonCamera) SceneManager.MoveGameObjectToScene(thirdPersonCamera.gameObject, SceneManager.GetSceneByName(sceneName));
    }
    #endregion
}
