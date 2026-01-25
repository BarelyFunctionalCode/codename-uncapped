using System;
using System.Collections.Generic;


public class StatTracker
{
    public int player_id;
    public Dictionary<StatEventType, int> stats = new Dictionary<StatEventType, int>{};

    public StatTracker(int p)
    {
        player_id = p;
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEventType s, int v)
    {
        if (!stats.ContainsKey(s))
        {
            stats.Add(s, 0);
        }

        stats[s] += v;
    }

    // Lookup what the current value of a stat is, given a stat type.
    public int FetchStatValue(StatEventType s)
    {
        int value = 0;
        stats.TryGetValue(s, out value);

        return value;
    }
}
