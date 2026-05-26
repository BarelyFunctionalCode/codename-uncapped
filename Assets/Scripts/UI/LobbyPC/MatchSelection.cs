using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchSelection : NetworkBehaviour
{
    [SerializeField] private TMP_Dropdown levelSelectDropdown;
    [SerializeField] private TMP_Dropdown gameModeSelectDropdown;
    [SerializeField] private TMP_Dropdown maxPlayersSelectDropdown;
    [SerializeField] private TMP_Dropdown timeLimitSelectDropdown;
    [SerializeField] private TMP_Text winConditionNameText;
    [SerializeField] private TMP_Text winConditionValueText;
    [SerializeField] private GameObject winConditionValueIncrementButtonContainer;
    [SerializeField] private GameObject winConditionValueDecrementButtonContainer;

    [SerializeField] private TMP_Text selectedLevelTitleText;
    [SerializeField] private TMP_Text selectedLevelDescriptionText;

    [SerializeField] private TMP_Text selectedGameModeTitleText;
    [SerializeField] private TMP_Text selectedGameModeDescriptionText;

    [SerializeField] private GameObject playerListTeamSeparatorObj;
    [SerializeField] private Transform playerListColumn0;
    [SerializeField] private Transform playerListColumn1;
    [SerializeField] private GameObject lobbyPlayerPrefabObj;
    private List<LobbyCharacter> lobbyPlayers = new();

    private LevelSO selectedLevel;
    public GameModeSO selectedGameMode;
    private int selectedMaxPlayers;
    private int selectedTimeLimit;
    private int selectedWinConditionValue;

    [SerializeField] private LobbyPCController lobbyPC;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsHost)
        {
            CharacterManager.Instance.OnCharacterAdded.AddListener(AddCharacter);
            GameManager.Instance.OnClientDisconnectedEvent.AddListener(RemoveCharacter);
        }

        InitializeMenu();
    }

    private void InitializeMenu()
    {
        if (levelSelectDropdown.options.Count == 0)
        {
            selectedLevel = LevelManager.availableLevels[0];
            selectedLevelTitleText.text = selectedLevel.displayName;
            selectedLevelDescriptionText.text = selectedLevel.description;
            List<string> levelNames = LevelManager.availableLevels.Select(level => level.displayName).ToList();
            levelSelectDropdown.AddOptions(levelNames);
        }

        if (gameModeSelectDropdown.options.Count == 0)
        {
            List<string> gameModeNames = GameModeHandler.availableGameModes.Values.Select(gm => gm.displayName).ToList();
            selectedGameMode = GameModeHandler.availableGameModes[(GameModes)1];
            selectedGameModeTitleText.text = selectedGameMode.displayName;
            selectedGameModeDescriptionText.text = selectedGameMode.description;
            gameModeSelectDropdown.AddOptions(gameModeNames);
        }

        maxPlayersSelectDropdown.ClearOptions();
        MaxAllowedPlayersOptions maxPlayersOptionDefault = selectedGameMode.maxAllowedPlayers;
        List<string> maxPlayerOptions = System.Enum.GetValues(typeof(MaxAllowedPlayersOptions))
            .Cast<MaxAllowedPlayersOptions>()
            .Where(option => option <= maxPlayersOptionDefault)
            .Select(option => ((int)option).ToString())
            .ToList();
        selectedMaxPlayers = int.Parse(maxPlayerOptions[^1]);
        maxPlayersSelectDropdown.AddOptions(maxPlayerOptions);
        maxPlayersSelectDropdown.value = maxPlayerOptions.IndexOf(selectedMaxPlayers.ToString());

        timeLimitSelectDropdown.ClearOptions();
        MaxAllowedTimeLimitOptions timeLimitOptionDefault = selectedGameMode.maxAllowedTimeLimitMinutes;
        List<string> timeLimitOptions = System.Enum.GetValues(typeof(MaxAllowedTimeLimitOptions))
            .Cast<MaxAllowedTimeLimitOptions>()
            .Where(option => option <= timeLimitOptionDefault)
            .Select(option => ((int)option).ToString())
            .ToList();
        selectedTimeLimit = int.Parse(timeLimitOptions[^1]);
        timeLimitSelectDropdown.AddOptions(timeLimitOptions);
        timeLimitSelectDropdown.value = timeLimitOptions.IndexOf(selectedTimeLimit.ToString());

        winConditionNameText.text = selectedGameMode.winConditionReaderFriendlyName + " Limit";
        winConditionValueText.text = selectedGameMode.winConditionDefaultValue.ToString();
        selectedWinConditionValue = (int)selectedGameMode.winConditionDefaultValue;

        if (!IsHost)
        {
            levelSelectDropdown.interactable = false;
            gameModeSelectDropdown.interactable = false;
            maxPlayersSelectDropdown.interactable = false;
            timeLimitSelectDropdown.interactable = false;
            winConditionValueIncrementButtonContainer.SetActive(false);
            winConditionValueDecrementButtonContainer.SetActive(false);
        }

        playerListTeamSeparatorObj.SetActive(selectedGameMode.teamBasedType == TeamBasedType.TEAM);
        if (IsHost)
        {
            foreach (Character character in CharacterManager.Instance.characters)
            {
                AddCharacter(character.identification.FetchEntityId());
            }
        }
    }

    public void OnLevelSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnLevelSelectValueChangedRpc(dropdown.value);
    }
    [Rpc(SendTo.Everyone)]
    private void OnLevelSelectValueChangedRpc(int index)
    {
        selectedLevel = LevelManager.availableLevels[index];
        selectedLevelTitleText.text = selectedLevel.displayName;
        selectedLevelDescriptionText.text = selectedLevel.description;

        InitializeMenu();
        if (IsHost) return;
        levelSelectDropdown.value = index;
    }

    public void OnGameModeSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnGameModeSelectValueChangedRpc(dropdown.value);
    }
    [Rpc(SendTo.Everyone)]
    private void OnGameModeSelectValueChangedRpc(int index)
    {
        selectedGameMode = GameModeHandler.availableGameModes.Values.ToList()[index];
        selectedGameModeTitleText.text = selectedGameMode.displayName;
        selectedGameModeDescriptionText.text = selectedGameMode.description;
        playerListTeamSeparatorObj.SetActive(selectedGameMode.teamBasedType == TeamBasedType.TEAM);

        foreach (LobbyCharacter lobbyPlayer in lobbyPlayers)
        {
            lobbyPlayer.UpdateTeamButtons(selectedGameMode.teamBasedType == TeamBasedType.TEAM);
        }

        InitializeMenu();
        if (IsHost) return;
        gameModeSelectDropdown.value = index;
    }

    public void OnMaxPlayersSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnMaxPlayersSelectValueChangedRpc(dropdown.value);
    }
    [Rpc(SendTo.Everyone)]
    private void OnMaxPlayersSelectValueChangedRpc(int index)
    {
        selectedMaxPlayers = int.Parse(maxPlayersSelectDropdown.options[index].text);

        if (IsHost) return;
        maxPlayersSelectDropdown.value = index;
    }

    public void OnTimeLimitSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnTimeLimitSelectValueChangedRpc(dropdown.value);
    }
    [Rpc(SendTo.Everyone)]
    private void OnTimeLimitSelectValueChangedRpc(int index)
    {
        selectedTimeLimit = int.Parse(timeLimitSelectDropdown.options[index].text);

        if (IsHost) return;
        timeLimitSelectDropdown.value = index;
    }

    public void OnObjectiveLimitSelectIncremented()
    {
        if (!IsHost) return;

        selectedWinConditionValue = Mathf.Min(666, selectedWinConditionValue + 1);
        OnObjectiveLimitSelectValueChangedRpc(selectedWinConditionValue);
    }

    public void OnObjectiveLimitSelectDecremented()
    {       
        if (!IsHost) return;

        selectedWinConditionValue = Mathf.Max(1, selectedWinConditionValue - 1);
        OnObjectiveLimitSelectValueChangedRpc(selectedWinConditionValue);
    }

    [Rpc(SendTo.Everyone)]
    private void OnObjectiveLimitSelectValueChangedRpc(int value)
    {
        selectedWinConditionValue = value;
        winConditionValueText.text = selectedWinConditionValue.ToString();
    }

    public void OnStartMatchButtonPressed()
    {
        if (!IsHost) return;

        lobbyPC.Reset();
        GameManager.Instance.SetLevel(selectedLevel.sceneName);

        GameModeData gameModeData = selectedGameMode.GetGameModeData(
            selectedMaxPlayers,
            selectedTimeLimit,
            selectedWinConditionValue
        );
        GameManager.Instance.SetGameModeData(gameModeData);
        GameManager.Instance.LoadLevel();
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
        int teamId = (selectedGameMode.teamBasedType == TeamBasedType.TEAM) ? (int)entityIdentification.FetchTeamId() : -1;

        AddCharacterRpc(characterId, playerName, teamId);
    }

    [Rpc(SendTo.Everyone)]
    private void AddCharacterRpc(ulong characterId, string playerName, int teamId)
    {
        Transform parentColumn = teamId == 0 ? playerListColumn0 : playerListColumn1;
        if (selectedGameMode.teamBasedType != TeamBasedType.TEAM)
        {
            parentColumn = playerListColumn0.childCount <= playerListColumn1.childCount ? playerListColumn0 : playerListColumn1;
            teamId = parentColumn == playerListColumn0 ? 0 : 1;
        }

        GameObject lobbyPlayerObj = Instantiate(lobbyPlayerPrefabObj, parentColumn);
        LobbyCharacter lobbyPlayer = lobbyPlayerObj.GetComponent<LobbyCharacter>();
        lobbyPlayer.Initialize(this, characterId, playerName, teamId, IsHost);
        lobbyPlayers.Add(lobbyPlayer);
    }

    private void RemoveCharacter(ulong characterId)
    {
        if (!NetworkManager.IsListening || !IsHost) return;
        RemoveCharacterRpc(characterId);
    }

    [Rpc(SendTo.Everyone)]
    private void RemoveCharacterRpc(ulong characterId)
    {
        LobbyCharacter lobbyPlayerToRemove = lobbyPlayers.Find(lp => lp.GetComponent<LobbyCharacter>().characterId == characterId);
        if (lobbyPlayerToRemove != null)
        {
            lobbyPlayers.Remove(lobbyPlayerToRemove);
            Destroy(lobbyPlayerToRemove.gameObject);
        }
    }

    public void TryChangeCharacterTeam(ulong characterId, int newTeam)
    {
        if (!IsHost) return;
        if (selectedGameMode.teamBasedType != TeamBasedType.TEAM) return;
        if ((newTeam == 0 ? playerListColumn0 : playerListColumn1).childCount >= selectedMaxPlayers / 2) return;
        ChangeCharacterTeamRpc(characterId, newTeam);
    }

    [Rpc(SendTo.Everyone)]
    private void ChangeCharacterTeamRpc(ulong characterId, int newTeam)
    {
        LobbyCharacter lobbyPlayer = lobbyPlayers.Find(lp => lp.GetComponent<LobbyCharacter>().characterId == characterId);
        if (lobbyPlayer != null)
        {
            lobbyPlayer.transform.SetParent(newTeam == 0 ? playerListColumn0 : playerListColumn1);
            lobbyPlayer.OnTeamChange(newTeam);
        }
    }
}
