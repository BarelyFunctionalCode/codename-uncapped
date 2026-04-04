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
    public List<WinCondition> winConditionsDefaults;
    public TeamBasedType teamBasedType;

    public GameModeData GetGameModeData(int maxPlayers = -1, int timeLimitMinutes = -1, List<float> winConditionValues = null)
    {
        if (maxPlayers == -1) maxPlayers = (int)maxAllowedPlayers;
        if (timeLimitMinutes == -1) timeLimitMinutes = (int)maxAllowedTimeLimitMinutes;

        List<WinCondition> winConditions = new(winConditionsDefaults);
        if (winConditionValues != null)
        {
            for (int i = 0; i < winConditions.Count && i < winConditionValues.Count; i++)
            {
                winConditions[i].SetValue(winConditionValues[i]);
            }
        }

        return new GameModeData(this, maxPlayers, timeLimitMinutes, winConditions);
    }
}

public class GameModeData
{
    public GameModeData(GameModeSO gameModeSO, int maxPlayers, int timeLimitMinutes, List<WinCondition> winConditions)
    {
        this.gameModeSO = gameModeSO;
        this.maxPlayers = maxPlayers;
        this.timeLimitMinutes = timeLimitMinutes;
        this.winConditions = winConditions;
    }

    public GameModeSO gameModeSO;
    public int maxPlayers;
    public int timeLimitMinutes;
    public List<WinCondition> winConditions;
    public int objectiveLimit;
}
