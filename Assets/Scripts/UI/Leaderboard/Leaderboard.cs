using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardEntryPrefabObj;

    [SerializeField] private TMP_Text leaderboardTitleText;

    [SerializeField] private GameObject listColumn0Obj;
    [SerializeField] private TMP_Text listColumn0TitleText;
    [SerializeField] private GameObject listColumn1Obj;
    [SerializeField] private TMP_Text listColumn1TitleText;
    [SerializeField] private GameObject teamSeparatorObj;

    [SerializeField] private GameObject capturesStat0Obj;
    [SerializeField] private GameObject capturesStat1Obj;

    private List<LeaderboardEntry> entries = new();
    private bool isTeamBased = false;
    private bool enableCapturesStat;

    private bool isActive = false;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Initialize()
    {
        GameModeHandler.Instance.OnGameModeChanged.AddListener(SetGameModeData);
        GameModeHandler.Instance.OnStatUpdated.AddListener(OnStatEventReceived);
        GameModeHandler.Instance.OnPlayerChangedTeam.AddListener(AddEntry);
    }

    private void SetGameModeData(GameModes g)
    {
        TeamBasedType teamBasedType = GameModeHandler.Instance.FetchTeamBasedType(g);

        leaderboardTitleText.text = g.ToString();
        isTeamBased = teamBasedType == TeamBasedType.TEAM;

        listColumn0TitleText.text = isTeamBased ? "Team 1" : "Player";
        listColumn1TitleText.text = isTeamBased ? "Team 2" : "";
        listColumn1Obj.SetActive(isTeamBased);
        teamSeparatorObj.SetActive(isTeamBased);

        enableCapturesStat = false; // TODO: Change this to be based on the selected GameModeSO
        capturesStat0Obj.SetActive(enableCapturesStat);
        capturesStat1Obj.SetActive(enableCapturesStat);

        isActive = true;
    }

    public void ToggleMenu(bool enabled)
    {
        if (!isActive) return;
        gameObject.SetActive(enabled);
    }

    private void AddEntry(EventArgsPlayerChangedTeam e)
    {
        ulong playerId = e.player_id;
        LeaderboardEntry entryToRemove = entries.Find(entry => entry.playerId == playerId);
        if (entryToRemove != null) RemoveEntry(playerId);
        
        int teamIndex = e.teamIndex;
        Character character = CharacterManager.Instance.GetCharacterByCharacterId(playerId);
        if (character == null) return;
        string name = character.identification.FetchEntityName();
        GameObject entryObj = Instantiate(leaderboardEntryPrefabObj, (!isTeamBased || teamIndex == 0) ? listColumn0Obj.transform : listColumn1Obj.transform);
        LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();
        entry.Initialize(playerId, name, enableCapturesStat);
        entries.Add(entry);
    }

    private void RemoveEntry(ulong playerId)
    {
        LeaderboardEntry entryToRemove = entries.Find(entry => entry.playerId == playerId);
        if (entryToRemove != null)
        {
            Destroy(entryToRemove.gameObject);
            entries.Remove(entryToRemove);
        }
    }

    private void OnStatEventReceived(StatEvent statEvent)
    {
        LeaderboardEntry entryToUpdate = entries.Find(entry => entry.playerId == statEvent.Source);
        if (entryToUpdate != null) entryToUpdate.UpdateStats(statEvent);
    }

    public void ClearEntries()
    {
        foreach (LeaderboardEntry entry in entries)
        {
            Destroy(entry.gameObject);
        }
        entries.Clear();
    }


}
