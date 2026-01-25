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

public class StatEvent
{
    // What kind of event happened?
    public StatEventType type;

    // What is the value of the event?
    public float value;

    // Which player caused the event?
    public int source;

    // Who received the event?
    public int? target;

    public StatEvent(StatEventType t, int v)
    {
        type = t;
        value = v;
    }
}
