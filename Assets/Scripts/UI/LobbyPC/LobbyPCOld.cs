using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyPC : CustomUIElementBase
{
    private VisualElement interactPrompt;
    private VisualElement autoStartNotice;
    private VisualElement optionsListsContainer;

    private struct OptionListData
    {
        public string listName;
        public Func<List<string>> items;
    }
    private readonly List<OptionListData> optionsListsData = new()
    {
        new OptionListData { listName = "LEVEL", items = () => LevelManager.availableLevels.Select(level => level.displayName).ToList() },
        new OptionListData { listName = "GAME MODE", items = () => GameModeHandler.availableGameModes.Values.Select(gm => gm.displayName).ToList() },
    };

    public void Initialize()
    {
        interactPrompt = this.Q("interact-prompt");
        autoStartNotice = this.Q("auto-start-notice");
        optionsListsContainer = this.Q("options-lists");

        ToggleInteractPrompt(false);
        ToggleAutoStartNotice(false);

        BuildOptionsLists();
    }

    public void ToggleInteractPrompt(bool show)
    {
        interactPrompt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void ToggleAutoStartNotice(bool show)
    {
        autoStartNotice.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BuildOptionsLists()
    {
        for (int i = 0; i < optionsListsData.Count; i++)
        {
            ExpandableList newExpandableList = (ExpandableList)UIManager.Spawn("UI/ExpandableList/ExpandableList", optionsListsContainer);
            newExpandableList.Initialize(optionsListsData[i].listName, OnListItemSelected, true);

            foreach (string item in optionsListsData[i].items())
            {
                newExpandableList.AddListItem(item, item, true);
            }
        }
    }

    private void OnListItemSelected(string listName, string itemValue)
    {
        
    }
}


public class LobbyPCOld : NetworkBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private LobbyPC lobbyPC;
    // private Canvas canvas;
    [SerializeField] private CinemachineCamera pcCam;
    [SerializeField] private LayerMask noPlayerMask;

    // [SerializeField] private GameObject interactPromptObj;
    // [SerializeField] private GameObject autoStartObj;

    // [SerializeField] private GameObject cursorObj;
    [SerializeField] public AudioSource musicSource;

    // private float camToCanvasDistance;
    private bool showInteractPrompt = false;
    private bool isActive = false;
    private bool autoInteract = true;
    private bool autoStart = false;

    // [SerializeField] private Button activeTabButton;
    // [SerializeField] private GameObject matchConfigurationContainerObj;
    // [SerializeField] private GameObject lobbiesListContainerObj;

    // private float activeTabButtonAlpha = 0.4f;
    // private float inactiveTabButtonAlpha;

    private bool isInitialized = false;


    void Awake()
    {
        GameManager.Instance.OnLevelLoadedEvent.AddListener(OnLevelLoadedEvent);

        lobbyPC = uiDocument.rootVisualElement.Q<LobbyPC>();
        lobbyPC.Initialize();
        
        // canvas = GetComponentInChildren<Canvas>();
        // camToCanvasDistance = pcCam.GetComponent<CinemachinePositionComposer>().CameraDistance;

        lobbyPC.ToggleInteractPrompt(true);
        // cursorObj.SetActive(false);
        // matchConfigurationContainerObj.SetActive(true);
        // lobbiesListContainerObj.SetActive(false);

        // Color tabColor = activeTabButton.image.color;
        // inactiveTabButtonAlpha = tabColor.a;
        // tabColor.a = activeTabButtonAlpha;
        // activeTabButton.image.color = tabColor;

        DevNetworkManager possibleDevNetworkManager = FindAnyObjectByType<DevNetworkManager>();
        if (possibleDevNetworkManager != null && possibleDevNetworkManager.doAutoStart)
        {
            autoStart = true;
            lobbyPC.ToggleAutoStartNotice(true);
            lobbyPC.ToggleInteractPrompt(false);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        Reset();
        if (GameManager.Instance) GameManager.Instance.OnLevelLoadedEvent.RemoveListener(OnLevelLoadedEvent);
    }

    void Update()
    {
        if (!isInitialized && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (autoInteract)
            {
                autoInteract = false;
                Interact();
                Intro possibleIntro = FindAnyObjectByType<Intro>();
                if (possibleIntro != null) possibleIntro.IsLoaded();
            }
            isInitialized = true;
        }

        musicSource.spatialBlend = Mathf.Lerp(musicSource.spatialBlend, isActive ? 0f : 1f, Time.deltaTime);

        // if (Camera.main != null && cursorObj != null && isActive)
        // {
        //     Vector2 mousePosition = Mouse.current.position.ReadValue();
        //     Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, camToCanvasDistance));
        //     cursorObj.transform.position = cursorPosition;
        // }

        if (Keyboard.current.fKey.wasPressedThisFrame && showInteractPrompt && !isActive)
        {
            Interact();
        }
    }

    private void FixedUpdate()
    {
        lobbyPC.ToggleInteractPrompt(showInteractPrompt && !isActive);
        showInteractPrompt = false;
    }

    private void OnLevelLoadedEvent(string levelName)
    {
        if (levelName == gameObject.scene.name)
        {
            foreach (Character character in CharacterManager.Instance.characters)
            {
                if (character == null) continue;
                
                character.characterInputs.SetHUDActiveRpc(false);
                character.Teleport(Vector3.zero, Quaternion.identity);
            }
        }
    }

    private void Interact()
    {
        if (isActive) return;
        Player.Instance.DisableControls();

        // Sets priority to PC Cam and then unlocks the cursor
        Camera.main.cullingMask = noPlayerMask;
        pcCam.Priority.Value = 99;
        Player.Instance.playerHUD.SetCursorState(true, true);
        // cursorObj.SetActive(true);
        isActive = true;
        autoInteract = false;
    }

    public void Reset()
    {
        if (!isActive) return;
        // Resets priority to PC Cam and then locks the cursor
        isActive = false;
        // cursorObj.SetActive(false);
        if (Camera.main != null) Camera.main.cullingMask = -1;
        pcCam.Priority.Value = 0;

        if (Player.Instance == null) return;
        Player.Instance.playerHUD.SetCursorState(false);
        Player.Instance.EnableControls();
    }

    // public void OnExitButtonSelect()
    // {
    //     Reset();
    // }

    void OnTriggerStay(Collider other)
    {
        if (isActive || autoStart) return;

        Character localPlayerCharacter = Player.Instance.Character;
        if (localPlayerCharacter == null) return;
        Character character = other.GetComponentInParent<Character>();
        CharacterPuppet characterPuppet = other.GetComponentInParent<CharacterPuppet>();
        if ((character != null && character == localPlayerCharacter) || characterPuppet != null)
        {
            lobbyPC.ToggleInteractPrompt(true);
            showInteractPrompt = true;
        }
    }
}