using System.Collections.Generic;
using UnityEngine;
public enum MaxAllowedPlayersOptions
{
    TWO = 2,
    FOUR = 4,
    EIGHT = 8,
    SIXTEEN = 16,
}

public enum MaxAllowedTimeLimitOptions
{
    FIVE = 5,
    TEN = 10,
    FIFTEEN = 15,
    THIRTY = 30,
    SIXTY = 60,
}

[CreateAssetMenu(fileName = "New Game Mode", menuName = "Game Mode/Game Mode")]
public class GameModeSO : ScriptableObject
{
    public GameModes gameModeName;
    public string displayName;
    public string description;
    public MaxAllowedPlayersOptions maxAllowedPlayers;
    public MaxAllowedTimeLimitOptions maxAllowedTimeLimitMinutes;
    public string objectiveName;
    public int defaultObjectiveLimit;
    public TeamBasedType teamBasedType;
    public GameObject prefab;
}
