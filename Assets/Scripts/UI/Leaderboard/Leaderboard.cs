using System.Collections.Generic;
using TMPro;
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
    }

    public void ToggleMenu(bool enabled)
    {
        gameObject.SetActive(enabled);
    }

    public void AddEntry(ulong playerId, string name, int teamIndex)
    {
        GameObject entryObj = Instantiate(leaderboardEntryPrefabObj, teamIndex == 0 ? listColumn0Obj.transform : listColumn1Obj.transform);
        LeaderboardEntry entry = entryObj.GetComponent<LeaderboardEntry>();
        entry.Initialize(playerId, name, enableCapturesStat);
        entries.Add(entry);
    }

    public void RemoveEntry(ulong playerId)
    {
        LeaderboardEntry entryToRemove = entries.Find(entry => entry.playerId == playerId);
        if (entryToRemove != null)
        {
            Destroy(entryToRemove.gameObject);
            entries.Remove(entryToRemove);
        }
    }

    public void UpdateEntryStats(ulong playerId, int kills, int deaths, int assists, int captures = 0)
    {
        LeaderboardEntry entryToUpdate = entries.Find(entry => entry.playerId == playerId);
        if (entryToUpdate != null)
        {
            entryToUpdate.UpdateStats(kills, deaths, assists, captures);
        }
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
