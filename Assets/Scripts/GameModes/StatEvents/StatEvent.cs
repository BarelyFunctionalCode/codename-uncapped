using Unity.Netcode;

public enum StatEventType
{
    NONE,
    KILL,
    KILL_ASSIST,
    DEATHS,
    SHOT_FIRED,
    SHOT_HIT,
    FLAG_CAPTURE,
    FLAG_RETURN,
    FLAG_PICKED_UP,
    FLAG_HELD,
    DAMAGE_TAKEN,
    DAMAGE_DEALT,
    INTEL_STOLEN,
    WIN_CONDITION
}

public struct StatEvent: INetworkSerializeByMemcpy
{
    // What kind of event happened?
    public StatEventType StatType;

    // What is the value of the event?
    public float Value;

    // Which player caused the event?
    public ulong Source;

    public StatEvent(StatEventType t, float v, ulong s)
    {
        StatType = t;
        Value = v;
        Source = s;
    }
}
