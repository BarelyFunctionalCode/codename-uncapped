using UnityEngine;
using System.Collections.Generic;

/*  TeamStructure handles teams of players
*      - Maintains list of teams and each players' assigned team
*      - Checks for friendly fire, which can be a toggled option for server hosts to allow/disallow friendly fire
*      - Free-for-all games (no teams) = Each player is on its own team, where the team name = player name
*/

public class TeamStructure : MonoBehaviour
{
    // Team names, "Red" vs "Blue" or empty for a free-for-all
    [SerializeField]
    public List<string> teams = new List<string> {};

    // Players are assigned here by PlayerHandler, referenced by their instance ID
    // Format: Dictionary[Player, TeamName]
    public Dictionary<int, string> team_assignment = new Dictionary<int, string>();

    // Check if the acting player is on the enemy team of receiving player.
    // By default, a FFA match has no teams and therefore will always return true.
    public bool IsEnemies (int acting_player_id, int receiving_player_id)
    {
        bool result = true;

        if (teams.Count > 0)
        {
            string ActingTeam = GetTeam(acting_player_id);
            string RcvingTeam = GetTeam(receiving_player_id);

            // Test if they are enemies - Not(Same team)
            result = !(ActingTeam == RcvingTeam);
        }

        return result;

    }

    public string GetTeam (int player_id)
    {
        string result;
        team_assignment.TryGetValue(player_id, out result);

        return result;
    }

}
