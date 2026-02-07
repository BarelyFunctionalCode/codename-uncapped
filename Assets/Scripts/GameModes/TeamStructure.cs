using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using System.Collections.Generic;


/*  TeamStructure handles teams of players
*      - Maintains list of teams and each players' assigned team
*      - Checks for friendly fire, which can be a toggled option for server hosts to allow/disallow friendly fire
*      - Free-for-all games (no teams) = Each player is on its own team, where the team name = player name
*/

public class TeamStructure : NetworkBehaviour
{
    #region Delegates & Events
    public event EventHandler<EventArgsPlayerChangedTeam> PlayerChangedTeam;
    #endregion

    #region Properties
    // Team names, "Red" vs "Blue" for example.
    public NetworkList<FixedString128Bytes> teams;

    // Players are assigned here by PlayerHandler, referenced by their instance ID
    // Format: Dictionary[PlayerID, TeamName]
    public Dictionary<int, string> team_assignment = new Dictionary<int, string>();

    // Players may select a new team
    public bool AllowChangeTeams = true;

    // Players may change teams by being assigned by a captain
    public bool ForceCaptainChangeTeams = false;
    #endregion

    /*
     * IsEnemies should be moved to EntityManager, where TeamStructure
     * can provide Team information to it with `GetTeam()`
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
    */

    private void Awake()
    {
        teams = new NetworkList<FixedString128Bytes>(readPerm: NetworkVariableReadPermission.Everyone);
    }

    public string GetTeam (int player_id)
    {
        string result;
        team_assignment.TryGetValue(player_id, out result);

        return result;
    }

    public int GetTeamIndex(string team_name)
    {
        return GetTeams().IndexOf(team_name);
    }

    public List<string> GetTeams()
    {
        List<string> x = new List<string>();
        foreach (FixedString128Bytes f in teams)
        {
            x.Add(f.ToString());
        }

        return x;
    }


    // pseudo
    public void SetPlayerTeam (int player_id, string team)
    {
        team_assignment[player_id] = team;
        OnPlayerChangedTeam(new EventArgsPlayerChangedTeam(player_id, team));
    }

    public void AddNewTeam(string team)
    {
        teams.Add(team);
    }

    public void RemoveTeam(string team)
    {
        if (teams.Contains(team)) { teams.Remove(team); }
    }

    public void WipeTeams()
    {
        teams.Clear();
    }

    #region Event Handlers
    public void OnPlayerChangedTeam(EventArgsPlayerChangedTeam e)
    {
        PlayerChangedTeam?.Invoke(this, e);
    }
    #endregion
}

public class EventArgsPlayerChangedTeam : EventArgs
{
    public int      player_id   { get; set; }
    public string   team        {get; set; }

    public EventArgsPlayerChangedTeam(int lPlayerId, string lTeam)
    {
        player_id   = lPlayerId;
        team        = lTeam;
    }
}
