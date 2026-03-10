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

    private static List<string> levelNames = new();

    private string selectedLevel;
    private GameModes selectedGameMode;
    private int selectedMaxPlayers;
    private int selectedTimeLimit;

    [SerializeField] private LobbyPC lobbyPC;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

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
}
