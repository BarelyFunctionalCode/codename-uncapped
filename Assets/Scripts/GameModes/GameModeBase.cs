using UnityEngine;
using System;
using System.Collections.Generic;


public class GameModeBase : MonoBehaviour
{
    #region Component references
    private TeamStructure team_structure;
    private PhaseSystem   phase_system;
    private GameStats     game_stats;
    private WinConditions win_conditions;
    #endregion

    #region State
    private bool isInSession	   = false;
    private bool isComplete		   = true;
    private bool isDamageAllowed   = false;
    private bool isLoaded          = false;
    #endregion

    #region Public Methods
    public void StatEventReceiver(StatEvent s)
    {
        // Stats should only accumulate during active game session.
        if (phase_system.GetCurrentPhase() == Phase.ACTIVE)
        {
            game_stats.AddToStat(s);
        }
    }

    // Entry point for the game mode to begin its session.
    public void StartGame()
    {
        if (!isLoaded)
        {
            return;
        }
        team_structure.WipeTeams();
        phase_system.HardSet(Phase.PRELOAD);
    }

    // And to end a game mode session. Begin all cleanup, unloading operations.
    public void StopGame()
    {
        phase_system.HardSet(Phase.ENDGAME);
    }
    #endregion

    #region Message Receivers
    public void OnPhaseChanged(EventArgsPhaseChanged e)
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
        ulong player_id = 0; // e.player_id;

        game_stats.CheckAddEntry(player_id, StatsGroup.PLAYER);
    }

    public void OnPointsChanged(Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points)
    {
        CheckScore(points[StatsGroup.TEAM]);
    }

    public void OnGameWon()
    {
        print("Game won!");
    }

    private void Start()
    {
        team_structure  = gameObject.GetComponent<TeamStructure>();
        phase_system    = gameObject.GetComponent<PhaseSystem>();
        game_stats      = gameObject.GetComponent<GameStats>();
        win_conditions  = gameObject.GetComponent<WinConditions>();

        isLoaded = true;
        StartGame();
    }
    #endregion

    #region Private Methods
    // Check score against win condition, complete the game if score is met.
    private void CheckScore(Dictionary<ulong, StatTracker> team_points)
    {
        win_conditions.CheckAll(team_points);
    }
    #endregion
}
