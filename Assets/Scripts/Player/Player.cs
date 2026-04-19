using Steamworks;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, PlayerControls.ICharacterActions
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

        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Initialize(Character character)
    {
        Debug.Log("Initializing player.");
        playerControls = new PlayerControls();
        Character = character;
        playerHUD.Initialize(this, character);
        thirdPersonCamera.SetFollowTarget(character.localCharacterType.cameraLookAtTarget);
        firstPersonCamera.SetLookAtTarget(character.localCharacterType.cameraLookAtTarget);
        firstPersonCamera.SetFollowTarget(character.localCharacterType.firstPersonCameraFollowTarget);
        thirdPersonCamera.SetState(true);
        RegisterCharacterInputs();
    }

    public void Deinitialize()
    {
        Debug.Log("Deinitializing player.");
        playerHUD.Deinitialize();
        UnregisterCharacterInputs();
        playerControls.Dispose();
        Character = null;
    }

    public void EnableControls() => playerControls?.Enable();
    public void DisableControls() => playerControls?.Disable();

    private void RegisterCharacterInputs()
    {
        if (playerControls == null || Character == null) return;

        playerControls.Character.SetCallbacks(this);
        playerControls.Enable();
    }

    private void UnregisterCharacterInputs()
    {
        playerControls.Disable();
        playerControls.Character.RemoveCallbacks(this);
    }

    private void ToggleCameraView()
    {
        bool isThirdPerson = thirdPersonCamera.IsEnabled;
        thirdPersonCamera.SetState(!isThirdPerson);
        firstPersonCamera.SetState(isThirdPerson);
    }



    #region Input Callbacks
    public void OnLook(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            Vector2 lookInput = context.ReadValue<Vector2>();
            Character.characterInputs.LookInput(lookInput);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) Character.characterInputs.MoveInput(context.ReadValue<Vector2>());
    }

    public void OnPrimaryFire(InputAction.CallbackContext context)
    {
        if (context.started) Character.characterLoadout.OnPrimaryFireStartedRpc();
        else if (context.canceled) Character.characterLoadout.OnPrimaryFireCanceledRpc();
    }

    public void OnThrowable(InputAction.CallbackContext context)
    {
        if (context.started) Character.characterInputs.ThrowableStarted();
        else if (context.canceled) Character.characterInputs.ThrowableReleased();
    }

    public void OnActivateDrive(InputAction.CallbackContext context)
    {
        if (context.started) Character.characterLoadout.ActivateDriveRpc();
    }

    public void OnPreviousWeapon(InputAction.CallbackContext context)
    {
        if (context.started) Character.characterLoadout.PreviousWeaponRpc();
    }

    public void OnNextWeapon(InputAction.CallbackContext context)
    {
        if (context.started) Character.characterLoadout.NextWeaponRpc();
    }

    public void OnSki(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) Character.characterInputs.SkiInput(context.ReadValue<float>());
    }

    public void OnJumpJet(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) Character.characterInputs.JetInput(context.ReadValue<float>());
    }

    public void OnDownJet(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled) Character.characterInputs.DownJetInput(context.ReadValue<float>());
    }

    public void OnToggleCameraView(InputAction.CallbackContext context)
    {
        if (context.started) ToggleCameraView();
    }
    #endregion
}
