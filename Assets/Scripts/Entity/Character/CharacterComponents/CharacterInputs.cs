using Unity.Netcode;
using UnityEngine;

public class CharacterInputs : EntityComponent
{
    private Character character;
    private CharacterLoadoutManager characterLoadout;
    
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
        characterLoadout = GetComponent<CharacterLoadoutManager>();
        character = GetComponent<Character>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector3 newMovementDirection = character.localTransform.TransformDirection(MovementInput); // NOT SUPPOSED TO BE NORMALIZED
            bool changed = false;
            if (newMovementDirection != this.newMovementDirection) changed = true;
            this.newMovementDirection = newMovementDirection;
            if (changed) MoveDirectionRpc(newMovementDirection);
        }
    }
    
    [Rpc(SendTo.Server)]
    private void MoveDirectionRpc(Vector3 newMovementDirection)
    {
        this.newMovementDirection = newMovementDirection;
    }

    [Rpc(SendTo.Server)]
    public void SetCharacterControlsRpc(bool enabled)
    {
        ControlsDisabledCount += enabled ? -1 : 1;
        ControlsDisabledCount = Mathf.Max(0, ControlsDisabledCount);
        if (!character.isAI.Value) SetLocalPlayerCharacterControlsRpc(ControlsDisabledCount == 0);
        // else Whatever way to disable AI controls
    }
    [Rpc(SendTo.Owner)]
    private void SetLocalPlayerCharacterControlsRpc(bool enabled)
    {
        if (enabled) Player.Instance.EnableControls();
        else Player.Instance.DisableControls();
    }

    [Rpc(SendTo.Owner)]
    public void SetHUDActiveRpc(bool enabled)
    {
        Player.Instance.playerHUD.SetHUDActive(enabled);
    }

    public void ThrowableStarted()
    {
        if (character.pickupContainer.CurrentlyHeldPickup != null)
        {
            character.pickupContainer.StartPutDownRpc();
        }
        else
        {
            characterLoadout.OnThrowableStartedRpc();
        }
    }

    public void ThrowableReleased()
    {
        if (character.pickupContainer.CurrentlyHeldPickup != null)
        {
            Vector3 throwDirection = character.localCharacterType.cameraLookAtTarget.forward;
            character.pickupContainer.TryPutDownRpc(throwDirection);
        }
        characterLoadout.OnThrowableCanceledRpc();
    }

    public void MoveInput(Vector2 rawMovementInput)
    {
        MovementInput = new(rawMovementInput.x, 0f, rawMovementInput.y);
        MoveInputRpc(MovementInput);
    }
    [Rpc(SendTo.Server)]
    private void MoveInputRpc(Vector3 movementInput)
    {
        this.MovementInput = movementInput;
    }
    public void LookInput(Vector2 lookInput)
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
    public void SkiInput(float skiInput)
    {
        IsSkiing = skiInput > 0.0f;
        SkiInputRpc(skiInput);
    }
    [Rpc(SendTo.Server)]
    private void SkiInputRpc(float skiInput)
    {
        IsSkiing = skiInput > 0.0f;
    }
    public void JetInput(float jetInput)
    {
        upJettingInput = jetInput > 0.0f;
        JetInputRpc(jetInput);
    }
    [Rpc(SendTo.Server)]
    private void JetInputRpc(float jetInput)
    {
        upJettingInput = jetInput > 0.0f;
    }
    public void DownJetInput(float downJetInput)
    {
        downJettingInput = downJetInput > 0.0f;
        DownJetInputRpc(downJetInput);
    }
    [Rpc(SendTo.Server)]
    private void DownJetInputRpc(float downJetInput)
    {
        downJettingInput = downJetInput > 0.0f;
    }
    public void JumpInput()
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
        character.localCharacterType.HandleCamera(RotationInputY, ControlsDisabledCount);
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
