using Unity.Netcode;
using UnityEngine;

public class PlayerInputs : EntityComponent
{
    private PlayerController playerController;
    private PlayerLoadoutManager playerLoadout;
    
    public PlayerControls playerControls;
    [PauseMenuOption("Horizontal Look", 0f, 100f)]
    public float horizontalRotationSpeed = 20f;
    public readonly float horizontalRotationLimit = 100f;
    public float RotationInputX { get; private set; } = 0f;
    public float RotationInputY { get; private set; } = 0f;
    public int ControlsDisabledCount { get; private set; } = 0;
    public Vector3 MovementInput { get; private set; } = Vector3.zero;
    public Vector3 MovementDirection { get; private set; } = Vector3.zero;
    public bool IsJumping { get; private set; } = false;
    public bool IsSkiing { get; private set; } = false;
    public bool IsUpJetting { get; private set; } = false;
    public bool IsDownJetting { get; private set; } = false;

    Vector3 newMovementDirection = Vector3.zero;
    float rawRotationInputX = 0f;
    bool upJettingInput = false;
    bool downJettingInput = false;

    private void Awake()
    {
        playerLoadout = GetComponent<PlayerLoadoutManager>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector3 newMovementDirection = playerController.localTransform.TransformDirection(MovementInput); // NOT SUPPOSED TO BE NORMALIZED
            bool changed = false;
            if (newMovementDirection != this.newMovementDirection) changed = true;
            this.newMovementDirection = newMovementDirection;
            if (changed) MoveDirectionRpc(newMovementDirection);
        }
    }

    public override void Initialize(Entity entity)
    {
        base.Initialize(entity);

        playerController = entity.GetComponent<PlayerController>();

        if (!IsLocalPlayer) return;

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
        playerControls.Character.Throwable.started += ctx => ThrowableStarted();
        playerControls.Character.Throwable.canceled += ctx => ThrowableReleased();
        playerControls.Character.ActivateDrive.started += ctx => playerLoadout.ActivateDriveRpc();
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

    public override void Deinitialize()
    {
        base.Deinitialize();
        
        if (!IsLocalPlayer) return;

        playerControls.Disable();
        playerControls.Character.Move.performed -= ctx => MoveInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Move.canceled -= ctx => MoveInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Look.performed -= ctx => LookInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Look.canceled -= ctx => LookInput(ctx.ReadValue<Vector2>());
        playerControls.Character.PrimaryFire.started -= ctx => playerLoadout.OnPrimaryFireStartedRpc();
        playerControls.Character.PrimaryFire.canceled -= ctx => playerLoadout.OnPrimaryFireCanceledRpc();
        playerControls.Character.Throwable.started -= ctx => ThrowableStarted();
        playerControls.Character.Throwable.canceled -= ctx => ThrowableReleased();
        playerControls.Character.ActivateDrive.started -= ctx => playerLoadout.ActivateDriveRpc();
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
    }

    [Rpc(SendTo.Server)]
    private void MoveDirectionRpc(Vector3 newMovementDirection)
    {
        this.newMovementDirection = newMovementDirection;
    }

    private void ToggleCameraView()
    {
        bool isThirdPerson = playerController.thirdPersonCamera.IsEnabled;
        playerController.thirdPersonCamera.SetState(!isThirdPerson);
        playerController.localPlayerType.firstPersonCamera.SetState(isThirdPerson);
    }

    [Rpc(SendTo.Server)]
    public void SetPlayerControlsRpc(bool enabled)
    {
        ControlsDisabledCount += enabled ? -1 : 1;
        ControlsDisabledCount = Mathf.Max(0, ControlsDisabledCount);
        SetPlayerControlsOwnerRpc(ControlsDisabledCount == 0);
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
        playerController.playerHUD.SetHUDActive(enabled);
    }
    [Rpc(SendTo.Owner)]
    public void OpenLoadoutMenuRpc()
    {
        playerController.playerHUD.ToggleMenu(HUDMenu.LoadoutMenu, true);
    }
    [Rpc(SendTo.Owner)]
    public void SetCursorStateRpc(bool enabled, bool usingCustomCursor = false)
    {
        playerController.playerHUD.SetCursorState(enabled, usingCustomCursor);
    }

    private void ThrowableStarted()
    {
        if (playerController.pickupContainer.CurrentlyHeldPickup != null)
        {
            playerController.pickupContainer.StartPutDownRpc();
        }
        else
        {
            playerLoadout.OnThrowableStartedRpc();
        }
    }

    private void ThrowableReleased()
    {
        if (playerController.pickupContainer.CurrentlyHeldPickup != null)
        {
            Vector3 throwDirection = playerController.localPlayerType.freeLookTargetTransform.forward;
            playerController.pickupContainer.TryPutDownRpc(throwDirection);
        }
        playerLoadout.OnThrowableCanceledRpc();
    }

    private void MoveInput(Vector2 rawMovementInput)
    {
        MovementInput = new(rawMovementInput.x, 0f, rawMovementInput.y);
        MoveInputRpc(MovementInput);
    }
    [Rpc(SendTo.Server)]
    private void MoveInputRpc(Vector3 movementInput)
    {
        this.MovementInput = movementInput;
    }
    private void LookInput(Vector2 lookInput)
    {
        rawRotationInputX += lookInput.x;
        RotationInputY -= lookInput.y;
        LookInputRpc(lookInput);
    }
    [Rpc(SendTo.Server)]
    private void LookInputRpc(Vector2 lookInput)
    {
        rawRotationInputX += lookInput.x;
        RotationInputY -= lookInput.y;
    }
    private void SkiInput(float skiInput)
    {
        IsSkiing = skiInput > 0.0f;
        SkiInputRpc(skiInput);
    }
    [Rpc(SendTo.Server)]
    private void SkiInputRpc(float skiInput)
    {
        IsSkiing = skiInput > 0.0f;
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
        IsJumping = true;
        JumpInputRpc();
    }
    [Rpc(SendTo.Server)]
    private void JumpInputRpc()
    {
        IsJumping = true;
    }

    public void ResetJumpInput()
    {
        IsJumping = false;
    }

    public void HandleCameraInput()
    {
        // Handle camera pitch rotation on local client
        playerController.localPlayerType.HandleCamera(RotationInputY, ControlsDisabledCount);
        RotationInputY = 0f;
    }

    public void HandleInputs()
    {
        // Get rotation input
        float rotationYaw = rawRotationInputX;
        rawRotationInputX = 0f;
        rotationYaw *= horizontalRotationSpeed * Time.fixedDeltaTime;
        RotationInputX = Mathf.Clamp(rotationYaw, -horizontalRotationLimit, horizontalRotationLimit);

        // Get direction of movement relative to player rotation
        MovementDirection = newMovementDirection;

        // Get input for skiing, jumping, and down jetting
        IsUpJetting = upJettingInput && IsSkiing;
        IsDownJetting = downJettingInput && IsSkiing;

        if (ControlsDisabledCount > 0)
        {
            MovementDirection = Vector3.zero;
            IsSkiing = false;
            IsUpJetting = false;
            IsDownJetting = false;
        }
    }
}
