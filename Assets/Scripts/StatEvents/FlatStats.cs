public struct FlatStatData: INetworkSerializeByMemcpy
{
    public ulong id;
    public StatsGroup stat_group_id;

    public float None;
    public float Kill;
    public float Kill_Assist;
    public float Shot_Fired;
    public float Shot_Hit;
    public float Flag_Capture;
    public float Flag_Return;
    public float Flag_Picked_Up;
    public float Flag_Held;
    public float Damage_Taken;

    public FlatStatData(
        ulong _id,
        StatsGroup _stat_group_id,
        float _None,
        float _Kill,
        float _Kill_Assist,
        float _Shot_Fired,
        float _Shot_Hit,
        float _Flag_Capture,
        float _Flag_Return,
        float _Flag_Picked_Up,
        float _Flag_Held,
        float _Damage_Taken
    ) {
        this.id = _id;
        this.stat_group_id = _stat_group_id;

        this.None = _None;
        this.Kill = _Kill;
        this.Kill_Assist = _Kill_Assist;
        this.Shot_Fired = _Shot_Fired;
        this.Shot_Hit = _Shot_Hit;
        this.Flag_Capture = _Flag_Capture;
        this.Flag_Return = _Flag_Return;
        this.Flag_Picked_Up = _Flag_Picked_Up;
        this.Flag_Held = _Flag_Held;
        this.Damage_Taken = _Damage_Taken;
    }
}
