using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public enum HUDMenu
{
    None,
    LoadoutMenu,
    PauseMenu
}

public class HUD : MonoBehaviour
{
    [SerializeField] private CenterClusterUI centerClusterUI;
    [SerializeField] private Transform weaponsContainer;
    [SerializeField] private GameObject weaponUIPrefabObj;
    [SerializeField] private ThrowableUI throwableUI;
    [SerializeField] private Transform gearContainer;
    [SerializeField] private RectTransform dynamicReticle;

    private PlayerController playerController;
    private PlayerControls playerControls;
    private List<HUDMenu> openMenus = new();
    [SerializeField] public LoadoutMenu loadoutMenu;
    [SerializeField] private PauseMenu pauseMenu;

    private float dynamicReticleMaxMoveRange = 50f;
    private float dynamicReticleMaxVelocityDeflection = 50f;

    private bool menuLock = false;

    private bool isInitialized = false;

    private void Update()
    {
        if (!isInitialized) return;

        // Update dynamic reticle position based on player velocity
        if (playerController == null || playerController.localRb == null) return;
        Vector3 velocity = playerController.transform.InverseTransformVector(playerController.localRb.linearVelocity);
        float deflectionX = Mathf.Clamp(-velocity.x, -dynamicReticleMaxVelocityDeflection, dynamicReticleMaxVelocityDeflection);
        float deflectionY = Mathf.Clamp(-velocity.y, -dynamicReticleMaxVelocityDeflection, dynamicReticleMaxVelocityDeflection);
        Vector2 dynamicReticleTargetPos = new Vector2(deflectionX / dynamicReticleMaxVelocityDeflection * dynamicReticleMaxMoveRange,
                                                      deflectionY / dynamicReticleMaxVelocityDeflection * dynamicReticleMaxMoveRange);
        dynamicReticle.anchoredPosition = Vector2.Lerp(dynamicReticle.anchoredPosition, dynamicReticleTargetPos, Time.deltaTime * 10f);
    }

    public void ToggleHUD()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = !canvas.enabled;
        }
    }

    public void Initialize(PlayerController playerController)
    {
        if (isInitialized) return;

        this.playerController = playerController;
        playerControls = playerController.playerControls;
        playerControls.UI.PauseMenu.performed += ctx => ToggleMenu(HUDMenu.PauseMenu);
        playerControls.UI.LoadoutMenu.performed += ctx => ToggleMenu(HUDMenu.LoadoutMenu);
        playerControls.UI.Chat.performed += ctx => EnableChatInput();
        playerControls.UI.Close.performed += ctx => EscapePressed();

        InitializePauseMenuElements(playerController);
        centerClusterUI.Initialize(playerController);
        loadoutMenu.Initialize(playerController.GetComponent<PlayerLoadoutManager>(), this);

        isInitialized = true;
    }

    public void AddWeaponUI(Weapon weapon)
    {
        GameObject weaponUIObj = Instantiate(weaponUIPrefabObj, weaponsContainer);
        WeaponUI weaponUI = weaponUIObj.GetComponent<WeaponUI>();
        weaponUI.Initialize(weapon);
    }

    public void SetThrowableUI(ThrowableManager throwableManager)
    {
        throwableUI.Initialize(throwableManager);
    }

    private void InitializePauseMenuElements(PlayerController playerController)
    {
        // Initialize player options in pause menu
        FieldInfo[] fields = playerController.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuOptionAttribute[] attribute = (PauseMenuOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuOptionAttribute), true);

            if (attribute.Length > 0)
            {
                if (!PauseMenu.Instance.devMode && attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute)) continue;
                PauseMenu.Instance.AddOption(
                    attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute) ? "dev - " + attribute[0].label : attribute[0].label,
                    (float)field.GetValue(playerController),
                    attribute[0].minValue,
                    attribute[0].maxValue,
                    (float value) => { field.SetValue(playerController, value); }
                );
            }
        }

        // Initialize player controls in pause menu
        List<string> controlIgnoreList = new List<string> { "Pause","Move", "Look" };
        // InputActionMap movementMap = playerControls.Movement;
        foreach (var actionMap in playerControls.asset.actionMaps)
        {
            foreach (var action in actionMap)
            {
                if (controlIgnoreList.Contains(action.name)) continue;
                PauseMenu.Instance.AddControl(action);
            }
        }

        // Initialize player debug settings in pause menu
        if (!PauseMenu.Instance.devMode) return;
        fields = playerController.playerTelemetry.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuDevOptionAttribute[] attribute = (PauseMenuDevOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuDevOptionAttribute), true);

            if (attribute.Length > 0)
            {
                PauseMenu.Instance.AddDebug(
                    field.Name,
                    attribute[0].label,
                    (bool)field.GetValue(playerController.playerTelemetry),
                    value => { field.SetValue(playerController.playerTelemetry, value); }
                );
            }
        }
    }

    public void EscapePressed()
    {
        if (ChatManager.Instance.isChatInputActive) // Prioritize closing chat input if it's open
        {
            ChatManager.Instance.ToggleChatInput(false);
            menuLock = false;
            return;
        }
        ToggleMenu(openMenus[^1]);
    }

    private void EnableChatInput()
    {
        if (openMenus.Count > 0) return; // Don't open chat if any menu is open
        ChatManager.Instance.ToggleChatInput(true);
        menuLock = true;
    } 

    public void ToggleMenu(HUDMenu menu, bool forceOpen = false)
    {
        if (menuLock) return;
        if (forceOpen && openMenus.Contains(menu)) return;

        switch (menu)
        {
            case HUDMenu.LoadoutMenu:
                loadoutMenu.ToggleMenu();
                break;
            case HUDMenu.PauseMenu:
                pauseMenu.ToggleMenu();
                break;
            default:
                break;
        }

        if (openMenus.Contains(menu))
        {
            openMenus.Remove(menu);
            if (openMenus.Count == 0) playerController.SetPlayerControlsRpc(true);
        }
        else
        {
            if (openMenus.Count == 0) playerController.SetPlayerControlsRpc(false);
            openMenus.Add(menu);
        }

        Cursor.lockState = openMenus.Count > 0 ? CursorLockMode.Confined : CursorLockMode.Locked;
    }
}
