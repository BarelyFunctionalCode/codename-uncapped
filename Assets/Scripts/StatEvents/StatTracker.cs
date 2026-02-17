using System;
using System.Collections.Generic;


public class StatTracker<T>
    where T : struct
{
    // id = either Team_ID or Player_ID
    public ulong id;

    /* Dictionary<StatEventType, int> stats
     * { StatEventType.KILLS:         5,
     *   StatEventType.FLAG_CAPTURES: 1,
     *   StatEventType.FLAG_RETURNS:  3,
     * }
     */
    public Dictionary<StatEventType, float> stats = new Dictionary<StatEventType, float>{};

    public StatTracker<T>(ulong p)
    {
        id = p;
    }

    // Add to a stat value, check first if that stat has an entry. If not, add a default stat 0.
    public void AddToStat<V>(StatEventType s, V v)
        where V : T
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
