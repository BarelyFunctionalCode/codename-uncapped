using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement(libraryPath = "Leaderboard/Leaderboard")]
public partial class Leaderboard : CustomUIElementBase
{
    private Label gameModeNameLabel;
    private VisualElement section0;
    private VisualElement section0Body;
    private Label section0NameLabel;
    private VisualElement section1;
    private VisualElement section1Body;
    private Label section1NameLabel;
    private VisualElement sectionSeparator;

    private VisualElement[] captureStatHeaders = new VisualElement[2];

    private List<LeaderboardEntry> entries = new();
    private bool isTeamBased = false;
    private bool enableCapturesStat = false;

    private bool isMenuActive = false;


    public void Initialize()
    {
        gameModeNameLabel = this.Q<Label>("GameModeName");
        section0 = this.Q<VisualElement>("Section0");
        section0Body = section0.Q<VisualElement>("SectionBody");
        section0NameLabel = section0.Query<VisualElement>("SectionHeader").Children<Label>("Name").First();
        section1 = this.Q<VisualElement>("Section1");
        section1Body = section1.Q<VisualElement>("SectionBody");
        section1NameLabel = section1.Query<VisualElement>("SectionHeader").Children<Label>("Name").First();
        sectionSeparator = this.Q<VisualElement>("SectionSeparator");   
        captureStatHeaders = this.Query<Label>(name: "Captures", className: "columnHeader").ToList().ToArray();

        EnableInClassList("active-menu", false);

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

    public bool ToggleMenu(bool isActive)
    {
        isMenuActive = isActive;
        EnableInClassList("active-menu", isMenuActive);
        pickingMode = isMenuActive ? PickingMode.Position : PickingMode.Ignore;
        if (isMenuActive) BringToFront();
        return isMenuActive;
    }

    public void SetGameModeData(GameModes g)
    {
        TeamBasedType teamBasedType = GameModeHandler.Instance.FetchTeamBasedType(g);

        string gameModeName = g.ToString();
        bool isTeamBased = teamBasedType == TeamBasedType.TEAM;
        bool enableCapturesStat = false; // TODO: Change this to be based on the selected GameModeSO

        string section0Name = isTeamBased ? GameModeHandler.Instance.currentGameMode.TeamStructure.GetTeamByIndex(0) : "Player";
        string section1Name = isTeamBased ? GameModeHandler.Instance.currentGameMode.TeamStructure.GetTeamByIndex(1) : "";

        gameModeNameLabel.text = gameModeName;
        section0NameLabel.text = section0Name;
        section1NameLabel.text = section1Name;
        this.isTeamBased = isTeamBased;
        this.enableCapturesStat = enableCapturesStat;

        section1.style.display = isTeamBased ? DisplayStyle.Flex : DisplayStyle.None;
        sectionSeparator.style.display = isTeamBased ? DisplayStyle.Flex : DisplayStyle.None;

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

    public void AddEntry(NetworkBehaviourReference characterRef)
    {
        characterRef.TryGet(out Character character);
        if (character == null) return;
        
        ulong characterId = character.identification.FetchEntityId();
        int teamIndex = character.identification.FetchTeamId();
        string name = character.identification.FetchEntityName();

        if (teamIndex == -1) return;

        LeaderboardEntry entryToRemove = entries.Find(entry => entry.characterId == characterId);
        if (entryToRemove != null) RemoveEntry(characterId);
        
        VisualElement parent = (!isTeamBased || teamIndex == 0) ? section0Body : section1Body;
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

[UxmlElement(libraryPath = "Leaderboard/LeaderboardEntry")]
public partial class LeaderboardEntry : CustomUIElementBase
{
    public ulong characterId;

    private Label nameLabel;
    private Label killsLabel;
    private Label deathsLabel;
    private Label assistsLabel;
    private Label capturesLabel;

    public int sortingIndex = 0;

    private Dictionary<StatEventType, Label> statLabels;
    private Dictionary<StatEventType, int> statValues;

    public void Initialize(ulong characterId, string name, bool enableCapturesStat = false, int kills = 0, int deaths = 0, int assists = 0, int captures = 0)
    {
        nameLabel = this.Q<Label>("Name");
        killsLabel = this.Q<Label>("Kills");
        deathsLabel = this.Q<Label>("Deaths");
        assistsLabel = this.Q<Label>("Assists");
        capturesLabel = this.Q<Label>("Captures");

        this.characterId = characterId;
        nameLabel.text = name;
        killsLabel.text = kills.ToString();
        deathsLabel.text = deaths.ToString();
        assistsLabel.text = assists.ToString();
        capturesLabel.text = captures.ToString();
        capturesLabel.style.display = enableCapturesStat ? DisplayStyle.Flex : DisplayStyle.None;

    statLabels = new()
        {
            { StatEventType.KILL, this.killsLabel },
            { StatEventType.DEATHS, this.deathsLabel },
            { StatEventType.KILL_ASSIST, assistsLabel },
            { StatEventType.FLAG_CAPTURE, capturesLabel }
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