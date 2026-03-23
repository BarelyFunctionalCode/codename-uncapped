using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;


/*  TeamStructure handles teams of players
*      - Maintains list of teams and each players' assigned team
*      - Free-for-all games (no teams) = Each player is on its own team, where the team name = player name
*/

public class TeamStructure : NetworkBehaviour
{
    #region Properties

    // Team names, "Red" vs "Blue" for example.
    public NetworkList<FixedString128Bytes> teams;

    // Players are assigned here by PlayerHandler, referenced by their PlayerController.EntityId
    // Format: Dictionary[PlayerID, TeamIndex]
    public Dictionary<ulong, int> team_assignment = new();

    // Players may select a new team freely
    public bool AllowChangeTeams = true;

    // Players may only change teams by being assigned by a captain
    public bool ForceCaptainChangeTeams = false;
    #endregion

    #region Private methods
    private void EmitPlayerChangedTeam(EventArgsPlayerChangedTeam e)
    {
        // gameObject.BroadcastMessage("OnPlayerChangedTeam", e);
        EmitPlayerChangedTeamRpc(e);
    }

    [Rpc(SendTo.Everyone)]
    private void EmitPlayerChangedTeamRpc(EventArgsPlayerChangedTeam e)
    {
        // Notify all clients about the team change
        GameModeHandler.Instance.OnPlayerChangedTeam.Invoke(e);
    }

    #endregion

    #region public methods
    public string GetTeam(ulong player_id)
    {
        team_assignment.TryGetValue(player_id, out int teamIndex);
        string result = teamIndex >= 0 && teamIndex < teams.Count ? teams[teamIndex].ToString() : "No Team";

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

    // pseudo helper function
    public void SetPlayerTeamFFA(ulong entityId)
    {
        int teamIndex;
        if (!teams.Contains(entityId.ToString())) teamIndex = AddNewTeam(entityId.ToString());
        else teamIndex = GetTeamIndex(entityId.ToString());
        SetPlayerTeam(entityId, teamIndex);
    }

    // pseudo
    public void SetPlayerTeam(ulong player_id, int teamIndex)
    {
        if (team_assignment.ContainsKey(player_id) && team_assignment[player_id] == teamIndex) return;
        team_assignment[player_id] = teamIndex;
        EmitPlayerChangedTeam(new EventArgsPlayerChangedTeam(player_id, teamIndex));
    }

    public int AddNewTeam(string team)
    {
        teams.Add(team);
        return teams.Count - 1;
    }

    public void RemoveTeam(string team)
    {
        if (teams.Contains(team)) { teams.Remove(team); }
    }

    public void WipeTeams()
    {
        teams.Clear();
    }
    #endregion

    #region Message Receivers
    private void Awake()
    {
        teams = new NetworkList<FixedString128Bytes>(readPerm: NetworkVariableReadPermission.Everyone);
    }
    #endregion
}

public struct EventArgsPlayerChangedTeam : INetworkSerializeByMemcpy
{
    public ulong    player_id;
    public int    teamIndex;

    public EventArgsPlayerChangedTeam(ulong lPlayerId, int lTeamIndex)
    {
        player_id   = lPlayerId;
        teamIndex   = lTeamIndex;
    }
}
