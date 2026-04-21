using Unity.Netcode;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


/*  TeamStructure handles teams of players
*      - Maintains list of teams and each players' assigned team
*      - Free-for-all games (no teams) = Each player is on its own team, where the team name = player name
*/

public class TeamStructure : NetworkBehaviour
{
    protected NetworkList<FixedString32Bytes> teamNames = new();

    public uint AddTeam(string teamName)
    {
        if (teamNames.Contains(teamName)) return (uint)teamNames.IndexOf(teamName);

        teamNames.Add(teamName);
        return (uint)(teamNames.Count - 1);
    }

    public void ClearTeams()
    {
        teamNames.Clear();
        foreach (Character c in CharacterManager.Instance.characters) c.identification.SetTeamId(-1);
    }

    public int GetTeamByName(string teamName) => teamNames.IndexOf(teamName);
    public string GetTeamByIndex(int teamIndex) => teamNames[teamIndex].ToString();

    public bool AssignCharacterToTeam(Character character, int teamIndex = -1)
    {
        if (teamIndex == -1) teamIndex = (int)AddTeam(character.identification.FetchEntityName());
        if (teamIndex >= teamNames.Count) return false;

        character.identification.SetTeamId(teamIndex);
        return true;
    }

    public List<NetworkBehaviourReference> GetTeamMembers(int teamIndex)
    {
        return CharacterManager.Instance.GetCharactersByTeamId(teamIndex);
    }
    public List<NetworkBehaviourReference> GetTeamMembers(string teamName)
    {
        int teamIndex = GetTeamByName(teamName);
        return GetTeamMembers(teamIndex);
    }
    public int GetTeamWithFewestPlayers()
    {
        int fewestPlayers = int.MaxValue;
        int teamWithFewestPlayers = -1;

        for (int i = 0; i < teamNames.Count; i++)
        {
            int playerCount = GetTeamMembers(i).Count;
            if (playerCount < fewestPlayers)
            {
                fewestPlayers = playerCount;
                teamWithFewestPlayers = i;
            }
        }

        return teamWithFewestPlayers;
    }

}