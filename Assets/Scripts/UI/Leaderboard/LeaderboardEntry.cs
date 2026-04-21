using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private TMP_Text capturesText;

    public ulong characterId;
    private Dictionary<StatEventType, Action<float>> statUpdaters = new();

    public void Initialize(ulong playerId, string name, bool enableCapturesStat)
    {
        statUpdaters = new()
        {
            { StatEventType.KILL,         (value) => { killsText.text = value.ToString(); } },
            { StatEventType.DEATHS,       (value) => { deathsText.text = value.ToString(); } },
            { StatEventType.KILL_ASSIST,  (value) => { assistsText.text = value.ToString(); } },
            { StatEventType.FLAG_CAPTURE, (value) => { capturesText.text = value.ToString(); } },
        };

        this.characterId = playerId;
        nameText.text = name;
        statUpdaters[StatEventType.KILL](0);
        statUpdaters[StatEventType.DEATHS](0);
        statUpdaters[StatEventType.KILL_ASSIST](0);
        statUpdaters[StatEventType.FLAG_CAPTURE](0);
        capturesText.gameObject.SetActive(enableCapturesStat);
    }

    public void UpdateStats(StatEvent statEvent)
    {
        if (statUpdaters.ContainsKey(statEvent.StatType)) statUpdaters[statEvent.StatType](statEvent.Value);
    }
}
