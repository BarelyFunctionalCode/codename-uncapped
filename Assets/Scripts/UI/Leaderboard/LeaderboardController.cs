using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class Leaderboard : CustomUIElementBase
{
    private Label GameModeName => this.Q<Label>("GameModeName");
    private VisualElement Section0 => this.Q<VisualElement>("Section0");
    private VisualElement Section0Body => Section0.Q<VisualElement>("SectionBody");
    private Label Section0Name => Section0.Query<VisualElement>("SectionHeader").Children<Label>("Name").First();
    private VisualElement Section1 => this.Q<VisualElement>("Section1");
    private VisualElement Section1Body => Section1.Q<VisualElement>("SectionBody");
    private Label Section1Name => Section1.Query<VisualElement>("SectionHeader").Children<Label>("Name").First();
    private VisualElement SectionSeparator => this.Q<VisualElement>("SectionSeparator");

    private VisualElement[] captureStatHeaders = new VisualElement[2];

    private List<LeaderboardEntry> entries = new();
    private bool isTeamBased = false;
    private bool enableCapturesStat = false;


    public void Initialize(string gameModeName, string section0Name, string section1Name, bool isTeamBased, bool enableCapturesStat)
    {
        captureStatHeaders = this.Query<Label>(name: "Captures", className: "columnHeader").ToList().ToArray();
        GameModeName.text = gameModeName;
        Section0Name.text = section0Name;
        Section1Name.text = section1Name;
        this.isTeamBased = isTeamBased;
        this.enableCapturesStat = enableCapturesStat;

        Section1.style.display = isTeamBased ? DisplayStyle.Flex : DisplayStyle.None;
        SectionSeparator.style.display = isTeamBased ? DisplayStyle.Flex : DisplayStyle.None;

        foreach (var captureStatHeader in captureStatHeaders)
        {
            captureStatHeader.style.display = enableCapturesStat ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void OnStatEventReceived(StatEvent statEvent)
    {
        LeaderboardEntry entryToUpdate = entries.Find(entry => entry.characterId == statEvent.Source);
        entryToUpdate?.UpdateStats(statEvent);
        SortEntries();
    }

    private void SortEntries()
    {
        entries.Sort((a, b) => b.sortingIndex.CompareTo(a.sortingIndex));

        foreach (LeaderboardEntry entry in entries)
        {
            entry.BringToFront();
        }
    }

    public void AddEntry(ulong characterId, int teamIndex, string name)
    {
        if (teamIndex == -1) return;

        LeaderboardEntry entryToRemove = entries.Find(entry => entry.characterId == characterId);
        if (entryToRemove != null) RemoveEntry(characterId);
        
        VisualElement parent = (!isTeamBased || teamIndex == 0) ? Section0Body : Section1Body;
        LeaderboardEntry entry = (LeaderboardEntry)UIManager.Spawn("ui/Leaderboard/LeaderboardEntry", parent);
        entry.Initialize(characterId, name, enableCapturesStat);
        entries.Add(entry);
        SortEntries();
    }

    private void RemoveEntry(ulong characterId)
    {
        LeaderboardEntry entryToRemove = entries.Find(entry => entry.characterId == characterId);
        if (entryToRemove != null)
        {
            entryToRemove.RemoveFromHierarchy();
            entries.Remove(entryToRemove);
        }
    }

    public void ClearEntries()
    {
        foreach (LeaderboardEntry entry in entries)
        {
            entry.RemoveFromHierarchy();
        }
        entries.Clear();
    }
}

[UxmlElement]
public partial class LeaderboardEntry : CustomUIElementBase
{
    public ulong characterId;

    private Label Name => this.Q<Label>("Name");
    private Label Kills => this.Q<Label>("Kills");
    private Label Deaths => this.Q<Label>("Deaths");
    private Label Assists => this.Q<Label>("Assists");
    private Label Captures => this.Q<Label>("Captures");

    public int sortingIndex = 0;

    private Dictionary<StatEventType, Label> statLabels;
    private Dictionary<StatEventType, int> statValues;

    public void Initialize(ulong characterId, string name, bool enableCapturesStat = false, int kills = 0, int deaths = 0, int assists = 0, int captures = 0)
    {
        this.characterId = characterId;
        Name.text = name;
        Kills.text = kills.ToString();
        Deaths.text = deaths.ToString();
        Assists.text = assists.ToString();
        Captures.text = captures.ToString();
        Captures.style.display = enableCapturesStat ? DisplayStyle.Flex : DisplayStyle.None;

        statLabels = new()
        {
            { StatEventType.KILL, Kills },
            { StatEventType.DEATHS, Deaths },
            { StatEventType.KILL_ASSIST, Assists },
            { StatEventType.FLAG_CAPTURE, Captures }
        };

        statValues = new()
        {
            { StatEventType.KILL, kills },
            { StatEventType.DEATHS, deaths },
            { StatEventType.KILL_ASSIST, assists },
            { StatEventType.FLAG_CAPTURE, captures }
        };
    }

    public void UpdateStats(StatEvent statEvent)
    {
        if (statLabels.TryGetValue(statEvent.StatType, out Label label))
        {
            statValues[statEvent.StatType] = (int)statEvent.Value;
            label.text = statEvent.Value.ToString();
        }

        sortingIndex = statValues[StatEventType.KILL] + 
                        Mathf.CeilToInt(statValues[StatEventType.KILL_ASSIST] * 0.5f) + 
                        (int)(statValues[StatEventType.FLAG_CAPTURE] * 2f);
    }
}


public class LeaderboardController : MonoBehaviour
{
    private UIDocument leaderboardUIDocument;
    private Leaderboard leaderboard;
    private bool isActive = false;


    public void Initialize()
    {
        leaderboardUIDocument = GetComponent<UIDocument>();
        leaderboard = leaderboardUIDocument.rootVisualElement.Q<Leaderboard>();
        leaderboard.style.display = DisplayStyle.None;

        GameModeHandler.Instance.OnGameModeChanged.AddListener(SetGameModeData);
        GameModeHandler.Instance.TriggerGameModeUpdateRpc(NetworkManager.Singleton.LocalClientId);

        GameModeHandler.Instance.OnStatUpdated.AddListener(OnStatEventReceived);
        GameModeHandler.Instance.TriggerCharactersStatsDumpRpc(NetworkManager.Singleton.LocalClientId);

        CharacterManager.Instance.OnCharacterChangedTeam.AddListener(AddEntry);
        foreach (Character character in CharacterManager.Instance.characters)
        {
            AddEntry(new NetworkBehaviourReference(character));
        }
    }

    public void Deinitialize()
    {
        if (GameModeHandler.Instance)
        {
            GameModeHandler.Instance.OnGameModeChanged.RemoveListener(SetGameModeData);
            GameModeHandler.Instance.OnStatUpdated.RemoveListener(OnStatEventReceived);
        }
        if (CharacterManager.Instance)
        {
            CharacterManager.Instance.OnCharacterChangedTeam.RemoveListener(AddEntry);
        }
        ClearEntries();
    }

    public void ToggleMenu(bool enabled)
    {
        if (!isActive) return;
        leaderboard.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetGameModeData(GameModes g)
    {
        TeamBasedType teamBasedType = GameModeHandler.Instance.FetchTeamBasedType(g);

        string gameModeName = g.ToString();
        bool isTeamBased = teamBasedType == TeamBasedType.TEAM;
        bool enableCapturesStat = false; // TODO: Change this to be based on the selected GameModeSO

        string section0Name = isTeamBased ? GameModeHandler.Instance.currentGameMode.TeamStructure.GetTeamByIndex(0) : "Player";
        string section1Name = isTeamBased ? GameModeHandler.Instance.currentGameMode.TeamStructure.GetTeamByIndex(1) : "";

        leaderboard.Initialize(gameModeName, section0Name, section1Name, isTeamBased, enableCapturesStat);

        isActive = true;
    }

    private void OnStatEventReceived(StatEvent statEvent) => leaderboard.OnStatEventReceived(statEvent);

    private void AddEntry(NetworkBehaviourReference characterRef)
    {
        characterRef.TryGet(out Character character);
        if (character == null) return;
        
        ulong characterId = character.identification.FetchEntityId();
        int teamIndex = character.identification.FetchTeamId();
        string name = character.identification.FetchEntityName();
        leaderboard.AddEntry(characterId, teamIndex, name);
    }

    private void ClearEntries() => leaderboard.ClearEntries();
}