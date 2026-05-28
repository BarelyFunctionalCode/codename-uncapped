using System.Linq;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


public class LobbyPCController : NetworkBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private LobbyPC lobbyPC;
    private MatchSelection matchSelection;

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
        matchSelection = lobbyPC.MatchSelection;

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
                IntroController possibleIntro = FindAnyObjectByType<IntroController>();
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


    #region MatchSelection
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
        matchSelection.SetClientOptionValue("LEVEL", newLevelName.ToString());
    }
    private void OnSelectedGameModeNameChanged(FixedString64Bytes _, FixedString64Bytes newGameModeName)
    {
        if (IsHost) return;
        matchSelection.SetClientOptionValue("GAME MODE", newGameModeName.ToString());
    }
    private void OnSelectedMaxPlayersChanged(int _, int newMaxPlayers)
    {
        if (IsHost) return;
        matchSelection.SetClientOptionValue("MAX PLAYERS", newMaxPlayers.ToString());
    }
    private void OnSelectedTimeLimitChanged(int _, int newTimeLimit)
    {
        if (IsHost) return;
        matchSelection.SetClientOptionValue("TIME LIMIT", newTimeLimit.ToString());
    }
    private void OnSelectedWinConditionValueChanged(int _, int newWinConditionValue)
    {
        if (IsHost) return;
        matchSelection.SetClientOptionValue("WIN CONDITION VALUE", newWinConditionValue.ToString());
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
        matchSelection.AddCharacter(character);
    }
    #endregion
}