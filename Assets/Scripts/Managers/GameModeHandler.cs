using System.Collections.Generic;
using System;
using UnityEngine;

public enum GameModes
{
    NONE,
    FFA,
    CTF,
}

// Owns the current GameMode.
// Begins and ends the game.
// Must be notified to begin or end it (signalled by admin).
// GameModes will handle beginning/ending their own game session
// through PhaseSystem, once it has begun.
public class GameModeHandler : MonoBehaviour
{


    private static GameModeHandler _instance;
    public static GameModeHandler Instance
    {
        get
        {
            return _instance;
        }
    }

    public GameModeBase current_game_mode;

    #region Gamemode prefab cache
    // Pseudo
    [SerializeField]
    public IReadOnlyDictionary<GameModes, int> game_mode_cache = new Dictionary<GameModes, int>()
    {
        { GameModes.FFA, 0 }
    };

    #endregion


    #region Public Methods
    public void StatEventReceiver(StatEvent s)
    {
        current_game_mode.StatEventReceiver(s);
    }
    
    // Game mode can be selected at any time. player voting, admin selection, in-game host selection should all call from here.
    // Psuedo
    public void SelectNewMode(GameModes g)
    {
        // Delete the current one
        Destroy(current_game_mode);

        // Fetch the new one and assign it
        int game_mode = game_mode_cache[g];
        //current_game_mode = game_mode.Instantiate();
    }
    #endregion

    #region Message Receivers
    private void Start()
    {
        // Init singleton logic
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

        else
        {
            _instance = this;
        }

        current_game_mode = gameObject.GetComponent<GameModeBase>();
    }
    #endregion
}
