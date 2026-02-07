using System;
using System.Collections.Generic;


public class StatTracker
{
    // id = either Team_ID or Player_ID
    public int id;

    /* Dictionary<StatEventType, int> stats
     * { StatEventType.KILLS:         5,
     *   StatEventType.FLAG_CAPTURES: 1,
     *   StatEventType.FLAG_RETURNS:  3,
     * }
     */
    public Dictionary<StatEventType, float> stats = new Dictionary<StatEventType, float>{};

    public StatTracker(int p)
    {
        id = p;
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat(StatEventType s, float v)
    {
        if (!stats.ContainsKey(s))
        {
            stats.Add(s, 0);
        }

        stats[s] += v;
    }

    // Lookup what the current value of a stat is, given a stat type.
    public float FetchStatValue(StatEventType s)
    {
        float value = 0f;
        stats.TryGetValue(s, out value);

        return value;
    }
}
