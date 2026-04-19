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
    [SerializeField]
    public NetworkList<FixedString512Bytes> teams;

    // Default team names can be set in the unity inspector which will be used if custom team names aren't provided to `InitializeTeamNames`
    [SerializeField]
    public List<string> predefined_team_names;

    // Players are assigned here by PlayerHandler, referenced by their Character.EntityId
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

    private void RemoveTeamAssignments()
    {
        team_assignment.Clear();
    }
    #endregion

    #region public methods
    public string GetPlayersTeam(ulong player_id)
    {
        team_assignment.TryGetValue(player_id, out int teamIndex);
        string result = teamIndex >= 0 && teamIndex < teams.Count ? teams[teamIndex].ToString() : "No Team";

        return result;
    }

    public int GetTeamIndex(string team_name)
    {
        return GetTeams().IndexOf(team_name);
    }

    // Fetch a team from the list
    // NetworkList<FixedString512Bytes>[index]
    public string GetTeam(int index)
    {
        return teams[index].ToString();
    }

    public List<string> GetTeams()
    {
        List<string> x = new List<string>();
        foreach (FixedString512Bytes f in teams)
        {
            x.Add(f.ToString());
        }

        return x;
    }

    public void InitializeTeamNames(List<string> overrides)
    {
        List<string> consolidated_team_names;

        if (overrides.Count > 0)
        {   // We can send a custom list of team names to use
            consolidated_team_names = overrides;
        }
        else
        {   // Or use the team names predefined in the game mode
            consolidated_team_names = predefined_team_names;
        }

        foreach(string Name in consolidated_team_names)
        {
            AddNewTeam(Name);
        }
    }

    // FFA game modes force each player to be on their own team.
    // Their entityId will be their "team name" in the background.
    public void SetPlayerTeamFFA(ulong entityId)
    {
        int teamIndex;
        if (!teams.Contains(entityId.ToString())) teamIndex = AddNewTeam(entityId.ToString());
        else teamIndex = GetTeamIndex(entityId.ToString());
        SetPlayerTeam(entityId, teamIndex);
    }

    // Assign a player to a team.
    public void SetPlayerTeam(ulong player_id, int teamIndex)
    {
        // The player is already on this team, no-op
        if (team_assignment.ContainsKey(player_id) && team_assignment[player_id] == teamIndex) return;

        // Else, assign the player to the new team
        team_assignment[player_id] = teamIndex;
        EmitPlayerChangedTeam(new EventArgsPlayerChangedTeam(player_id, teamIndex));
    }

    public int AddNewTeam(string team)
    {
        teams.Add(new FixedString512Bytes(team));
//        teams[teams.Count - 1].ToString();
        return teams.Count - 1;
    }

    // Clear one team from the team list
    public void RemoveTeam(string team)
    {
        FixedString512Bytes cast_team_name = new FixedString512Bytes(team);
        if (teams.Contains(cast_team_name)) { teams.Remove(team); }
    }

    // Clear all teams from the team list and any player team assignments
    public void WipeTeams()
    {
        RemoveTeamAssignments();
        teams.Clear();
    }
    #endregion

    #region Message Receivers
    private void Awake()
    {
        teams = new NetworkList<FixedString512Bytes>(readPerm: NetworkVariableReadPermission.Everyone);
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
