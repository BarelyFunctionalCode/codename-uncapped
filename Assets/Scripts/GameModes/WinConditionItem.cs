using UnityEngine;

/*
 * A single win condition, such as FLAG_CAPTURES, that will be compared
 * against GameStats' current count.
 *
 */

[CreateAssetMenu(fileName = "WinConditionItem", menuName = "Scriptable Objects/WinConditionItem")]
public class WinConditionItem : ScriptableObject
{
    // Stat type
    public StatEventType statType;
    // Required score
    public float defaultValue;
}
