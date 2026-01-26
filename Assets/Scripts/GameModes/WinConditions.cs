using UnityEngine;
using System.Collections.Generic;

public class WinConditions : MonoBehaviour
{
    public List<WinConditionItem> win_conditions;

    public bool CheckAll(Dictionary<int, StatTracker> team_points)
    {
        bool result = false;

        // Does any winconditionitem indicate that the game has won?
        foreach (WinConditionItem w in win_conditions)
        {
            StatEventType s = w.StatType;
            int v = w.Value;

            foreach (StatTracker t in team_points)
            {
                int team_point_value = t.FetchStatValue(s);
                if (team_point_value >= v)
                {
                    result = true;
                    break;
                }
            }

            // We already found a winner
            if ( result )
            {
                break;
            }

        }
        return result;
    }
}
