using TMPro;
using UnityEngine;

public class LobbyPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private GameObject leftArrowButtonObj;
    [SerializeField] private GameObject rightArrowButtonObj;

    private MatchSelection matchSelection;
    public ulong clientId;
    private int teamIndex = -1;


    public void Initialize(MatchSelection matchSelection, ulong id, string name, int team, bool isHost = false)
    {
        this.matchSelection = matchSelection;
        clientId = id;
        playerNameText.text = name;
        teamIndex = team;
    
        if (!isHost) return;
        if (teamIndex == 0) leftArrowButtonObj.SetActive(true);
        else if (teamIndex == 1) rightArrowButtonObj.SetActive(true);
    }

    public void OnTeamChangeButtonPressed(int team)
    {
        if (team == teamIndex) return;
        matchSelection.TryChangePlayerTeam(clientId, team);
    }

    public void OnTeamChange(int newTeam)
    {
        teamIndex = newTeam;
        leftArrowButtonObj.SetActive(teamIndex != 0);
        rightArrowButtonObj.SetActive(teamIndex != 1);
    }
}
