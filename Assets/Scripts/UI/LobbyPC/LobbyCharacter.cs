using TMPro;
using UnityEngine;

public class LobbyCharacter : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private GameObject leftArrowButtonObj;
    [SerializeField] private GameObject rightArrowButtonObj;

    private MatchSelection matchSelection;
    public ulong characterId;
    private int teamIndex = -1;


    public void Initialize(MatchSelection matchSelection, ulong id, string name, int team, bool isHost = false)
    {
        this.matchSelection = matchSelection;
        characterId = id;
        playerNameText.text = name;
        teamIndex = team;
    
        if (!isHost) return;

        if (matchSelection.selectedGameMode.teamBasedType != TeamBasedType.TEAM)
        {
            leftArrowButtonObj.SetActive(false);
            rightArrowButtonObj.SetActive(false);
        }
        else
        {
            if (teamIndex == 0) rightArrowButtonObj.SetActive(true);
            else if (teamIndex == 1) leftArrowButtonObj.SetActive(true);
        }
    }

    public void UpdateTeamButtons(bool isTeamBased)
    {
        if (!isTeamBased)
        {
            leftArrowButtonObj.SetActive(false);
            rightArrowButtonObj.SetActive(false);
        }
        else
        {
            leftArrowButtonObj.SetActive(teamIndex == 1);
            rightArrowButtonObj.SetActive(teamIndex == 0);
        }
    }

    public void OnTeamChangeButtonPressed(int team)
    {
        if (team == teamIndex) return;
        matchSelection.TryChangeCharacterTeam(characterId, team);
    }

    public void OnTeamChange(int newTeam)
    {
        teamIndex = newTeam;
        leftArrowButtonObj.SetActive(teamIndex == 1);
        rightArrowButtonObj.SetActive(teamIndex == 0);
    }
}
