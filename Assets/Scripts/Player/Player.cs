using Steamworks;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; } = null;

    public ulong playerId;
    public PlayerControls playerControls;
    public PlayerCamera thirdPersonCamera;
    public PlayerCamera firstPersonCamera;
    public HUD playerHUD;

    public Character Character { get; private set; }

    void Awake()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        
        try
        {
            playerId = SteamClient.SteamId.Value;
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Not running with Steam client, assigning random player ID.");
            playerId = (ulong)Random.Range(1, int.MaxValue); // Assign a default value or handle as needed
        }
        playerControls = new PlayerControls();

        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Initialize(Character character)
    {
        Character = character;
        playerHUD.Initialize(this, character);
        thirdPersonCamera.SetFollowTarget(character.localCharacterType.cameraLookAtTarget);
        firstPersonCamera.SetLookAtTarget(character.localCharacterType.cameraLookAtTarget);
        firstPersonCamera.SetFollowTarget(character.localCharacterType.firstPersonCameraFollowTarget);
        thirdPersonCamera.SetState(true);
        RegisterCharacterInputs();
    }

    public void EnableControls() => playerControls.Enable();
    public void DisableControls() => playerControls.Disable();

    private void RegisterCharacterInputs()
    {
        CharacterInputs playerInputs = Character.characterInputs;
        CharacterLoadoutManager playerLoadout = Character.characterLoadout;

        playerControls.Enable();
        playerControls.Character.Move.performed += ctx => playerInputs.MoveInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Move.canceled += ctx => playerInputs.MoveInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Look.performed += ctx => playerInputs.LookInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Look.canceled += ctx => playerInputs.LookInput(ctx.ReadValue<Vector2>());
        playerControls.Character.PrimaryFire.started += ctx => playerLoadout.OnPrimaryFireStartedRpc();
        playerControls.Character.PrimaryFire.canceled += ctx => playerLoadout.OnPrimaryFireCanceledRpc();
        playerControls.Character.Throwable.started += ctx => playerInputs.ThrowableStarted();
        playerControls.Character.Throwable.canceled += ctx => playerInputs.ThrowableReleased();
        playerControls.Character.ActivateDrive.started += ctx => playerLoadout.ActivateDriveRpc();
        playerControls.Character.NextWeapon.started += ctx => playerLoadout.NextWeaponRpc();
        playerControls.Character.PreviousWeapon.started += ctx => playerLoadout.PreviousWeaponRpc();
        playerControls.Character.Ski.performed += ctx => playerInputs.SkiInput(ctx.ReadValue<float>());
        playerControls.Character.Ski.canceled += ctx => playerInputs.SkiInput(ctx.ReadValue<float>());
        playerControls.Character.JumpJet.performed += ctx => playerInputs.JetInput(ctx.ReadValue<float>());
        playerControls.Character.JumpJet.canceled += ctx => playerInputs.JetInput(ctx.ReadValue<float>());
        playerControls.Character.DownJet.performed += ctx => playerInputs.DownJetInput(ctx.ReadValue<float>());
        playerControls.Character.DownJet.canceled += ctx => playerInputs.DownJetInput(ctx.ReadValue<float>());
        playerControls.Character.JumpJet.started += ctx => playerInputs.JumpInput();
        playerControls.Character.ToggleCameraView.started += ctx => ToggleCameraView();
    }

    private void UnregisterCharacterInputs()
    {
        CharacterInputs playerInputs = Character.characterInputs;
        CharacterLoadoutManager playerLoadout = Character.characterLoadout;

        playerControls.Character.Move.performed -= ctx => playerInputs.MoveInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Move.canceled -= ctx => playerInputs.MoveInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Look.performed -= ctx => playerInputs.LookInput(ctx.ReadValue<Vector2>());
        playerControls.Character.Look.canceled -= ctx => playerInputs.LookInput(ctx.ReadValue<Vector2>());
        playerControls.Character.PrimaryFire.started -= ctx => playerLoadout.OnPrimaryFireStartedRpc();
        playerControls.Character.PrimaryFire.canceled -= ctx => playerLoadout.OnPrimaryFireCanceledRpc();
        playerControls.Character.Throwable.started -= ctx => playerInputs.ThrowableStarted();
        playerControls.Character.Throwable.canceled -= ctx => playerInputs.ThrowableReleased();
        playerControls.Character.ActivateDrive.started -= ctx => playerLoadout.ActivateDriveRpc();
        playerControls.Character.NextWeapon.started -= ctx => playerLoadout.NextWeaponRpc();
        playerControls.Character.PreviousWeapon.started -= ctx => playerLoadout.PreviousWeaponRpc();
        playerControls.Character.Ski.performed -= ctx => playerInputs.SkiInput(ctx.ReadValue<float>());
        playerControls.Character.Ski.canceled -= ctx => playerInputs.SkiInput(ctx.ReadValue<float>());
        playerControls.Character.JumpJet.performed -= ctx => playerInputs.JetInput(ctx.ReadValue<float>());
        playerControls.Character.JumpJet.canceled -= ctx => playerInputs.JetInput(ctx.ReadValue<float>());
        playerControls.Character.DownJet.performed -= ctx => playerInputs.DownJetInput(ctx.ReadValue<float>());
        playerControls.Character.DownJet.canceled -= ctx => playerInputs.DownJetInput(ctx.ReadValue<float>());
        playerControls.Character.JumpJet.started -= ctx => playerInputs.JumpInput();
        playerControls.Character.ToggleCameraView.started -= ctx => ToggleCameraView();

        playerControls.Disable();
    }

    private void ToggleCameraView()
    {
        bool isThirdPerson = thirdPersonCamera.IsEnabled;
        thirdPersonCamera.SetState(!isThirdPerson);
        firstPersonCamera.SetState(isThirdPerson);
    }
}
