using System.Linq;
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
    public NetworkVariable<bool> isAI = new();

    // Debug
    private DevVectorRenderer devVectorRenderer;
    [HideInInspector] public CharacterTelemetry characterTelemetry;

    [Header("Main Components")]
    [HideInInspector] public CharacterInputs characterInputs;
    [HideInInspector] public CharacterMovement characterMovement;
    [HideInInspector] public CharacterLoadoutManager characterLoadout;
    public Transform localTransform;
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

        localTransform = transform;
        localRb = GetComponent<Rigidbody>();
        localRb.sleepThreshold = 0.0f;

        if (IsServer) isAI.Value = true;
    }

    private void Update()
    {
        if (!isInitialized || !(IsServer || IsOwner) || state.IsDead) return;

        // Update player telemetry for debugging purposes
        if (characterTelemetry != null) characterTelemetry.Update();

        // Death plane check
        if (IsServer && localTransform.position.y < -1000f) state.Die();

        // Character ground detection and physics material updates
        characterMovement.ProcessUpdate();
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
        if (IsOwner && !IsHost && !isAI.Value) ClientAuthorityRotationSyncRpc(localRb.rotation);

        characterInputs.ResetJumpInput();

        if (characterTelemetry != null)
        {
            characterTelemetry.movementDirection = characterInputs.MovementDirection;
            characterTelemetry.isSkiing = characterInputs.IsSkiing;
            characterTelemetry.isUpJetting = characterInputs.IsUpJetting;
            characterTelemetry.isDownJetting = characterInputs.IsDownJetting;
            characterTelemetry.position = localTransform.position;
            characterTelemetry.velocity = localRb.linearVelocity;
            characterTelemetry.distanceToSurface = characterMovement.DistanceToSurface;
            characterTelemetry.surfacePoint = characterMovement.SurfacePoint;
            characterTelemetry.isGrounded = state.IsGrounded;
            characterTelemetry.surfaceNormal = characterMovement.SurfaceNormal;
        }
    }

    [Rpc(SendTo.Server)]
    private void ClientAuthorityRotationSyncRpc(Quaternion ownerRotation)
    {
        localRb.rotation = Quaternion.RotateTowards(localRb.rotation, ownerRotation, characterInputs.horizontalRotationLimit);
    }
    #endregion


    #region Initialization
    public void Initialize(GameObject defaultCharacterTypePrefabObj, ulong characterId = 0)
    {
        if (characterId == 0) characterId = NetworkObjectId + 1;
        else isAI.Value = false;
        
        if (!isAI.Value && GameManager.Instance.usingSteam == true) identification.SetEntityName(new Friend(characterId).Name);
        else identification.SetEntityName($"Character {characterId}");
        identification.SetEntityId(characterId);
        identification.SetTeamId((uint)characterId);

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
        characterMovement.UpdateCharacterData(null, characterType.characterCollider, null);
        GetComponent<PickupContainer>().pickupHoldPoint = characterType.pickupContainerHoldPoint;

        if (IsOwner) InitializeServerRpc();
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
        characterTelemetry = new CharacterTelemetry(devVectorRenderer);
        if (Player.Instance != null) Player.Instance.Initialize(this);
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
}
