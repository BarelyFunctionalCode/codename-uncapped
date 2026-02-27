using UnityEngine;
using System.Collections.Generic;
using System.Linq;


public class WinConditions : MonoBehaviour
{
    public List<WinConditionItem> win_conditions;

    public void CheckAll(Dictionary<ulong, StatTracker> team_points)
    {
        bool result = false;
        ulong winning_id = 0;

        // Does any winconditionitem indicate that the game has won?
        foreach (WinConditionItem w in win_conditions)
        {
            StatEventType stat_type = w.GetStatType();
            float stat_value = w.GetStatValue();

            // Check each team's stat tracker
            foreach (KeyValuePair<ulong, StatTracker> t in team_points)
            {
                float team_point_value = t.Value.FetchStatValue(stat_type);
                if (team_point_value >= stat_value)
                {
                    result = true;
                    winning_id = t.Value.FetchId();

                    break;
                }
            }

            // We already found a winner, break the loop
            if ( result )
            {
                EmitGameWon(winning_id);
                break;
            }
        }
    }

    public void EmitGameWon(ulong winning_id)
    {
        gameObject.BroadcastMessage("OnGameWon", winning_id);
    }

    // Fetch Stats that are required to win the game
    public List<StatEventType> GetWinConditionStats()
    {
        List<StatEventType> l = new List<StatEventType>();
        foreach (StatEventType t in win_conditions.Select(w => w.GetStatType()))
        {
            l.Add(t);
        }
        return l;
    }
}
