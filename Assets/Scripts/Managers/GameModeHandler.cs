using System.Collections.Generic;
using UnityEngine;

// Owns the current GameMode.
// Begins and ends the game.
// Must be notified to begin or end it (signalled by admin).
// GameModes will handle beginning/ending their own game session
// through PhaseSystem, once it has begun.
public class GameModeHandler : MonoBehaviour
{

    // Setting a new game mode will start the game automatically.
    private GameModeBase _current_game_mode;
    public GameModeBase CurrentGameMode
    {
        get
        {
            return _current_game_mode;
        }
        set
        {
            _current_game_mode = value;
            StartGame();
        }
    }

    // For debug/testing, automatically load a game mode selectable from in the inspector.
    [SerializeField]
    public GameModeBase default_game_mode;
    [SerializeField]
    public bool loadDefaultGameMode = true;
    
    // Entry point for the game mode to begin its session.
    public void StartGame()
    {
        CurrentGameMode.LaunchSession();
    }

    // And to end a game mode session. Begin all cleanup, unloading operations.
    public void StopGame()
    {
        CurrentGameMode.EndSession();
    }
    
    // Game mode can be selected at any time. player voting, admin selection, in-game host selection should all call from here.
    public void SelectNewMode(GameModeBase g)
    {
        CurrentGameMode?.EndSession();
    	CurrentGameMode = g;
    }

    // For debug/testing, automatically load a game mode selectable from in the inspector.
    void Start()
    {
        if ( loadDefaultGameMode )
        {
            CurrentGameMode = default_game_mode;
        }
    }
}
