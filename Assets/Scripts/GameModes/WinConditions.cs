using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.Netcode;
using Unity.Collections;
using UnityEditor;


/// <summary>
/// PropertyDrawer for a FixedString512Bytes
/// </summary>
[CustomPropertyDrawer(typeof(FixedString512Bytes))]
public class FixedString512BytesPropertyDrawer : PropertyDrawer
{
    #region Public methods

    /// <summary>
    /// Called when the UI is drawn
    /// </summary>
    /// <param name="position">The position of the field</param>
    /// <param name="property">The property to serialize</param>
    /// <param name="label">The text</param>
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginChangeCheck();
        string fixedStringValue = EditorGUI.TextField(position, label, property.boxedValue.ToString());

        if (EditorGUI.EndChangeCheck())
        {
            property.boxedValue = new FixedString512Bytes(fixedStringValue);
        }
    }

    /// <summary>
    /// Ensures the field will stay at the proper position
    /// </summary>
    /// <param name="property">The property</param>
    /// <param name="label">The text</param>
    /// <returns>The proper height of the field</returns>
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    #endregion
}


[Serializable]
public struct WinCondition: INetworkSerializable, IEquatable<WinCondition>
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref readerFriendlyName);
        serializer.SerializeValue(ref statType);
        serializer.SerializeValue(ref value);
    }
    public bool Equals(WinCondition other)
    {
        return readerFriendlyName.Equals(other.readerFriendlyName) &&
               statType == other.statType &&
               value == other.value;
    }

    public FixedString512Bytes readerFriendlyName;
    // Stat type
    [SerializeField]
    private StatEventType statType;
    // Required score
    [SerializeField]
    private float value;

    // Is the current score greater than or equal to the required score?
    public bool Check(float v) => v >= value;
    public void SetValue(float v) => value = v;
    public StatEventType GetStatType() => statType;
    public float GetStatValue() => value;
}

public class WinConditions : NetworkBehaviour
{
    private NetworkList<WinCondition> _win_conditions = new();
    private List<WinCondition> win_conditions
    {
        get
        {
            return _win_conditions.AsNativeArray().ToList();
        }
        set
        {
            _win_conditions.Clear();
            foreach (WinCondition w in value)
            {
                _win_conditions.Add(w);
            }
        }
    }

    public void Initialize(List<WinCondition> win_condition_items)
    {
        win_conditions = win_condition_items;
    }

    public void CheckAll(Dictionary<ulong, StatTracker> team_points)
    {
        bool result = false;
        ulong winning_id = 0;

        // Does any winconditionitem indicate that the game has won?
        foreach (WinCondition w in win_conditions)
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

    public void EmitGameWon(ulong winning_id) => gameObject.BroadcastMessage("OnGameWon", winning_id);

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
