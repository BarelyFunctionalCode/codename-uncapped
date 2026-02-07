using System.Collections.Generic;
using System;
using UnityEngine;

// Owns the current GameMode.
// Begins and ends the game.
// Must be notified to begin or end it (signalled by admin).
// GameModes will handle beginning/ending their own game session
// through PhaseSystem, once it has begun.
public class GameModeHandler : MonoBehaviour
{
    #region State
    private bool isInSession	   = false;
    private bool isComplete		   = true;
    private bool isDamageAllowed   = false;
    #endregion
    private static GameModeHandler _instance;
    public static GameModeHandler Instance
    {
        get
        {
            return _instance;
        }
    }

    #region Component references
    private TeamStructure team_structure;
    private PhaseSystem   phase_system;
    private GameStats     game_stats;
    private WinConditions win_conditions;
    #endregion

    #region Message Callbacks
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

    }
    #endregion
    

    #region Private Methods
    // Check score against win condition, complete the game if score is met.
    private bool CheckScore()
    {
        Dictionary<StatsGroup, Dictionary<int, StatTracker>> points = game_stats.FetchStats();
        Dictionary<int, StatTracker> team_points = points[StatsGroup.TEAM];

        bool result = win_conditions.CheckAll(team_points);

        return result;
    }
    #endregion

    #region Public Methods
    // Entry point for the game mode to begin its session.
    public void StartGame()
    {
        team_structure.WipeTeams();
        phase_system.Step();
    }

    // And to end a game mode session. Begin all cleanup, unloading operations.
    public void StopGame()
    {
        phase_system.HardSet(Phase.ENDGAME);
    }
    
    // Game mode can be selected at any time. player voting, admin selection, in-game host selection should all call from here.
    public void SelectNewMode(GameObject g)
    {
        // CurrentGameMode?.EndSession();
    	// CurrentGameMode = g;


        /*
         / /* Cache references
         team_structure  = gameObject.GetComponent<TeamStructure>();
         phase_system    = gameObject.GetComponent<PhaseSystem>();
         game_stats      = gameObject.GetComponent<GameStats>();
         win_conditions  = gameObject.GetComponent<WinConditions>();

         // Connect signals
         phase_system.PhaseChanged += OnPhaseChanged;
         */
    }
    #endregion

    #region Event Handlers
    public void OnPhaseChanged(object sender, EventArgsPhaseChanged e)
    {
        // if in active session, toggle state booleans appropriately
        switch( e.phase )
        {
            case Phase.ACTIVE:
                isInSession		= true;
                isComplete		= false;
                isDamageAllowed	= true;
                break;
            default:
                isInSession		= false;
                isComplete		= true;
                isDamageAllowed	= false;
                break;
        }
    }

    // Pseudo
    // Need to update the EventArgs to make sure its sending player id
    public void OnPlayerJoined(object sender, EventArgs e)
    {
        int player_id = 0; // e.player_id;

        game_stats.CheckAddEntry(player_id, StatsGroup.PLAYER);
    }

    public void OnGameStatsPointsChanged(object sender, EventArgs e)
    {
        CheckScore();
    }

    #endregion
}
