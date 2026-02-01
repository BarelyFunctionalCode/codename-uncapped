using UnityEngine;
using System;

/*
 * A single win condition, such as FLAG_CAPTURES, that will be compared
 * against GameStats' current count.
 *
 */

[CreateAssetMenu(fileName = "WinConditionItem", menuName = "Scriptable Objects/WinConditionItem")]
public class WinConditionItem : ScriptableObject
{
    // Stat type
    [SerializeField]
    private StatEventType StatType;
    // Required score
    [SerializeField]
    private int Value;

    // Is the current score greater than or equal to the required score?
    public bool Check(int v)
    {
        return v >= Value;
    }

    public StatEventType GetStatType()
    {
        return StatType;
    }

    public int GetStatValue()
    {
        return Value;
    }
}
