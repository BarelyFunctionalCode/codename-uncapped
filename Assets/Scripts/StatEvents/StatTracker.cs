using System;
using System.Collections.Generic;
using Unity.Netcode;


public struct StatTracker : INetworkSerializable
{
    // id = either Team_ID or Player_ID
    public ulong id;
    public StatsGroup stat_group_id;

    /* Dictionary<StatEventType, int> stats
     * { StatEventType.KILLS:         5,
     *   StatEventType.FLAG_CAPTURES: 1,
     *   StatEventType.FLAG_RETURNS:  3,
     * }
     */
    public List<float> stats;

    // Class constructor
    public StatTracker(ulong _id, StatsGroup _stat_group_id)
    {
        id = _id;
        stat_group_id = _stat_group_id;
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

    public ulong FetchId()
    {
        return id;
    }

    // Lookup what the current value of a stat is, given a stat type.
    public float FetchStatValue(StatEventType s)
    {
        float value = 0f;
        stats.TryGetValue(s, out value);

        return value;
    }

    private List<float> ConstructEmptyStats()
    {
        List<float> list = new List<float>();
        //pseudo
        /*
        foreach (enum e in StatEventType)
        {
            list.Add(e);
        }
        */
        return list;
    }

    #region INetworkSerializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {

        }
        else
        {

        }
    }
    #endregion
}
