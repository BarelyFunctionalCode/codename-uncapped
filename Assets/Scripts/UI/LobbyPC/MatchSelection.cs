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
    private List<LobbyPlayer> lobbyPlayers = new();

    private LevelSO selectedLevel;
    public GameModeSO selectedGameMode;
    private int selectedMaxPlayers;
    private int selectedTimeLimit;
    private int selectedWinConditionValue;

    [SerializeField] private LobbyPC lobbyPC;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsHost)
        {
            GameManager.Instance.OnClientConnectedEvent.AddListener(AddPlayer);
            GameManager.Instance.OnClientDisconnectedEvent.AddListener(RemovePlayer);
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
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                AddPlayer(client.ClientId);
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

        foreach (LobbyPlayer lobbyPlayer in lobbyPlayers)
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


    private void AddPlayer(ulong clientId)
    {
        if (!IsHost) return;

        LobbyPlayer lobbyPlayerToRemove = lobbyPlayers.Find(lp => lp.GetComponent<LobbyPlayer>().clientId == clientId);
        if (lobbyPlayerToRemove != null) return;

        PlayerController playerController = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerController>();
        if (!playerController.isInitialized) return;

        Identification entityIdentification = playerController.identification;

        string playerName = entityIdentification.FetchEntityName();
        int teamId = (selectedGameMode.teamBasedType == TeamBasedType.TEAM) ? (int)entityIdentification.FetchTeamId() : -1;

        AddPlayerRpc(clientId, playerName, teamId);
    }

    [Rpc(SendTo.Everyone)]
    private void AddPlayerRpc(ulong clientId, string playerName, int teamId)
    {
        Transform parentColumn = teamId == 0 ? playerListColumn0 : playerListColumn1;
        if (selectedGameMode.teamBasedType != TeamBasedType.TEAM)
        {
            parentColumn = playerListColumn0.childCount <= playerListColumn1.childCount ? playerListColumn0 : playerListColumn1;
            teamId = parentColumn == playerListColumn0 ? 0 : 1;
        }

        GameObject lobbyPlayerObj = Instantiate(lobbyPlayerPrefabObj, parentColumn);
        LobbyPlayer lobbyPlayer = lobbyPlayerObj.GetComponent<LobbyPlayer>();
        lobbyPlayer.Initialize(this, clientId, playerName, teamId, IsHost);
        lobbyPlayers.Add(lobbyPlayer);
    }

    private void RemovePlayer(ulong clientId)
    {
        if (!IsHost) return;
        RemovePlayerRpc(clientId);
    }

    [Rpc(SendTo.Everyone)]
    private void RemovePlayerRpc(ulong clientId)
    {
        LobbyPlayer lobbyPlayerToRemove = lobbyPlayers.Find(lp => lp.GetComponent<LobbyPlayer>().clientId == clientId);
        if (lobbyPlayerToRemove != null)
        {
            lobbyPlayers.Remove(lobbyPlayerToRemove);
            Destroy(lobbyPlayerToRemove.gameObject);
        }
    }

    public void TryChangePlayerTeam(ulong clientId, int newTeam)
    {
        if (!IsHost) return;
        if (selectedGameMode.teamBasedType != TeamBasedType.TEAM) return;
        if ((newTeam == 0 ? playerListColumn0 : playerListColumn1).childCount >= selectedMaxPlayers / 2) return;
        ChangePlayerTeamRpc(clientId, newTeam);
    }

    [Rpc(SendTo.Everyone)]
    private void ChangePlayerTeamRpc(ulong clientId, int newTeam)
    {
        LobbyPlayer lobbyPlayer = lobbyPlayers.Find(lp => lp.GetComponent<LobbyPlayer>().clientId == clientId);
        if (lobbyPlayer != null)
        {
            lobbyPlayer.transform.SetParent(newTeam == 0 ? playerListColumn0 : playerListColumn1);
            lobbyPlayer.OnTeamChange(newTeam);
        }
    }
}
