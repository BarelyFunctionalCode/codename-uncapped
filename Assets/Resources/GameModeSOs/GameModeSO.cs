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
    public string winConditionReaderFriendlyName;
    public StatEventType winConditionStatType;
    public float winConditionDefaultValue;
    public TeamBasedType teamBasedType;
    public List<string> defaultTeamNames;
    public bool isAvailable = true;

    public GameModeData GetGameModeData(int maxPlayers = -1, int timeLimitMinutes = -1, float winConditionValue = -1)
    {
        if (maxPlayers == -1) maxPlayers = (int)maxAllowedPlayers;
        if (timeLimitMinutes == -1) timeLimitMinutes = (int)maxAllowedTimeLimitMinutes;
        if (winConditionValue == -1) winConditionValue = winConditionDefaultValue;

        return new GameModeData(this, maxPlayers, timeLimitMinutes, winConditionValue);
    }
}

public class GameModeData
{
    public GameModeData(GameModeSO gameModeSO, int maxPlayers, int timeLimitMinutes, float winConditionValue)
    {
        this.gameModeSO = gameModeSO;
        this.maxPlayers = maxPlayers;
        this.timeLimitMinutes = timeLimitMinutes;
        winConditionStatType = gameModeSO.winConditionStatType;
        this.winConditionValue = winConditionValue;
    }

    public GameModeSO gameModeSO;
    public int maxPlayers;
    public int timeLimitMinutes;
    public StatEventType winConditionStatType;
    public float winConditionValue;
}
