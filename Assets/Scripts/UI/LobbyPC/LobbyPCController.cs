using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyPC : CustomUIElementBase
{
    private LobbyPCController lobbyPCController;
    private VisualElement interactPrompt;
    private VisualElement autoStartNotice;
    private VisualElement optionsListsContainer;
    private Label levelNameLabel;
    private Label levelDescriptionLabel;
    private Label gameModeNameLabel;
    private Label gameModeDescriptionLabel;
    private Button startMatchButton;
    private VisualElement playerListContainer;
    private VisualElement chatContainer;

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

    public void Initialize(LobbyPCController lobbyPCController)
    {
        this.lobbyPCController = lobbyPCController;
        interactPrompt = this.Q("interact-prompt");
        autoStartNotice = this.Q("auto-start-notice");
        optionsListsContainer = this.Q("options-lists");
        levelNameLabel = this.Q<Label>("level-name");
        levelDescriptionLabel = this.Q<Label>("level-description");
        gameModeNameLabel = this.Q<Label>("game-mode-name");
        gameModeDescriptionLabel = this.Q<Label>("game-mode-description");
        startMatchButton = this.Q<Button>("start-button");
        playerListContainer = this.Q<VisualElement>("player-list");
        chatContainer = this.Q<VisualElement>("chat-container");

        ChatWindow chatWindow = (ChatWindow)UIManager.Spawn("UI/Chat/ChatWindow", chatContainer);
        chatWindow.Initialize(null);

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

    public void OnListItemSelected(string listName, string itemValue)
    {
        if (listName == "LEVEL")
        {
            LevelSO levelInfo = LevelManager.availableLevels.FirstOrDefault(level => level.displayName == itemValue);
            if (levelInfo != null)
            {
                levelNameLabel.text = levelInfo.displayName;
                levelDescriptionLabel.text = levelInfo.description;
            }
            lobbyPCController.SetSelectedLevel(itemValue);
        }
        else if (listName == "GAME MODE")
        {
            GameModeSO gameModeInfo = GameModeHandler.availableGameModes.Values.FirstOrDefault(gm => gm.displayName == itemValue);
            if (gameModeInfo != null)
            {
                gameModeNameLabel.text = gameModeInfo.displayName;
                gameModeDescriptionLabel.text = gameModeInfo.description;
            }
            lobbyPCController.SetSelectedGameMode(itemValue);
        }
    }

    public void AddPlayerToList(string playerName)
    {
        Label newPlayerLabel = new Label(playerName);
        playerListContainer.Add(newPlayerLabel);
    }
}


public class LobbyPCController : NetworkBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private LobbyPC lobbyPC;

    [SerializeField] private CinemachineCamera pcCam;
    [SerializeField] private LayerMask noPlayerMask;
    [SerializeField] public AudioSource musicSource;

    private NetworkVariable<FixedString64Bytes> selectedLevelName = new();
    private NetworkVariable<FixedString64Bytes> selectedGameModeName = new();



    private List<LobbyCharacter> lobbyPlayers = new();




    private bool showInteractPrompt = false;
    private bool isActive = false;
    private bool autoInteract = true;
    private bool autoStart = false;
    private bool isInitialized = false;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        GameManager.Instance.OnLevelLoadedEvent.AddListener(OnLevelLoadedEvent);

        lobbyPC = uiDocument.rootVisualElement.Q<LobbyPC>();
        lobbyPC.Initialize(this);

        selectedLevelName.OnValueChanged += OnSelectedLevelNameChanged;
        selectedGameModeName.OnValueChanged += OnSelectedGameModeNameChanged;
        OnSelectedLevelNameChanged(string.Empty, selectedLevelName.Value);
        OnSelectedGameModeNameChanged(string.Empty, selectedGameModeName.Value);
        
        lobbyPC.ToggleInteractPrompt(true);

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

        if (Keyboard.current.fKey.wasPressedThisFrame && showInteractPrompt && !isActive)
        {
            Interact();
        }
    }

    private void FixedUpdate()
    {
        if (lobbyPC == null) return;
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
        Player.Instance.playerHUD.SetCursorState(true, false);
        isActive = true;
        autoInteract = false;
    }

    public void Reset()
    {
        if (!isActive) return;
        // Resets priority to PC Cam and then locks the cursor
        isActive = false;
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
            if (lobbyPC == null) return;
            lobbyPC.ToggleInteractPrompt(true);
            showInteractPrompt = true;
        }
    }

    public void SetSelectedLevel(string levelName)
    {
        if (!IsHost) return;
        selectedLevelName.Value = levelName;
    }

    public void SetSelectedGameMode(string gameModeName)
    {
        if (!IsHost) return;
        selectedGameModeName.Value = gameModeName;
    }

    private void OnSelectedLevelNameChanged(FixedString64Bytes oldLevelName, FixedString64Bytes newLevelName)
    {
        if (IsHost) return;
        lobbyPC.OnListItemSelected("LEVEL", newLevelName.ToString());
    }

    private void OnSelectedGameModeNameChanged(FixedString64Bytes oldGameModeName, FixedString64Bytes newGameModeName)
    {
        if (IsHost) return;
        lobbyPC.OnListItemSelected("GAME MODE", newGameModeName.ToString());
    }





    private void AddCharacter(ulong characterId)
    {
        if (!IsHost) return;

        LobbyCharacter lobbyPlayerToRemove = lobbyPlayers.Find(lp => lp.GetComponent<LobbyCharacter>().characterId == characterId);
        if (lobbyPlayerToRemove != null) return;

        Character character = CharacterManager.Instance.GetCharacterByEntityId(characterId);
        if (!character.isInitialized) return;

        Identification entityIdentification = character.identification;

        string playerName = entityIdentification.FetchEntityName();
        // int teamId = (selectedGameMode.teamBasedType == TeamBasedType.TEAM) ? (int)entityIdentification.FetchTeamId() : -1;

        // GameObject lobbyPlayerObj = Instantiate(lobbyPlayerPrefabObj, parentColumn);
        // LobbyCharacter lobbyPlayer = lobbyPlayerObj.GetComponent<LobbyCharacter>();
        // lobbyPlayer.Initialize(this, characterId, playerName, teamId, IsHost);
        // lobbyPlayers.Add(lobbyPlayer);
    }
}