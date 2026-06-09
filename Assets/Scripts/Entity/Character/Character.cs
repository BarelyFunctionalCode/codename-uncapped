using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(CharacterInputs))]
[RequireComponent(typeof(CharacterNetworkTransform))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterState))]
[RequireComponent(typeof(Identification))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Energy))]
[RequireComponent(typeof(CharacterLoadoutManager))]
[RequireComponent(typeof(DevVectorRenderer))]
[RequireComponent(typeof(PickupContainer))]
public class Character : Entity
{
    private NetworkVariable<bool> isPlayerCharacter = new();
    public bool IsPlayerCharacter => isPlayerCharacter.Value;

    // Debug
    private DevVectorRenderer devVectorRenderer;
    [HideInInspector] public CharacterTelemetry characterTelemetry;

    [Header("Prefabs")]
    [SerializeField] private GameObject characterPuppetPrefabObj;

    [Header("Main Components")]
    [HideInInspector] public CharacterInputs characterInputs;
    [HideInInspector] public GameObject characterPuppetObj;
    [HideInInspector] public CharacterMovement characterMovement;
    [HideInInspector] public CharacterLoadoutManager characterLoadout;

    [SerializeField] private LayerMask aimIgnoreLayers;
    public Vector3 characterAimPosition;

    // These 2 components are set to either the components on this object, or the components on the puppet object,
    // depending on whether this character is owned by the host or a client.
    public CharacterType localCharacterType;
    public Rigidbody localRb;

    [Header("State")]
    public bool isInitialized = false;


    #region Lifecycle
    private void Awake()
    {
        devVectorRenderer = GetComponent<DevVectorRenderer>();
        characterInputs = GetComponent<CharacterInputs>();
        characterMovement = GetComponent<CharacterMovement>();
        characterLoadout = GetComponent<CharacterLoadoutManager>();
    }

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SceneManager.activeSceneChanged += ChangedActiveScene;
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
        if (!isInitialized || !(IsServer || IsOwner) || state.IsDead) return;

        // Update player telemetry for debugging purposes
        if (characterTelemetry != null) characterTelemetry.Update();

        // Death plane check
        if (IsServer && localRb.position.y < -1000f) state.Die();

        // Character ground detection and physics material updates
        characterMovement.ProcessUpdate();

