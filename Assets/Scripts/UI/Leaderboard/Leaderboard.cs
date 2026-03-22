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
    private bool enableCapturesStat;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Initialize(string title, string column0Title = "Player", string column1Title = "", bool enableCapturesStat = false)
    {
        leaderboardTitleText.text = title;
        listColumn0TitleText.text = column0Title;
        listColumn1TitleText.text = column1Title;
        listColumn1Obj.SetActive(!string.IsNullOrEmpty(column1Title));
        teamSeparatorObj.SetActive(!string.IsNullOrEmpty(column1Title));

        this.enableCapturesStat = enableCapturesStat;
        capturesStat0Obj.SetActive(enableCapturesStat);
        capturesStat1Obj.SetActive(enableCapturesStat);

        GameModeHandler.Instance.OnStatUpdated.AddListener(OnStatEventReceived);
        GameModeHandler.Instance.OnPlayerChangedTeam.AddListener(AddEntry);
    }

    public void ToggleMenu(bool enabled)
    {
        gameObject.SetActive(enabled);
    }

    private void AddEntry(EventArgsPlayerChangedTeam e)
    {
        ulong playerId = e.player_id;
        LeaderboardEntry entryToRemove = entries.Find(entry => entry.playerId == playerId);
        if (entryToRemove != null) RemoveEntry(playerId);
        
        int teamIndex = int.Parse(e.team);
        string name = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject.GetComponent<PlayerController>().EntityName;
        GameObject entryObj = Instantiate(leaderboardEntryPrefabObj, teamIndex == 0 ? listColumn0Obj.transform : listColumn1Obj.transform);
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
