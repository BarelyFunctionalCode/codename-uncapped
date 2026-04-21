using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WinCondition : NetworkBehaviour
{
    [SerializeField] private GameModeBase gameModeBase;
    
    private NetworkVariable<StatEventType> winConditionStatType = new();
    private NetworkVariable<float> winConditionValue = new();

    public void Initialize(StatEventType statType, float value)
    {
        winConditionStatType.Value = statType;
        winConditionValue.Value = value;
    }

    public void CheckAll(Dictionary<ulong, StatTracker> team_points)
    {
        // Check each team's stat tracker
        foreach (KeyValuePair<ulong, StatTracker> t in team_points)
        {
            float team_point_value = t.Value.FetchStatValue(winConditionStatType.Value);
            if (team_point_value >= winConditionValue.Value)
            {
                EmitGameWon(t.Value.FetchId());
                break;
            }
        }
    }

    public void EmitGameWon(ulong winningId) => gameModeBase.OnGameWon(winningId);

    // Fetch Stats that are required to win the game
    public StatEventType GetWinConditionStat()
    {
        return winConditionStatType.Value;
    }
}
