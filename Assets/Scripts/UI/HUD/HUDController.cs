using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HUDController : MonoBehaviour
{
    private UIDocument hudUIDocument;

    private HUDObjectiveContainer objectiveContainer;
    private HUDCenter centerContainer;
    private HUDLeftSide leftSideContainer;
    private HUDRightSide rightSideContainer;

    private ToastContainer killFeed;

    private Character character;
    private PlayerControls playerControls;

    private List<HUDMenu> openMenus = new();
    private HUDMenu menuLock = HUDMenu.None;
    int cursorLockCounter = 0;

    private bool isActive = false;
    private bool isInitialized = false;

    private void Awake()
    {
        hudUIDocument = GetComponent<UIDocument>();
        var hudRoot = hudUIDocument.rootVisualElement;
        objectiveContainer = hudRoot.Q<HUDObjectiveContainer>();
        objectiveContainer.LeftObjectiveNumber.Text = "00";
        objectiveContainer.RightObjectiveNumber.Text = "00";

        centerContainer = hudRoot.Q<HUDCenter>();

        leftSideContainer = hudRoot.Q<HUDLeftSide>();
        rightSideContainer = hudRoot.Q<HUDRightSide>();
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (character != null)
        {
            centerContainer.HealthRatio = character.health.HealthPercentage;
            centerContainer.EnergyRatio = character.energy.EnergyPercentage;
        }
    }


    public void Initialize(Player player, Character character)
    {
        if (isInitialized) return;

        this.character = character;
        playerControls = player.playerControls;

        // TODO: Spawn other menu documents
        
        // playerControls.UI.PauseMenu.performed += ctx => ToggleMenu(HUDMenu.PauseMenu);
        // playerControls.UI.LoadoutMenu.performed += ctx => ToggleMenu(HUDMenu.LoadoutMenu);
        // playerControls.UI.Chat.performed += ctx => ToggleMenu(HUDMenu.Chat, true);
        // playerControls.UI.Close.performed += ctx => HandleCloseInput();

        // playerControls.UI.Leaderboard.started += ctx => leaderboard.ToggleMenu(true);
        // playerControls.UI.Leaderboard.canceled += ctx => leaderboard.ToggleMenu(false);
        // playerControls.UI.Enable();

        // pauseMenu.Initialize(player, character);
        // chatWindow.Initialize(this);
        // centerClusterUI.Initialize(character);
        // loadoutMenu.Initialize(character.GetComponent<CharacterLoadoutManager>(), this);
        // leaderboard.Initialize();
        killFeed = (ToastContainer)UIManager.Spawn("UI/Toast/ToastContainer", hudUIDocument.rootVisualElement);
        killFeed.name = "KillFeed";
        killFeed.Initialize(NotificationType.KillFeed, 5f);
        // identifierManager.Initialize();

        // health.onAppliedDamage.AddListener(SetHitMarker);
        GameModeHandler.Instance.OnStatUpdated.AddListener(SetObjectiveData);
        GameModeHandler.Instance.currentPhaseCountdown.OnValueChanged += SetCountDownTimer;
        // GameModeHandler.Instance.currentPhase.OnValueChanged += SetCurrentPhaseData;

        isInitialized = true;
        SetHUDActive(false);
    }

    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;

        playerControls.UI.Disable();
        // playerControls.UI.PauseMenu.performed -= ctx => ToggleMenu(HUDMenu.PauseMenu);
        // playerControls.UI.LoadoutMenu.performed -= ctx => ToggleMenu(HUDMenu.LoadoutMenu);
        // playerControls.UI.Chat.performed -= ctx => ToggleMenu(HUDMenu.Chat, true);
        // playerControls.UI.Close.performed -= ctx => HandleCloseInput();

        // playerControls.UI.Leaderboard.started -= ctx => leaderboard.ToggleMenu(true);
        // playerControls.UI.Leaderboard.canceled -= ctx => leaderboard.ToggleMenu(false);

        // pauseMenu.Deinitialize();
        // chatWindow.Deinitialize();
        // centerClusterUI.Deinitialize();
        // loadoutMenu.Deinitialize();
        // leaderboard.Deinitialize();
        killFeed?.Deinitialize();
        killFeed = null;
        // identifierManager.Deinitialize();

        leftSideContainer.Query<LoadoutItemUI>().ForEach(child => child.Deinitialize());
        rightSideContainer.Query<LoadoutItemUI>().ForEach(child => child.Deinitialize());
        centerContainer.Q<DriveUI>()?.Deinitialize();

        // if (health != null) health.onAppliedDamage.RemoveListener(SetHitMarker);
        if (GameModeHandler.Instance)
        {
            GameModeHandler.Instance.OnStatUpdated.RemoveListener(SetObjectiveData);
            GameModeHandler.Instance.currentPhaseCountdown.OnValueChanged -= SetCountDownTimer;
            // GameModeHandler.Instance.currentPhase.OnValueChanged -= SetCurrentPhaseData;
        }

        character = null;
        playerControls = null;
    }







    public void SetHUDActive(bool isActive)
    {
        this.isActive = isActive;
        hudUIDocument.rootVisualElement.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetObjectiveData(StatEvent statEvent)
    {
        if (statEvent.StatType == StatEventType.WIN_CONDITION)
        {
            if (statEvent.Source == character.identification.FetchEntityId())
            {
                objectiveContainer.LeftObjectiveNumber.Text = statEvent.Value.ToString("00");
            }
            else
            {
                int.TryParse(objectiveContainer.RightObjectiveNumber.Text, out int currentValue);
                if (statEvent.Value > currentValue) objectiveContainer.RightObjectiveNumber.Text = statEvent.Value.ToString("00");
            }
        }
    }

    private void SetCountDownTimer(float _, float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        objectiveContainer.Clock.Text = $"{minutes:00}:{seconds:00}";
    }

    
    private void SetCurrentPhaseData(Phase _, Phase phase)
    {
        // TODO: Do this
        if (phase == Phase.NULL)
        {
            // currentPhaseText.gameObject.SetActive(false);
            return;
        } 
        
        // currentPhaseText.text = phase.ToString();
        // currentPhaseText.color = phaseColors[phase];
        // if (phase == Phase.ACTIVE) currentPhaseText.gameObject.SetActive(false);
        // else currentPhaseText.gameObject.SetActive(true);
    }

    public DriveUI SetDrive(Drive drive)
    {
        DriveUI driveUI = (DriveUI)UIManager.Spawn("UI/HUD/DriveUI/DriveUI", centerContainer);
        driveUI.Initialize(drive);
        return driveUI;
    }

    public LoadoutItemUI SetThrowableUI(ThrowableManager throwable)
    {
        LoadoutItemUI throwableUI = (LoadoutItemUI)UIManager.Spawn("UI/HUD/LoadoutItemUI/LoadoutItemUI", leftSideContainer);
        throwableUI.BringToFront();
        throwableUI.Initialize(throwable);
        return throwableUI;
    }

    public LoadoutItemUI SetGearUI(Gear gear)
    {
        LoadoutItemUI gearUI = (LoadoutItemUI)UIManager.Spawn("UI/HUD/LoadoutItemUI/LoadoutItemUI", leftSideContainer);
        gearUI.SendToBack();
        gearUI.Initialize(gear);
        return gearUI;
    }

    public LoadoutItemUI AddWeaponUI(Weapon weapon)
    {
        LoadoutItemUI weaponUI = (LoadoutItemUI)UIManager.Spawn("UI/HUD/LoadoutItemUI/LoadoutItemUI", rightSideContainer);
        weaponUI.Initialize(weapon);
        return weaponUI;
    }

















    
    public void SetCursorState(bool enabled, bool usingCustomCursor = false)
    {
        if (enabled) cursorLockCounter++;
        else cursorLockCounter = Mathf.Max(0, cursorLockCounter - 1);

        UnityEngine.Cursor.visible = !(usingCustomCursor && enabled);
        UnityEngine.Cursor.lockState = cursorLockCounter > 0 ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

    public void ResetCursorState()
    {
        cursorLockCounter = 0;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    private void HandleCloseInput()
    {
        if (openMenus.Count > 0) ToggleMenu(HUDMenu.None);
        else ToggleMenu(HUDMenu.PauseMenu);
    }

    public void ToggleMenu(HUDMenu menu, bool forceOpen = false)
    {
        // Don't do anything if the HUD is disabled.
        if (!isActive) return;

        // If no menu is specified, close the last-opened menu.
        if (menu == HUDMenu.None)
        {
            if (openMenus.Count > 0) ToggleMenu(openMenus[^1]);
            return;
        }
        
        // Some menus can lock out the system so that the currently opened menu must be closed before another can be opened.
        if (menuLock != HUDMenu.None && menuLock != menu) return;
        if (forceOpen && openMenus.Contains(menu)) return;

        switch (menu)
        {
            case HUDMenu.Chat:
                // Only enable chat if no other menus are open, and lock other menus while chat is open.
                if (openMenus.Count > 0 && !openMenus.Contains(HUDMenu.Chat)) return;
                // bool isActive = chatWindow.ToggleMenu();
                menuLock = isActive ? HUDMenu.Chat : HUDMenu.None;
                break;
            case HUDMenu.LoadoutMenu:
                // loadoutMenu.ToggleMenu();
                break;
            case HUDMenu.PauseMenu:
                // pauseMenu.ToggleMenu();
                break;
            default:
                break;
        }

        if (openMenus.Contains(menu))
        {
            openMenus.Remove(menu);
            if (openMenus.Count == 0) character.characterInputs.SetCharacterControlsRpc(true);
            SetCursorState(false);
        }
        else
        {
            SetCursorState(true);
            if (openMenus.Count == 0) character.characterInputs.SetCharacterControlsRpc(false);
            openMenus.Add(menu);
        }
    }
}
