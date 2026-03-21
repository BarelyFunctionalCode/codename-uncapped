using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Events;


/*  TeamStructure handles teams of players
*      - Maintains list of teams and each players' assigned team
*      - Free-for-all games (no teams) = Each player is on its own team, where the team name = player name
*/

public class TeamStructure : NetworkBehaviour
{
    #region Properties
    public UnityEvent<EventArgsPlayerChangedTeam> OnPlayerChangedTeam = new();

    // Team names, "Red" vs "Blue" for example.
    public NetworkList<FixedString128Bytes> teams;

    // Players are assigned here by PlayerHandler, referenced by their PlayerController.EntityId
    // Format: Dictionary[PlayerID, TeamName]
    public Dictionary<ulong, string> team_assignment = new Dictionary<ulong, string>();

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
        OnPlayerChangedTeam.Invoke(e);
    }

    #endregion

    #region public methods
    public string GetTeam(ulong player_id)
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

    // pseudo helper function
    public void SetPlayerTeamFFA(ulong entityId)
    {
        SetPlayerTeam(entityId, entityId.ToString());
    }

    // pseudo
    public void SetPlayerTeam(ulong player_id, string team)
    {
        if (team_assignment.ContainsKey(player_id) && team_assignment[player_id] == team) return;
        team_assignment[player_id] = team;
        EmitPlayerChangedTeam(new EventArgsPlayerChangedTeam(player_id, team));
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
    public ulong    player_id   { get; set; }
    public string   team        { get; set; }

    public EventArgsPlayerChangedTeam(ulong lPlayerId, string lTeam)
    {
        player_id   = lPlayerId;
        team        = lTeam;
    }
}
