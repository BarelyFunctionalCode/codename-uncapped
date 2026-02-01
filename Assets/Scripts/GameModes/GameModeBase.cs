using UnityEngine;
using System;
using System.Collections.Generic;


/*
 *  - Manages game session -
 *
 */


public class GameModeBase : MonoBehaviour
{
    #region State
    private bool isInSession     = false;
    private bool isComplete      = false;
    private bool isDamageAllowed = false;
    #endregion

    #region Components
    [SerializeField] public TeamStructure team_structure;
    [SerializeField] public PhaseSystem   phase_system;
    [SerializeField] public GameStats     game_stats;
    [SerializeField] public WinConditions win_conditions;
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
    public void LaunchSession()
    {
        phase_system.Step();
    }

    public void EndSession()
    {
        phase_system.HardSet(Phase.ENDGAME);
    }

    public bool GetIsInSession()        { return isInSession; }
    public bool GetIsComplete()         { return isComplete; }
    public bool GetIsDamageAllowed()    { return isDamageAllowed; }

    public void SetIsDamageAllowed(bool b)
    {
        isDamageAllowed = b;
    }

    public void Start() // Pseudo
    {
        phase_system.PhaseChanged += OnPhaseChanged;
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
        };
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
