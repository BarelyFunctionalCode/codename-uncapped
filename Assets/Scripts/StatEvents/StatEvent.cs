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
}

public class StatEvent<T> : struct
{
    // What kind of event happened?
    public StatEventType StatType;

    // What is the value of the event?
    private T Value;

    // Which player caused the event?
    public ulong Source;

    // Who received the event?
    public ulong? Target;

    public StatEvent(StatEventType t, T v, ulong s)
    {
        StatType = t;
        Value = v;
        Source = s;
    }

    public T FetchInnerValue()
    {
        return Value;
    }
}
