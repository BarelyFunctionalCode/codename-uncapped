using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum StatsGroup
{
    NONE,
    TEAM,
    PLAYER
}

/*
 *  GameStats handles all StatEvents
 *      - All events in game will be tracked as a "StatEvent"
 *        - Bullets fired count toward a players' hitrate/missrate
 *        - Flag captures, drops, recoveries
 *        - Kills, deaths, assists
 *      - All game modes are point based, where points are given based on a certain type of StatEvent
 *        - Kills award points for Death match
 *        - Flag captures award points for CTF
 *        - Flag hold duration award points for rabbit
 *      - Points are awarded to players
 *        - Points are also awarded to teams based on which team the player is on
 *        - Player points do nothing but track stats
 *        - Team points trigger state change/win condition
 *          - This is to prevent players switching teams and triggering win condition with player points
 *      - Points accumulate to Team the player is on, if team based
 *      - On point change, check win condition
 *      - GameStat
*/


public class GameStats : NetworkBehaviour
{
    #region Properties

    /*
     * Vec4 Layout
     * [ StatsGroupID, TrackerID, StatEventTypeID, value ]
     *
     * Example
     * Vector4[
     *  StatsGroup.PLAYER,  // Stat entry is a specific player
     *  0,                  // Player is the server ( id = 0 )
     *  StatEventType.KILL, // Stat entry is for kill count
     *  5,                  // Server has 5 kills
     * ]
     */

    private NetworkList<StatTracker> points = new NetworkList<StatTracker>();
    #endregion

    #region Public Methods
    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEvent s)
    {
        NetworkList<Vector4> points = FetchStats();

        ulong player_id = s.Source;
        string team_name = gameObject.GetComponent<TeamStructure>().GetTeam(player_id);

        // Check if player's stats has this stat added yet
        stat_group = points[StatsGroup.PLAYER];
        CheckAddEntry(player_id, StatsGroup.PLAYER);
        // Add to the players' stats
        stat_group[player_id].AddToStat(s.StatType, s.Value);

        // Then add to the teams' stats ONLY IF the stat is being tracked by winconditions
        List<StatEventType> win_condition_stats = gameObject.GetComponent<WinConditions>().GetWinConditionStats();
        if (win_condition_stats.Contains(s.StatType))
        {
            stat_group = points[StatsGroup.TEAM];
            int team_index = gameObject.GetComponent<TeamStructure>().GetTeamIndex(team_name);

            // Fetch the players' team
            CheckAddEntry((ulong)team_index, StatsGroup.TEAM);
            StatTracker source_team_stats = stat_group[(ulong)team_index];
            source_team_stats.AddToStat(s.StatType, s.Value);
        }

        EmitPointsChanged();
    }

    public NetworkList<Vector4> FetchStats()
    {
         return points;
    }
    #endregion

    #region Private methods
    private void EmitPointsChanged()
    {
        gameObject.BroadcastMessage("OnPointsChanged", FetchStats());
    }

    private void CheckAddEntry(ulong id, StatsGroup stat_group_id)
    {
        NetworkList<Vector4> points = FetchStats();
        // Dictionary<ulong, StatTracker> stat_group = points[stat_group_id];

        // Iterate through all entries to find the right one by id & stat_group_id
        // If unable to find entry, add entry
        bool result = false;

        foreach (Vector4 v in points)
        {
            ulong entry_stat_group_id = v[0];
            ulong entry_id = v[1];

            if ((entry_stat_group_id == stat_group_id) && (entry_id == id))
            {
                result = true;
            }

            if (result)
            {
                break;
            }
        }

        // we didn't find an entry so let's add one
        if (!result)
        {
            stat_group.Add(id, new StatTracker(id, stat_group_id));
        }
    }

    // Unimplemented
    private Vector4 FindEntry(float _f) { return Vector4.one; }
    #endregion

    #region Message Receivers
    #endregion
}
