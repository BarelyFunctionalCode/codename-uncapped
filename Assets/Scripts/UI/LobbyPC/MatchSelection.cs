using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class MatchSelection : CustomUIElementBase
{
    private LobbyPCController lobbyPCController;

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

        BuildStaticOptionsLists();
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