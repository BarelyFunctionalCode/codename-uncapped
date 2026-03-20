public enum StatEventType
{
    NONE,
    KILL,
    KILL_ASSIST,
    SHOT_FIRED,
    SHOT_HIT,
    FLAG_CAPTURE,
    FLAG_RETURN,
    FLAG_PICKED_UP,
    FLAG_HELD,
    DAMAGE_TAKEN,
    DAMAGE_DEALT
}

public class StatEvent
{
    // What kind of event happened?
    public StatEventType StatType;

    // What is the value of the event?
    public float Value;

    // Which player caused the event?
    public ulong Source;

    // Who received the event?
    public ulong? Target;

    public StatEvent(StatEventType t, float v, ulong s)
    {
        StatType = t;
        Value = v;
        Source = s;
    }
}
