using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public enum HUDMenu
{
    None,
    Chat,
    LoadoutMenu,
    PauseMenu
}

public class HUD : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private TMP_Text leftObjectiveText;
    [SerializeField] private TMP_Text rightObjectiveText;
    [SerializeField] private TMP_Text countdownMinutesText;
    [SerializeField] private TMP_Text countdownSecondsText;
    [SerializeField] private TMP_Text currentPhaseText;
    readonly Dictionary<Phase, Color> phaseColors = new()
    {
        { Phase.PRELOAD, Color.white },
        { Phase.WARMUP, Color.yellow },
        { Phase.ACTIVE, Color.green },
        { Phase.ENDGAME, Color.red },
    };

    [SerializeField] private CenterClusterUI centerClusterUI;
    [SerializeField] private Transform weaponsContainer;
    [SerializeField] private GameObject weaponUIPrefabObj;
    [SerializeField] private ThrowableUI throwableUI;
    [SerializeField] private Transform gearContainer;
    [SerializeField] private RectTransform dynamicReticle;
    [SerializeField] private GameObject hitMarkerObj;
    [SerializeField] private AudioSource hitMarkerSound;

    private PlayerController playerController;
    private PlayerControls playerControls;
    private List<HUDMenu> openMenus = new();
    [SerializeField] private ChatWindow chatWindow;
    [SerializeField] public LoadoutMenu loadoutMenu;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private Leaderboard leaderboard;

    private float dynamicReticleMaxMoveRange = 50f;
    private float dynamicReticleMaxVelocityDeflection = 50f;

    private float hitmarkerDisplayTime = 0.07f;
    private float hitmarkerTimer = 0f;

    private HUDMenu menuLock = HUDMenu.None;

    private bool isInitialized = false;

    private void Update()
    {
        if (!isInitialized) return;

        // Update dynamic reticle position based on player velocity
        if (playerController == null || playerController.localRb == null) return;
        Vector3 localForwardVelocity = Vector3.Project(playerController.localRb.linearVelocity, playerController.transform.forward);
        Vector3 velocity = playerController.transform.InverseTransformVector(localForwardVelocity);
        float deflectionX = Mathf.Clamp(-velocity.x, -dynamicReticleMaxVelocityDeflection, dynamicReticleMaxVelocityDeflection);
        float deflectionY = Mathf.Clamp(-velocity.y, -dynamicReticleMaxVelocityDeflection, dynamicReticleMaxVelocityDeflection);
        Vector2 dynamicReticleTargetPos = new Vector2(deflectionX / dynamicReticleMaxVelocityDeflection * dynamicReticleMaxMoveRange,
                                                      deflectionY / dynamicReticleMaxVelocityDeflection * dynamicReticleMaxMoveRange);
        dynamicReticle.anchoredPosition = Vector2.Lerp(dynamicReticle.anchoredPosition, dynamicReticleTargetPos, Time.deltaTime * 10f);

        // Update hitmarker timer
        if (hitMarkerObj.activeSelf)
        {
            hitmarkerTimer += Time.deltaTime;
            if (hitmarkerTimer >= hitmarkerDisplayTime)
            {
                hitMarkerObj.SetActive(false);
                hitmarkerTimer = 0f;
            }
        }
    }

    private void OnDestroy()
    {
        if (!isInitialized) return;

        playerControls.UI.PauseMenu.performed -= ctx => ToggleMenu(HUDMenu.PauseMenu);
        playerControls.UI.LoadoutMenu.performed -= ctx => ToggleMenu(HUDMenu.LoadoutMenu);
        playerControls.UI.Chat.performed -= ctx => ToggleMenu(HUDMenu.Chat, true);
        playerControls.UI.Close.performed -= ctx => ToggleMenu(HUDMenu.None);
        
        playerControls.UI.Leaderboard.started -= ctx => leaderboard.ToggleMenu(true);
        playerControls.UI.Leaderboard.canceled -= ctx => leaderboard.ToggleMenu(false);

        playerController.onAppliedDamage.RemoveListener(SetHitMarker);
        GameModeHandler.Instance.currentPhaseCountdown.OnValueChanged -= SetCountDownTimer;
        GameModeHandler.Instance.currentPhase.OnValueChanged -= SetCurrentPhase;
    }

    public void Initialize(PlayerController playerController)
    {
        if (isInitialized) return;

        this.playerController = playerController;
        playerControls = playerController.playerControls;
        playerControls.UI.PauseMenu.performed += ctx => ToggleMenu(HUDMenu.PauseMenu);
        playerControls.UI.LoadoutMenu.performed += ctx => ToggleMenu(HUDMenu.LoadoutMenu);
        playerControls.UI.Chat.performed += ctx => ToggleMenu(HUDMenu.Chat, true);
        playerControls.UI.Close.performed += ctx => ToggleMenu(HUDMenu.None);

        playerControls.UI.Leaderboard.started += ctx => leaderboard.ToggleMenu(true);
        playerControls.UI.Leaderboard.canceled += ctx => leaderboard.ToggleMenu(false);

        PauseMenu.Instance.Initialize(playerController);
        chatWindow.Initialize(this);
        centerClusterUI.Initialize(playerController);
        loadoutMenu.Initialize(playerController.GetComponent<PlayerLoadoutManager>(), this);
        leaderboard.Initialize();

        playerController.onAppliedDamage.AddListener(SetHitMarker);
        GameModeHandler.Instance.currentPhaseCountdown.OnValueChanged += SetCountDownTimer;
        GameModeHandler.Instance.currentPhase.OnValueChanged += SetCurrentPhase;

        isInitialized = true;
    }

    public void ToggleHUD()
    {
        if (mainCanvas != null) mainCanvas.enabled = !mainCanvas.enabled;
    }

    private void SetHitMarker(float damageAmount)
    {
        if (damageAmount <= 0) return;

        hitMarkerObj.SetActive(true);
        hitMarkerSound.Play();
    }

    private void SetCountDownTimer(float _, float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        countdownMinutesText.text = minutes.ToString("00");
        countdownSecondsText.text = seconds.ToString("00");
    }

    private void SetCurrentPhase(Phase _, Phase phase)
    {
        currentPhaseText.text = phase.ToString();
        currentPhaseText.color = phaseColors[phase];
        if (phase == Phase.ACTIVE) currentPhaseText.gameObject.SetActive(false);
        else currentPhaseText.gameObject.SetActive(true);
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

    public void ToggleMenu(HUDMenu menu, bool forceOpen = false)
    {
        // Don't do anything if the HUD is disabled.
        if (!mainCanvas.enabled) return;

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
                bool isActive = chatWindow.ToggleMenu();
                menuLock = isActive ? HUDMenu.Chat : HUDMenu.None;
                break;
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
