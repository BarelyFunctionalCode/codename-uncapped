using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private TMP_Text capturesText;

    public ulong playerId;

    public void Initialize(ulong playerId, string name, bool enableCapturesStat)
    {
        this.playerId = playerId;
        nameText.text = name;
        killsText.text = "0";
        deathsText.text = "0";
        assistsText.text = "0";
        capturesText.text = "0";
        capturesText.gameObject.SetActive(enableCapturesStat);
    }

    public void UpdateStats(int kills, int deaths, int assists, int captures = 0)
    {
        killsText.text = kills.ToString();
        deathsText.text = deaths.ToString();
        assistsText.text = assists.ToString();
        if (capturesText.gameObject.activeSelf)
        {
            capturesText.text = captures.ToString();
        }
    }
}
