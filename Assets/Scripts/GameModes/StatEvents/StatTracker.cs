using UnityEngine;
using System;
using System.Collections.Generic;


public class StatTracker
{
    // id = either Team_ID or Player_ID
    public ulong id;
    public StatsGroup statGroupId;

    /* Dictionary<StatEventType, int> stats
     * { StatEventType.KILLS:         5,
     *   StatEventType.FLAG_CAPTURES: 1,
     *   StatEventType.FLAG_RETURNS:  3,
     * }
     */
    public Dictionary<StatEventType, float> stats = new() { };

    public void Clear()
    {
        List<StatEventType> l = new();
        foreach (StatEventType s in stats.Keys) l.Add(s);
        foreach (StatEventType s in l) stats[s] = 0.0f;
    }

    public StatTracker(ulong _id, StatsGroup _stat_group_id)
    {
        id = _id;
        statGroupId = _stat_group_id;
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEventType s, float v)
    {
        if (!stats.ContainsKey(s)) stats.Add(s, 0);
        stats[s] += v;
    }

    public ulong FetchId() => id;

    // Lookup what the current value of a stat is, given a stat type.
    public float FetchStatValue(StatEventType s)
    {
        stats.TryGetValue(s, out float value);
        return value;
    }

    public void PrettyPrint()
    {
        foreach (KeyValuePair<StatEventType, float> kvp in stats)
        {
            Debug.Log($"Stats for ID: {id} stat {kvp.Key} = {kvp.Value}");
        }
    }
}
