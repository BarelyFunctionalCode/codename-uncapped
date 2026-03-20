using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchSelection : NetworkBehaviour
{
    [SerializeField] private TMP_Dropdown levelSelectDropdown;
    [SerializeField] private TMP_Dropdown gameModeSelectDropdown;
    [SerializeField] private TMP_Dropdown maxPlayersSelectDropdown;
    [SerializeField] private TMP_Dropdown timeLimitSelectDropdown;

    [SerializeField] private TMP_Text selectedLevelTitleText;
    [SerializeField] private TMP_Text selectedLevelDescriptionText;

    [SerializeField] private TMP_Text selectedGameModeTitleText;
    [SerializeField] private TMP_Text selectedGameModeDescriptionText;


    [SerializeField] private GameObject playerListTeamSeparatorObj;
    [SerializeField] private Transform playerListColumn0;
    [SerializeField] private Transform playerListColumn1;
    [SerializeField] private GameObject lobbyPlayerPrefabObj;
    private List<LobbyPlayer> lobbyPlayers = new();

    private static List<string> levelNames = new();

    private string selectedLevel;
    private GameModes selectedGameMode;
    private bool isGameModeTeamBased = false; // TODO: Change this to be based on the selected GameModeSO
    private int selectedMaxPlayers;
    private int selectedTimeLimit;

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
        if (levelNames.Count == 0) // TODO: Change this out for LevelSO that will have nice title and description.
        {
            string fullPath = Path.Combine(Application.dataPath, "Scenes/Levels");
            string[] sceneFiles = Directory.GetFiles(fullPath, "*.unity");
            foreach (string file in sceneFiles)
            {
                string fileName = Path.GetFileName(file);
                Debug.Log("Scene file: " + fileName);
                levelNames.Add(Path.GetFileNameWithoutExtension(fileName));
            }
        }
        selectedLevel = levelNames[0];
        selectedLevelTitleText.text = selectedLevel; // TODO: Change this to be the title from the LevelSO
        selectedLevelDescriptionText.text = "This is a description for " + selectedLevel; // TODO: Change this to be the description from the LevelSO
        levelSelectDropdown.AddOptions(levelNames);

        List<string> gameModeNames = new();
        bool isFirst = true;
        foreach (GameModes gameMode in System.Enum.GetValues(typeof(GameModes)))
        {
            if (isFirst) 
            {
                isFirst = false;
                continue; // Skip None
            }
            
            gameModeNames.Add(gameMode.ToString());
        }
        selectedGameMode = (GameModes)1;
        selectedGameModeTitleText.text = selectedGameMode.ToString(); // TODO: Change this to be the title from the GameModeSO
        selectedGameModeDescriptionText.text = "This is a description for " + selectedGameMode; // TODO: Change this to be the description from the GameModeSO
        gameModeSelectDropdown.AddOptions(gameModeNames);

        List<string> maxPlayerOptions = new() { "2", "4", "8", "16" }; // TODO: Change this to be dynamic based on the selected level and gamemode
        selectedMaxPlayers = int.Parse(maxPlayerOptions[0]);
        maxPlayersSelectDropdown.AddOptions(maxPlayerOptions);

        List<string> timeLimitOptions = new() { "5", "10", "15", "20", "30", "60" }; // TODO: Change this to be dynamic based on the selected level and gamemode
        selectedTimeLimit = int.Parse(timeLimitOptions[0]);
        timeLimitSelectDropdown.AddOptions(timeLimitOptions);

        // TODO: For Objective based gamemodes, add dropdown for objective selection that is populated based on the selected level.

        if (!IsHost)
        {
            levelSelectDropdown.interactable = false;
            gameModeSelectDropdown.interactable = false;
            maxPlayersSelectDropdown.interactable = false;
            timeLimitSelectDropdown.interactable = false;
        }

        playerListTeamSeparatorObj.SetActive(isGameModeTeamBased);
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
        selectedLevel = levelNames[index];
        selectedLevelTitleText.text = selectedLevel; // TODO: Change this to be the title from the LevelSO
        selectedLevelDescriptionText.text = "This is a description for " + selectedLevel; // TODO: Change this to be the description from the LevelSO

        if (IsHost) return;
        levelSelectDropdown.value = index;
    }

    public void OnGameModeSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnGameModeSelectValueChangedRpc(dropdown.value + 1); // Account for None value in enum
    }
    [Rpc(SendTo.Everyone)]
    private void OnGameModeSelectValueChangedRpc(int index)
    {
        selectedGameMode = (GameModes)index;
        selectedGameModeTitleText.text = selectedGameMode.ToString(); // TODO: Change this to be the title from the GameModeSO
        selectedGameModeDescriptionText.text = "This is a description for " + selectedGameMode; // TODO: Change this to be the description from the GameModeSO
        isGameModeTeamBased = false; // TODO: Change this to be based on the selected GameModeSO
        playerListTeamSeparatorObj.SetActive(isGameModeTeamBased);

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

    public void OnStartMatchButtonPressed()
    {
        if (!IsHost) return;

        Debug.Log($"Starting match with settings: Level - {selectedLevel}, Game Mode - {selectedGameMode}, Max Players - {selectedMaxPlayers}, Time Limit - {selectedTimeLimit} minutes");
        lobbyPC.Reset();
        GameManager.Instance.SetLevel(selectedLevel);
        GameManager.Instance.SetGameMode(selectedGameMode);
        GameManager.Instance.LoadLevel();
    }


    private void AddPlayer(ulong clientId)
    {
        if (!IsHost) return;

        Debug.Log("Adding player for clientId: " + clientId);

        LobbyPlayer lobbyPlayerToRemove = lobbyPlayers.Find(lp => lp.GetComponent<LobbyPlayer>().clientId == clientId);
        if (lobbyPlayerToRemove != null) return;

        PlayerController playerController = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerController>();
        if (!playerController.isInitialized) return;

        string playerName = playerController.EntityName;
        int teamId = isGameModeTeamBased ? (int)playerController.TeamId : -1;

        Debug.Log("Adding player with name: " + playerName + " to team: " + teamId);
        AddPlayerRpc(clientId, playerName, teamId);
    }

    [Rpc(SendTo.Everyone)]
    private void AddPlayerRpc(ulong clientId, string playerName, int teamId)
    {


        Transform parentColumn = teamId == 0 ? playerListColumn0 : playerListColumn1;
        if (!isGameModeTeamBased) parentColumn = playerListColumn0.childCount <= playerListColumn1.childCount ? playerListColumn0 : playerListColumn1;

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
        // TODO: Check to see if the team change is valid based on the current game mode and team sizes before sending the RPC
        ChangePlayerTeamRpc(clientId, newTeam);
    }

    [Rpc(SendTo.Everyone)]
    private void ChangePlayerTeamRpc(ulong clientId, int newTeam)
    {
        if (!isGameModeTeamBased) return;
        LobbyPlayer lobbyPlayer = lobbyPlayers.Find(lp => lp.GetComponent<LobbyPlayer>().clientId == clientId);
        if (lobbyPlayer != null)
        {
            lobbyPlayer.transform.SetParent(newTeam == 0 ? playerListColumn0 : playerListColumn1);
            lobbyPlayer.OnTeamChange(newTeam);
        }
    }
}
