using System;
using System.Collections.Generic;


public class StatTracker
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
    public Dictionary<StatEventType, float> stats = new Dictionary<StatEventType, float>{};

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

    // Cast 'stats' field from a Dictionary to struct
    public FlatStatData Flatten()
    {
        // Output variables to fetch stats
        float _NONE;
        float _KILL;
        float _KILL_ASSIST;
        float _SHOT_FIRED;
        float _SHOT_HIT;
        float _FLAG_CAPTURE;
        float _FLAG_RETURN;
        float _FLAG_PICKED_UP;
        float _FLAG_HELD;
        float _DAMAGE_TAKEN;

        // Fetch stats and collect success booleans
        bool b_NONE = stats.TryGetValue(StatEventType.NONE,                 out _NONE);
        bool b_KILL = stats.TryGetValue(StatEventType.KILL,                 out _KILL);
        bool b_KILL_ASSIST = stats.TryGetValue(StatEventType.KILL_ASSIST,   out _KILL_ASSIST);
        bool b_SHOT_FIRED = stats.TryGetValue(StatEventType.SHOT_FIRED,     out _SHOT_FIRED);
        bool b_SHOT_HIT = stats.TryGetValue(StatEventType.SHOT_HIT,         out _SHOT_HIT);
        bool b_FLAG_CAPTURE = stats.TryGetValue(StatEventType.FLAG_CAPTURE, out _FLAG_CAPTURE);
        bool b_FLAG_RETURN = stats.TryGetValue(StatEventType.FLAG_RETURN,   out _FLAG_RETURN);
        bool b_FLAG_PICKED_UP = stats.TryGetValue(StatEventType.FLAG_PICKED_UP, out _FLAG_PICKED_UP);
        bool b_FLAG_HELD = stats.TryGetValue(StatEventType.FLAG_HELD,           out _FLAG_HELD);
        bool b_DAMAGE_TAKEN = stats.TryGetValue(StatEventType.DAMAGE_TAKEN,      out _DAMAGE_TAKEN);

        // Flatten the stats data, provide 0.0 as defaults
        // if for some reason we couldn't fetch the stats data correctly
        FlatStatData f = new FlatStatData(
            id,
            stat_group_id,
            b_NONE ?            _NONE :             0.0f,
            b_KILL ?            _KILL :             0.0f,
            b_KILL_ASSIST ?     _KILL_ASSIST :      0.0f,
            b_SHOT_FIRED ?      _SHOT_FIRED :       0.0f,
            b_SHOT_HIT ?        _SHOT_HIT :         0.0f,
            b_FLAG_CAPTURE ?    _FLAG_CAPTURE :     0.0f,
            b_FLAG_RETURN ?     _FLAG_RETURN :      0.0f,
            b_FLAG_PICKED_UP ?  _FLAG_PICKED_UP :   0.0f,
            b_FLAG_HELD ?       _FLAG_HELD :        0.0f,
            b_DAMAGE_TAKEN ?    _DAMAGE_TAKEN :     0.0f
        );

        return f;
    }


    // Cast FlatStatsData back into 'stats' field
    public void Rebuild(FlatStatData f)
    {
        stats[StatEventType.NONE] =           f.None;
        stats[StatEventType.KILL] =           f.Kill;
        stats[StatEventType.KILL_ASSIST] =    f.Kill_Assist;
        stats[StatEventType.SHOT_FIRED] =     f.Shot_Fired;
        stats[StatEventType.SHOT_HIT] =       f.Shot_Hit;
        stats[StatEventType.FLAG_CAPTURE] =   f.Flag_Capture;
        stats[StatEventType.FLAG_RETURN] =    f.Flag_Return;
        stats[StatEventType.FLAG_PICKED_UP] = f.Flag_Picked_Up;
        stats[StatEventType.FLAG_HELD] =      f.Flag_Held;
        stats[StatEventType.DAMAGE_TAKEN] =   f.Damage_Taken;
    }
}
