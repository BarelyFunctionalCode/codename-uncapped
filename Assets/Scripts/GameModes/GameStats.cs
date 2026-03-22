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
    /* Trigger for sending stat updates to clients
    *   True = send to clients
    *   False = clients are already synced, do not send
    */
    private bool is_dirty = false;

    /*  Point tracking for teams & players
     * {
     *   StatsGroup.TEAM:   { 0: StatTracker},
     *   StatsGroup.PLAYER: {0: StatTracker,
     *                       1: StatTracker,
     * }}
     *
     */

    [SerializeField] private GameModeBase game_mode_base;

    [SerializeField]
    private Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points = new Dictionary<StatsGroup, Dictionary<ulong, StatTracker>>();
    #endregion

    #region Private methods
    private void ClearStats()
    {
        foreach(var group_of_entities in points.Values)
        {
            foreach (KeyValuePair<ulong, StatTracker> list_of_entities in group_of_entities)
            {
                list_of_entities.Value.Clear();
            }
        }
    }

    private void ContaminateStatState()
    {
        is_dirty = true;
    }

    private void EmitPointsChanged(StatEvent updated_stat_event)
    {
        game_mode_base.OnPointsChanged(FetchStats(), updated_stat_event);
    }
    #endregion

    #region Public Methods
    public void CheckAddEntry(ulong id, StatsGroup stat_group_id)
    {
        Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points = FetchStats();
        Dictionary<ulong, StatTracker> stat_group = points[stat_group_id];

        if (!stat_group.ContainsKey(id))
        {
            stat_group.Add(id, new StatTracker(id, stat_group_id));
        }
    }

    public bool CheckDirtyState()
    {
        return is_dirty;
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEvent s)
    {
        Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points = FetchStats();
        Dictionary<ulong, StatTracker> stat_group;
        ulong player_id = s.Source;
        string team_name = gameObject.GetComponent<TeamStructure>().GetTeam(player_id);

        // Check if player's stats has this stat added yet
        stat_group = points[StatsGroup.PLAYER];
        CheckAddEntry(player_id, StatsGroup.PLAYER);
        // Add to the players' stats
        stat_group[player_id].AddToStat(s.StatType, s.Value);

        // Debugging
        stat_group[player_id].PrettyPrint();

        // Create StatEvent with updated value
        StatEvent updated_stat_event = new(
            s.StatType,
            stat_group[player_id].FetchStatValue(s.StatType),
            s.Source
        );

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

        ContaminateStatState();
        EmitPointsChanged(updated_stat_event);
    }

    public void CleanStatState()
    {
        is_dirty = false;
    }

    public List<FlatStatData> FetchFlatStats()
    {
        List<FlatStatData> f = new List<FlatStatData>();

        /*  Point tracking for teams & players
         * {
         * ->StatsGroup.TEAM:   { 0: StatTracker},
         * ->StatsGroup.PLAYER: { 0: StatTracker,
         *                       1: StatTracker,
         * }}
         *
         */
        // Iterate through each stat_group first
        foreach(var group_of_entities in points.Values)
        {

            /*  Point tracking for teams & players
             * {
             *   StatsGroup.TEAM:   { ->0: StatTracker },
             *   StatsGroup.PLAYER: { ->0: StatTracker,
             *                        ->1: StatTracker,
             * }}
             *
             */
            // Then iterate through each list of entities
            foreach (KeyValuePair<ulong, StatTracker> list_of_entities in group_of_entities)
            {
                StatTracker stat_tracker = list_of_entities.Value;

                f.Add(stat_tracker.Flatten());
            }
        }

        return f;
    }

    // Helper function
    public List<FlatStatData> FetchFlatStatsAndCleanState()
    {
        CleanStatState();
        return FetchFlatStats();
    }

    public Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> FetchStats()
    {
         return points;
    }

    public void RebuildStats(List<FlatStatData> flat_stats)
    {
    print("game_stats RebuildStats");
        // Avoid re-allocations, re-use in the for loops
        ulong id;
        StatsGroup stat_group_id;
        StatTracker stat_tracker;
        Dictionary<ulong, StatTracker> _stats;

        // Loop through each flatstats and copy over the data into `stats` field
        // 1. Fetch id, stats_group_id from FlatStats
        // 2. Fetch StatTracker from `stats`
        //     Dictionary<StatsGroup, Dictionary<ulong, StatTracker>>
        // 3. Overwrite the data in that StatTracker
        foreach(FlatStatData f in flat_stats)
        {
            id = f.id;
            stat_group_id = f.stat_group_id;

            // Fetch the group of StatTrackers
            // points = Dictionary<StatGroup, Dictionary<ulong, StatTracker>>
            // stats = Dictionary<ulong, StatTracker>
            if (points.TryGetValue(stat_group_id, out _stats))
            {
                // Then fetch the individual StatTracker
                if (_stats.TryGetValue(id, out stat_tracker))
                {
                    stat_tracker.Rebuild(f);
                }
            }
        }
    }
    #endregion

    #region Message Receivers
    private void OnPhaseChanged(EventArgsPhaseChanged e)
    {
        if (e.phase == Phase.PRELOAD)
        {
            ClearStats();
        }
    }


    private void Start()
    {
        points[StatsGroup.PLAYER] = new Dictionary<ulong, StatTracker>();
        points[StatsGroup.TEAM] = new Dictionary<ulong, StatTracker>();
        points[StatsGroup.NONE] = new Dictionary<ulong, StatTracker>();
    }
    #endregion
}
