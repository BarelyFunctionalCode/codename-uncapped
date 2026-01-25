using UnityEngine;
using System;

[CreateAssetMenu(fileName = "WinConditionItem", menuName = "Scriptable Objects/WinConditionItem")]
public class WinConditionItem : ScriptableObject
{
    // Stat type
    public StatEventType type;
    // Required score
    public int value;

    // Is the current score greater than or equal to the required score?
    public bool Check(int v)
    {
        return v >= value;
    }
}
