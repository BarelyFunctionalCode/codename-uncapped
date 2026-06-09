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
    [SerializeField] private GameModeBase gameMode;

    /*  Point tracking for teams & players
     * {
     *   StatsGroup.TEAM:   { 0: StatTracker},
     *   StatsGroup.PLAYER: {0: StatTracker,
     *                       1: StatTracker,
     * }}
     */
    [SerializeField] private Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points = new();


    private void Awake()
    {
        points[StatsGroup.PLAYER] = new Dictionary<ulong, StatTracker>();
        points[StatsGroup.TEAM] = new Dictionary<ulong, StatTracker>();
        points[StatsGroup.NONE] = new Dictionary<ulong, StatTracker>();
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
        // statGroup[characterId].PrettyPrint();

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

    public Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> FetchStats() => points;
}