        if (IsOwner)
        {
            Ray ray;
            if (IsPlayerCharacter)
            {
                characterAimPosition = Camera.main.transform.position + Camera.main.transform.forward * 1000f;
                ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            }
            else
            {
                characterAimPosition = localCharacterType.cameraLookAtTarget.position + localCharacterType.cameraLookAtTarget.forward * 1000f;
                ray = new Ray(localCharacterType.cameraLookAtTarget.position, localCharacterType.cameraLookAtTarget.forward);
            }
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, ~aimIgnoreLayers))
                characterAimPosition = hitInfo.point;

            UpdateCharacterAimPositionRpc(characterAimPosition);
        }
    }
    [Rpc(SendTo.Server)]
    private void UpdateCharacterAimPositionRpc(Vector3 newAimPosition)
    {
        characterAimPosition = newAimPosition;
    }

    void LateUpdate()
    {
        if (!isInitialized || !(IsOwner || IsServer) || state.IsDead) return;

        // Handle camera pitch rotation on local client
        characterInputs.HandleCameraInput();
        
        // Additional visual effects based on player state
        localCharacterType.HandleExtraMotion(characterInputs.MovementDirection, characterInputs.IsSkiing, characterMovement.SurfaceNormal);
    }

    void FixedUpdate()
    {
        if (!isInitialized || !(IsServer || IsOwner) || state.IsDead) return;

        // First, we collect all of the inputs that go into moving the character, and create an input state
        characterInputs.HandleInputs();
        characterMovement.SetMovementInputs(
            characterInputs.MovementDirection,
            characterInputs.RotationInputX,
            characterInputs.IsJumping,
            characterInputs.IsSkiing,
            characterInputs.IsUpJetting,
            characterInputs.IsDownJetting
        );

        // Set animator values
        localCharacterType.UpdateAnimationData(
            characterInputs.MovementInput,
            localRb.linearVelocity,
            state.IsGrounded,
            characterInputs.IsSkiing,
            characterInputs.IsDownJetting,
            characterInputs.IsUpJetting,
            characterInputs.IsJumping
        );
        // Set audio values
        localCharacterType.HandleAudio(localRb.linearVelocity, characterInputs.IsSkiing);

        // Finally, we process the inputs to move the character locally and on the server
        characterMovement.ProcessFixedUpdate();

        // This makes the client's local rotation authoritative
        if (!IsHost && IsOwner && IsPlayerCharacter) ClientAuthorityRotationSyncRpc(localRb.rotation);

        characterInputs.ResetJumpInput();
    }

    [Rpc(SendTo.Server)]
    private void ClientAuthorityRotationSyncRpc(Quaternion ownerRotation)
    {
        localRb.rotation = Quaternion.RotateTowards(localRb.rotation, ownerRotation, characterInputs.horizontalRotationLimit);
    }
    #endregion


    #region Initialization
    public void Initialize(GameObject defaultCharacterTypePrefabObj, ulong characterId, bool isPlayerCharacter = true)
    {
        this.isPlayerCharacter.Value = isPlayerCharacter;
        
        if (IsPlayerCharacter && GameManager.Instance.usingSteam == true) identification.SetEntityName(new Friend(characterId).Name);
        else identification.SetEntityName($"Character {characterId}");
        identification.SetEntityId(characterId);

        SetCharacterType(defaultCharacterTypePrefabObj);
    }

    // The first thing that happens when the character spawns is that we spawn their CharacterType object,
    // which contains all of the visual and animation data for the character. CharacterType is a separate Network Object
    // that is spawned as a child of the Character, and the Character holds a reference to it.
    // This separation allows us to easily swap out the character's model and animations by simply despawning the
    // current CharacterType and spawning a new one.
    private void SetCharacterType(GameObject characterTypePrefabObj)
    {
        if (!IsServer) return;
        if (localCharacterType != null)
        {
            Destroy(localCharacterType.gameObject);
            localCharacterType = null;
        }
        SpawnManager.Instance.Spawn(
            characterTypePrefabObj,
            false,
            transform.position,
            transform.rotation,
            transform,
            OwnerClientId
        );
    }

    // This is called by the newly spawned CharacterType object after it finishes initializing itself.
    public void OnCharacterTypeObjectSpawned(CharacterType characterType)
    {
        localCharacterType = characterType;
        localRb.mass = characterType.mass;
        characterMovement.UpdateCharacterData(characterType.characterCollider, null);
        GetComponent<PickupContainer>().pickupHoldPoint = characterType.pickupContainerHoldPoint;

        if (IsOwner) InitializeOwner();
    }

    // InitializeOwner is called once on the local client after the character's CharacterType object is spawned and initialized.
    // This is to set up any local-only parts of the character object, like the camera and UI.
    public void InitializeOwner()
    {
        if (!IsOwner) return;

        // Initialize Character Puppet for local client prediction of character movement before server updates are received
        if (!IsHost && IsPlayerCharacter && !characterPuppetObj)
        {
            // Spawn a non-authoritative puppet on local client for predicting the character's position and rotation before the server updates it
            characterPuppetObj = Instantiate(characterPuppetPrefabObj, localCharacterType.transform.position, localCharacterType.transform.rotation);
            characterPuppetObj.GetComponent<CharacterPuppet>().Initialize(this);
            return;
        }

        InitializeServerRpc();
        isInitialized = true;
    }

    // InitializeServerRpc is called on the server after InitializeOwner is finished.
    // This is to set up any server-authoritative parts of the character object.
    [Rpc(SendTo.Server)]
    private void InitializeServerRpc()
    {
        characterLoadout.Initialize(true, this);
        PostInitializeOwnerRpc();
        isInitialized = true;
    }

    // PostInitializeOwnerRpc is called on the local client after InitializeServerRpc is finished.
    // This is to finalize any local-only configuration that depends on server-authoritative character data.
    [Rpc(SendTo.Owner)]
    private void PostInitializeOwnerRpc()
    {
        isInitialized = true;
        characterTelemetry = new CharacterTelemetry(devVectorRenderer, this);
        if (IsPlayerCharacter) Player.Instance.Initialize(this);
        else AI.List[identification.FetchEntityId()].Initialize();
    }
    #endregion

    #region Movement
    // Wrapper function for CharacterMovement's Teleport function, which toggles player controls while being teleported
    // to prevent any unwanted movement.
    public void Teleport(Vector3 destination, Quaternion rotation = default)
    {
        if (!IsServer) return;

        characterInputs.SetCharacterControlsRpc(false);
        characterMovement.Teleport(destination, rotation);
        characterInputs.SetCharacterControlsRpc(true);
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


    #region SceneManagement
    // This makes sure that the player-related objects that are not parented to the player object itself are preserved when changing scenes.
    private void ChangedActiveScene(Scene _, Scene next) => ChangedActiveSceneRpc(next.name);
    [Rpc(SendTo.Owner)]
    private void ChangedActiveSceneRpc(string sceneName)
    {
        if (GameManager.Instance?.debugMode == true) Debug.Log(GetType() + ": Changed active scene for " + name + " " + NetworkManager.Singleton.LocalClientId);
        if (characterPuppetObj) SceneManager.MoveGameObjectToScene(characterPuppetObj, SceneManager.GetSceneByName(sceneName));
    }
    #endregion
}
