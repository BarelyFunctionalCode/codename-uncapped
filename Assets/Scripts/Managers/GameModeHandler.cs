using System.Collections.Generic;
using UnityEngine;

// Owns the current GameMode.
// Begins and ends the game.
// Must be notified to begin or end it (signalled by admin).
// GameModes will handle beginning/ending their own game session
// through PhaseSystem, once it has begun.
public class GameModeHandler : MonoBehaviour
{	
    public GameModeBase current_game_mode;
    
    public void StartGame()
    {
        current_game_mode.LaunchSession();
    }
    
    public void StopGame()
    {
        current_game_mode.EndSession();
    }
    
    public void OnComplete()
    {

    }
    
    public void SelectNewMode(GameModeBase game_mode)
    {
        current_game_mode?.StopGame();
    	current_game_mode = game_mode;
    	StartGame();
    }
}
