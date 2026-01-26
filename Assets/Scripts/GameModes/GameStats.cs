using System;
using System.Collections.Generic;

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


public class GameStats
{
    #region Properties
    // Point tracking for teams & players
    public Dictionary<StatGroup, Dictionary<int, StatTracker>> points = new Dictionary<StatGroup, Dictionary<int, StatTracker>>();

    #endregion

    #region Public Methods
    public void AddEntry(int id, StatsGroup group)
    {
        Dictionary<StatGroup, Dictionary<int, StatTracker>> points = FetchStats();
        Dictionary<int, StatTracker> stat_group = points[group];

        if (!stat_group.ContainsKey(id))
        {
            stat_group.Add(id, new StatTracker(id));
        }
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEvent s)
    {
        Dictionary<StatGroup, Dictionary<int, StatTracker>> points = FetchStats();
        Dictionary<int, StatTracker> stat_group;
        // Add to players' stats first
        stat_group = points[StatGroup.PLAYER];
        StatTracker source_player_stat = stat_group[s.Source]
        source_player_stat.AddToStat(s.StatType, s.Value);

        // Then add to the teams' stats
        stat_group = points[StatGroup.TEAM];
        // Fetch the players' team

        source_player_stat.AddToStat(s.Value);

    }


    public Dictionary<StatGroup, Dictionary<int, StatTracker>> FetchStats()
    {
        /*            int = player_id or team_id
         *             |
         * Dictionary<int, StatTracker>
         * {
         *  1 = StatTracker,
         *  2 = StatTracker,
         * }
         */
        Dictionary<int, StatTracker> stat_group = new Dictionary<int, StatTracker>{};

        switch (group)
        {
            case StatsGroup.TEAM:
                stat_group = team_points;
                break;
            case StatsGroup.PLAYER:
                stat_group = player_points;
                break;
            default:
                break;
        }

        return stat_group;
    }
    #endregion
}
