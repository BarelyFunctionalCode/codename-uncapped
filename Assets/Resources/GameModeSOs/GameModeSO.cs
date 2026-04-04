using UnityEngine;

[CreateAssetMenu(fileName = "New Game Mode", menuName = "Game Mode/Game Mode")]
public class GameModeSO : ScriptableObject
{
    public string displayName;
    public string description;
    public string winConditionDescription;
    public GameModes gameModeType;}
