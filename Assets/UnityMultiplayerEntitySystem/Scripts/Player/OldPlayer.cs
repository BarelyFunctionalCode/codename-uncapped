using Steamworks;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public struct inputStateOld
{
    public double timestamp;
    public Vector2 input;
    public Quaternion lookDirection;
}

public class OldPlayer : NetworkBehaviour
{

    #region ClassVariables
    [Header("Player Class")]
    private SteamId localId;
    public SteamId PlayerSteamId { get { return localId; } }

    // Controls
    public PlayerControls controls;
    private Vector2 moveInput;
    private List<inputStateOld> inputBuffer = new();
    private AnticipatedNetworkTransform anticipatedNetworkTransform;

    // Camera
    [SerializeField] private GameObject playerCameraPrefabObj;
    private GameObject playerCameraObj;
    private CinemachineCamera cineCam;
    [SerializeField] private Transform freeLookTargetObj;
    private AudioListener audioListener;

    // UI
    [SerializeField] private GameObject playerUIPrefabObj;
    public GameObject playerUIObj;
    // [SerializeField] private UIController ui;
    // public UIController UI { get { return ui; } }

    // Animation
    [SerializeField] private Animator animator;
    private float previousSpeed = 0;

    public bool isInitialized = false;
    #endregion

    #region Lifecycle
    private void Awake()
    {

        // playerStats = stats as PlayerStats;

        anticipatedNetworkTransform = GetComponent<AnticipatedNetworkTransform>();
        anticipatedNetworkTransform.StaleDataHandling = StaleDataHandling.Reanticipate;
    }

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SceneManager.activeSceneChanged += ChangedActiveScene;

        if (IsOwner)
        {
            // Capture the mouse cursor
            Cursor.lockState = CursorLockMode.Locked;

            // Set up the player controls
            controls = new PlayerControls();

            // Set up the input callbacks
            // controls.Gameplay.Move.performed += ctx => Move(ctx.ReadValue<Vector2>());
            // controls.Gameplay.Move.canceled += ctx => Move(ctx.ReadValue<Vector2>());
            // controls.Gameplay.SwitchHands.performed += ctx => SwitchHands((int)ctx.ReadValue<float>());
            // controls.Gameplay.Use.performed += ctx => UseRpc();
            // controls.Gameplay.Drop.started += ctx => StartDropRpc();
            // controls.Gameplay.Drop.canceled += ctx => FinishDrop();
            // controls.Global.Exit.performed += ctx => ExitPressedRpc();

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
            // I was getting random errors in the editor for a null value, so this is a quick fix
            // controls.Gameplay.Disable();
            // controls.Global.Disable();

            // Disable audio listener
            if (audioListener) audioListener.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isInitialized) return;


        Vector2 adjustedInput = Vector2.zero;
        Quaternion lookDirection = Quaternion.identity;
        // TODO: Process raw movement inputs here

        if (!IsServer)
        {
            inputBuffer.Add(new inputStateOld { timestamp = NetworkManager.LocalTime.Time, input = adjustedInput, lookDirection = lookDirection });
            HandleMovement(adjustedInput, lookDirection); // Client side prediction
        }
        HandleMovementRpc(adjustedInput, lookDirection);

        UpdateAnimatorVelocityRpc();
    }

    [Rpc(SendTo.Server)]
    private void UpdateAnimatorVelocityRpc()
    {
        // Report speed
        float speed = 0f; // TODO: Calculate speed based rigidbody
        previousSpeed = speed;

        if (animator != null) animator.SetFloat("Velocity", speed);
    }
    #endregion

    #region Initialization
    public void Initialize()
    {
        if (IsOwner)
        {
            // If the player is spawned, enable the inputs
            // controls.Gameplay.Enable();
            // controls.Global.Enable();

            if (!playerUIObj) playerUIObj = Instantiate(playerUIPrefabObj);

            if (!playerCameraObj)
            {
                playerCameraObj = Instantiate(playerCameraPrefabObj);
                audioListener = playerCameraObj.GetComponentInChildren<AudioListener>();
                cineCam = playerCameraObj.GetComponentInChildren<CinemachineCamera>();
                cineCam.Follow = freeLookTargetObj;

                // Enable audio listener
                audioListener.enabled = true;

                // Enable the camera
                cineCam.Priority.Value = 1;
            }

            // if (FindFirstObjectByType<DevNetworkManager>() != null) UI.ToggleLevelUI(true);

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
        // PlayerManager.Instance.AddPlayer(this);
        // playerStats.Initialize(OwnerClientId);
        // playerStats.SetInventory();
    }
    #endregion

    #region Cleanup
    [Rpc(SendTo.Server)]
    public void DisconnectCleanupRpc()
    {
        DisconnectCleanupOwnerRpc();
    }

    [Rpc(SendTo.Owner)]
    private void DisconnectCleanupOwnerRpc()
    {
        // Disable the UI
        // ui.Reset();
    }
    #endregion

    #region Inputs

    private void Move(Vector2 direction) { moveInput = direction; }
    #endregion


    #region Movement
    [Rpc(SendTo.Server)]
    private void HandleMovementRpc(Vector2 movementInput, Quaternion lookDirection)
    {
        HandleMovement(movementInput, lookDirection);
    }

    private void HandleMovement(Vector2 movementInput, Quaternion lookDirection)
    {
        Quaternion newRotation = transform.rotation;

        // TODO: Do movement things here that update the player transform
        anticipatedNetworkTransform.AnticipateState(new AnticipatedNetworkTransform.TransformState
        {
            Position = transform.position, // TODO: I'm not sure if this has the new position or the old position
            Rotation = newRotation,
            Scale = transform.localScale
        });
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

                HandleMovement(input.input, input.lookDirection);

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
        // else if (sqDist < 3f * 3f)
        // {
        //     // Server updates are not necessarily smooth, so applying reanticipation can also result in
        //     // hitchy, unsmooth animations. To compensate for that, we call this to smooth from the previous
        //     // anticipated state (stored in "anticipatedValue") to the new state (which, because we have used
        //     // the "Move" method that updates the anticipated state of the transform, is now the current
        //     // transform anticipated state)
        //     anticipatedNetworkTransform.Smooth(previousState, anticipatedNetworkTransform.AnticipatedState, 0.1f);
        //     Physics.SyncTransforms();
        // }
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
        if (GameManager.Instance?.debugMode == true) Debug.Log("Changed active scene for " + name + " " + NetworkManager.Singleton.LocalClientId);
        if (playerUIObj) SceneManager.MoveGameObjectToScene(playerUIObj, SceneManager.GetSceneByName(sceneName));
        if (playerCameraObj) SceneManager.MoveGameObjectToScene(playerCameraObj, SceneManager.GetSceneByName(sceneName));

        if (sceneName == "Lobby") Initialize();
    }
    #endregion
}
