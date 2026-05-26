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
    private VisualElement levelSelectContainer;
    private VisualElement gameModeSelectContainer;
    private VisualElement otherOptionsContainer;
    private Label levelDescriptionLabel;
    private Label gameModeDescriptionLabel;
    private Button startMatchButton;
    private VisualElement playerListContainer;
    private VisualElement chatContainer;

    private struct OptionListData
    {
        public string listName;
        public Func<object, List<string>> items;
    }
    private readonly List<OptionListData> staticOptionsListsData = new()
    {
        new OptionListData { listName = "LEVEL", items = (object _) => LevelManager.availableLevels.Select(level => level.displayName).ToList() },
        new OptionListData { listName = "GAME MODE", items = (object _) => GameModeHandler.availableGameModes.Values.Select(gm => gm.displayName).ToList() },
    };
    private readonly List<OptionListData> dynamicOptionsListsData = new()
    {
        new OptionListData { listName = "MAX PLAYERS", items = (object gameMode) => {
                MaxAllowedPlayersOptions maxPlayersOptionDefault = ((GameModeSO)gameMode).maxAllowedPlayers;
                List<string> maxPlayerOptions = Enum.GetValues(typeof(MaxAllowedPlayersOptions))
                    .Cast<MaxAllowedPlayersOptions>()
                    .Where(option => option <= maxPlayersOptionDefault)
                    .Select(option => ((int)option).ToString())
                    .ToList();
                return maxPlayerOptions;
            }
        },
        new OptionListData { listName = "TIME LIMIT", items = (object gameMode) => {
                MaxAllowedTimeLimitOptions timeLimitOptionDefault = ((GameModeSO)gameMode).maxAllowedTimeLimitMinutes;
                List<string> timeLimitOptions = System.Enum.GetValues(typeof(MaxAllowedTimeLimitOptions))
                    .Cast<MaxAllowedTimeLimitOptions>()
                    .Where(option => option <= timeLimitOptionDefault)
                    .Select(option => ((int)option).ToString())
                    .ToList();
                return timeLimitOptions;
            }
        },
    };
    private Dictionary<string, ExpandableList> staticOptionsLists = new();
    private Dictionary<string, ExpandableList> dynamicOptionsLists = new();
    private NumberSelect winConditionValueSelect;


    public void Initialize(LobbyPCController lobbyPCController)
    {
        this.lobbyPCController = lobbyPCController;
        interactPrompt = this.Q("interact-prompt");
        autoStartNotice = this.Q("auto-start-notice");
        levelSelectContainer = this.Q("level-select");
        gameModeSelectContainer = this.Q("game-mode-select");
        otherOptionsContainer = this.Q("other-options");
        levelDescriptionLabel = this.Q<Label>("level-description");
        gameModeDescriptionLabel = this.Q<Label>("game-mode-description");
        startMatchButton = this.Q<Button>("start-button");
        playerListContainer = this.Q<VisualElement>("player-list");
        chatContainer = this.Q<VisualElement>("chat-container");

        ChatWindow chatWindow = (ChatWindow)UIManager.Spawn("UI/Chat/ChatWindow", chatContainer);
        chatWindow.Initialize(null);

        startMatchButton.clicked += OnStartMatchButtonPressed; // TODO: Is clicked really the correct event to use here?

        ToggleInteractPrompt(false);
        ToggleAutoStartNotice(false);

        BuildStaticOptionsLists();
    }

    public void ToggleInteractPrompt(bool show)
    {
        interactPrompt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void ToggleAutoStartNotice(bool show)
    {
        autoStartNotice.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BuildStaticOptionsLists()
    {
        for (int i = 0; i < staticOptionsListsData.Count; i++)
        {
            string listName = staticOptionsListsData[i].listName;
            ExpandableList newExpandableList = (ExpandableList)UIManager.Spawn("UI/ExpandableList/ExpandableList", listName == "LEVEL" ? levelSelectContainer : gameModeSelectContainer);
            newExpandableList.Initialize(listName, OnListItemSelected, true);

            foreach (string item in staticOptionsListsData[i].items(null))
            {
                newExpandableList.AddListItem(item, item, true);
            }

            string firstItemValue = staticOptionsListsData[i].items(null).FirstOrDefault();
            schedule.Execute(() => newExpandableList.SetSelectedItem(firstItemValue)).StartingIn(200);
            staticOptionsLists.Add(listName, newExpandableList);
        }
    }

    private void BuildDynamicOptionsLists(GameModeSO gameMode)
    {
        dynamicOptionsLists.Values.ToList().ForEach(list => list.RemoveFromHierarchy());
        dynamicOptionsLists.Clear();
        winConditionValueSelect?.RemoveFromHierarchy();

        for (int i = 0; i < dynamicOptionsListsData.Count; i++)
        {
            ExpandableList newExpandableList = (ExpandableList)UIManager.Spawn("UI/ExpandableList/ExpandableList", otherOptionsContainer);
            newExpandableList.Initialize(dynamicOptionsListsData[i].listName, OnListItemSelected, true);

            foreach (string item in dynamicOptionsListsData[i].items(gameMode))
            {
                newExpandableList.AddListItem(item, item, true);
            }

            string firstItemValue = dynamicOptionsListsData[i].items(gameMode).LastOrDefault();
            schedule.Execute(() => newExpandableList.SetSelectedItem(firstItemValue)).StartingIn(200);
            dynamicOptionsLists.Add(dynamicOptionsListsData[i].listName, newExpandableList);
        }

        winConditionValueSelect = (NumberSelect)UIManager.Spawn("UI/NumberSelect/NumberSelect", otherOptionsContainer);
        winConditionValueSelect.Initialize(gameMode.winConditionReaderFriendlyName + " Limit", (int)gameMode.winConditionDefaultValue, OnWinConditionValueChanged, 0, 666);
    }

    public void OnListItemSelected(string listName, string itemValue)
    {
        if (listName == "LEVEL")
        {
            LevelSO levelInfo = LevelManager.availableLevels.FirstOrDefault(level => level.displayName == itemValue);
            if (levelInfo != null)
            {
                levelDescriptionLabel.text = levelInfo.description;
            }
            lobbyPCController.SetSelectedLevel(itemValue);
        }
        else if (listName == "GAME MODE")
        {
            GameModeSO gameModeInfo = GameModeHandler.availableGameModes.Values.FirstOrDefault(gm => gm.displayName == itemValue);
            if (gameModeInfo != null)
            {
                gameModeDescriptionLabel.text = gameModeInfo.description;
                BuildDynamicOptionsLists(gameModeInfo);
            }
            lobbyPCController.SetSelectedGameMode(itemValue);
        }
        else if (listName == "MAX PLAYERS")
        {
            lobbyPCController.SetSelectedMaxPlayers(int.Parse(itemValue));
        }
        else if (listName == "TIME LIMIT")
        {
            lobbyPCController.SetSelectedTimeLimit(int.Parse(itemValue));
        }
    }

    private void OnWinConditionValueChanged(int newValue)
    {
        lobbyPCController.SetSelectedWinConditionValue(newValue);
    }

    public void SetClientOptionValue(string optionName, string value)
    {
        
        if (staticOptionsLists.TryGetValue(optionName, out ExpandableList list))
        {
            list.SetSelectedItem(value);
        }
        else if (dynamicOptionsLists.TryGetValue(optionName, out list))
        {
            list.SetSelectedItem(value);
        }
        else
        {
            winConditionValueSelect.SetValue(int.Parse(value));
        }
        
    }

    private void OnStartMatchButtonPressed()
    {
        lobbyPCController.OnStartMatchButtonPressed();
    }

    public void AddCharacter(Character character)
    {
        LobbyCharacterEntry existingEntry = playerListContainer.Children().OfType<LobbyCharacterEntry>().FirstOrDefault(entry => entry.Character == character);
        if (existingEntry != null) return;

        LobbyCharacterEntry newEntry = (LobbyCharacterEntry)UIManager.Spawn("UI/LobbyPC/LobbyCharacterEntry", playerListContainer);
        newEntry.Initialize(character);
    }
}

[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyCharacterEntry : CustomUIElementBase
{
    public Character Character { get; private set; }
    private Label characterNameLabel;

    public LobbyCharacterEntry()
    {
        characterNameLabel = new Label()
        {
            name = "character-name",
            text = "Character Name"
        };
        Add(characterNameLabel);
    }

    public void Initialize(Character character)
    {
        Character = character;

        Identification entityIdentification = character.identification;

        string characterName = entityIdentification.FetchEntityName();
        characterNameLabel.text = characterName;

        int teamId = entityIdentification.FetchTeamId();
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
    private NetworkVariable<int> selectedMaxPlayers = new();
    private NetworkVariable<int> selectedTimeLimit = new();
    private NetworkVariable<int> selectedWinConditionValue = new();

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
        selectedMaxPlayers.OnValueChanged += OnSelectedMaxPlayersChanged;
        selectedTimeLimit.OnValueChanged += OnSelectedTimeLimitChanged;
        selectedWinConditionValue.OnValueChanged += OnSelectedWinConditionValueChanged;
        OnSelectedLevelNameChanged(string.Empty, selectedLevelName.Value);
        OnSelectedGameModeNameChanged(string.Empty, selectedGameModeName.Value);
        OnSelectedMaxPlayersChanged(0, selectedMaxPlayers.Value);
        OnSelectedTimeLimitChanged(0, selectedTimeLimit.Value);
        OnSelectedWinConditionValueChanged(0, selectedWinConditionValue.Value);

        CharacterManager.Instance.OnCharacterChangedTeam.AddListener(AddCharacter);
        foreach (Character character in CharacterManager.Instance.characters)
        {
            AddCharacter(new NetworkBehaviourReference(character));
        }
        
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
    public void SetSelectedMaxPlayers(int maxPlayers)
    {
        if (!IsHost) return;
        selectedMaxPlayers.Value = maxPlayers;
    }
    public void SetSelectedTimeLimit(int timeLimit)
    {   
        if (!IsHost) return;
        selectedTimeLimit.Value = timeLimit;
    }
    public void SetSelectedWinConditionValue(int winConditionValue)
    {
        if (!IsHost) return;
        selectedWinConditionValue.Value = winConditionValue;
    }

    private void OnSelectedLevelNameChanged(FixedString64Bytes _, FixedString64Bytes newLevelName)
    {
        if (IsHost) return;
        lobbyPC.SetClientOptionValue("LEVEL", newLevelName.ToString());
    }
    private void OnSelectedGameModeNameChanged(FixedString64Bytes _, FixedString64Bytes newGameModeName)
    {
        if (IsHost) return;
        lobbyPC.SetClientOptionValue("GAME MODE", newGameModeName.ToString());
    }
    private void OnSelectedMaxPlayersChanged(int _, int newMaxPlayers)
    {
        if (IsHost) return;
        lobbyPC.SetClientOptionValue("MAX PLAYERS", newMaxPlayers.ToString());
    }
    private void OnSelectedTimeLimitChanged(int _, int newTimeLimit)
    {
        if (IsHost) return;
        lobbyPC.SetClientOptionValue("TIME LIMIT", newTimeLimit.ToString());
    }
    private void OnSelectedWinConditionValueChanged(int _, int newWinConditionValue)
    {
        if (IsHost) return;
        lobbyPC.SetClientOptionValue("WIN CONDITION VALUE", newWinConditionValue.ToString());
    }

    public void OnStartMatchButtonPressed()
    {
        if (!IsHost) return;

        Reset();
        
        LevelSO selectedLevel = LevelManager.availableLevels.FirstOrDefault(level => level.displayName == selectedLevelName.Value.ToString());
        GameManager.Instance.SetLevel(selectedLevel.sceneName);

        GameModeSO selectedGameMode = GameModeHandler.availableGameModes.Values.FirstOrDefault(gm => gm.displayName == selectedGameModeName.Value.ToString());
        GameModeData gameModeData = selectedGameMode.GetGameModeData(
            // selectedMaxPlayers,
            // selectedTimeLimit,
            // selectedWinConditionValue
        );
        GameManager.Instance.SetGameModeData(gameModeData);
        GameManager.Instance.LoadLevel();
    }


    private void AddCharacter(NetworkBehaviourReference characterRef)
    {
        characterRef.TryGet(out Character character);
        if (character == null) return;
        lobbyPC.AddCharacter(character);
    }
}