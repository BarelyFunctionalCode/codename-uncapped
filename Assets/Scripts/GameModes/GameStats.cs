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
    /*  Point tracking for teams & players
     * {
     *   StatsGroup.TEAM:   { 0: StatTracker},
     *   StatsGroup.PLAYER: {0: StatTracker,
     *                       1: StatTracker,
     * }}
     *
     */
    [SerializeField] private Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points = new();

    [SerializeField] private GameModeBase gameMode;


    private void Awake()
    {
        points[StatsGroup.PLAYER] = new Dictionary<ulong, StatTracker>();
        points[StatsGroup.TEAM] = new Dictionary<ulong, StatTracker>();
        points[StatsGroup.NONE] = new Dictionary<ulong, StatTracker>();
    }

    public void ClearStats()
    {
        foreach(var groupOfEntries in points.Values)
        {
            foreach (KeyValuePair<ulong, StatTracker> listOfEntries in groupOfEntries)
            {
                listOfEntries.Value.Clear();
            }
        }
    }

    public void CheckAddEntry(ulong id, StatsGroup statGroupId)
    {
        Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points = FetchStats();
        Dictionary<ulong, StatTracker> statGroup = points[statGroupId];

        if (!statGroup.ContainsKey(id))
        {
            statGroup.Add(id, new StatTracker(id, statGroupId));
        }
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEvent s)
    {
        Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points = FetchStats();
        Dictionary<ulong, StatTracker> statGroup;
        ulong characterId = s.Source;

        // Check if player's stats has this stat added yet
        statGroup = points[StatsGroup.PLAYER];
        CheckAddEntry(characterId, StatsGroup.PLAYER);
        // Add to the players' stats
        statGroup[characterId].AddToStat(s.StatType, s.Value);

        // Debugging
        statGroup[characterId].PrettyPrint();

        // Create StatEvent with updated value
        StatEvent updatedStatEvent = new(
            s.StatType,
            statGroup[characterId].FetchStatValue(s.StatType),
            s.Source
        );

        // Then add to the teams' stats ONLY IF the stat is being tracked by winconditions
        StatEventType winConditionStat = gameObject.GetComponent<WinCondition>().GetWinConditionStat();
        if (winConditionStat == s.StatType)
        {
            statGroup = points[StatsGroup.TEAM];
            int teamIndex = CharacterManager.Instance.GetCharacterByEntityId(characterId).identification.FetchTeamId();

            // Fetch the players' team
            CheckAddEntry((ulong)teamIndex, StatsGroup.TEAM);
            StatTracker sourceTeamStats = statGroup[(ulong)teamIndex];
            sourceTeamStats.AddToStat(s.StatType, s.Value);
        }

        gameMode.OnPointsChanged(FetchStats(), updatedStatEvent);
    }

    public List<FlatStatData> FetchFlatStats()
    {
        List<FlatStatData> f = new();

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
    public List<FlatStatData> FetchFlatStatsAndCleanState() => FetchFlatStats();

    public Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> FetchStats() => points;

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
}
