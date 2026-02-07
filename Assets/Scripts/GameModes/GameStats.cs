using System;
using System.Collections.Generic;
using UnityEngine;

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


public class GameStats : MonoBehaviour
{
    #region Properties
    /*  Point tracking for teams & players
     * {
     *   StatsGroup.TEAM:   { 0: StatTracker},
     *   StatsGroup.PLAYER: {0: StatTracker,
     *                       1: StatTracker,
     * }}
     *
     */
    private Dictionary<StatsGroup, Dictionary<int, StatTracker>> points = new Dictionary<StatsGroup, Dictionary<int, StatTracker>>();
    #endregion

    #region Private methods
    private void EmitPointsChanged()
    {
        gameObject.BroadcastMessage("OnPointsChanged", FetchStats());
    }
    #endregion

    #region Public Methods
    public void CheckAddEntry(int id, StatsGroup stat_group_id)
    {
        Dictionary<StatsGroup, Dictionary<int, StatTracker>> points = FetchStats();
        Dictionary<int, StatTracker> stat_group = points[stat_group_id];

        if (!stat_group.ContainsKey(id))
        {
            stat_group.Add(id, new StatTracker(id));
        }
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEvent s)
    {
        Dictionary<StatsGroup, Dictionary<int, StatTracker>> points = FetchStats();
        Dictionary<int, StatTracker> stat_group;
        int player_id = s.Source;
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
            CheckAddEntry(team_index, StatsGroup.TEAM);
            StatTracker source_team_stats = stat_group[team_index];
            source_team_stats.AddToStat(s.StatType, s.Value);
        }
    }

    public Dictionary<StatsGroup, Dictionary<int, StatTracker>> FetchStats()
    {
         return points;
    }
    #endregion
}
