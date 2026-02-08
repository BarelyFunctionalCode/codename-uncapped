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

    #region Gamemode prefab cache
    [SerializeField]
    public List<GameObject> game_mode_cache;
    #endregion

    #region Private Methods
    // For debugging, artificially increment kill count
    private void FixedUpdate()
    {

    }

    // Check score against win condition, complete the game if score is met.
    private void CheckScore(Dictionary<ulong, StatTracker> team_points)
    {
        print("Checking score");
        win_conditions.CheckAll(team_points);
    }
    #endregion

    #region Public Methods
    public void StatEventReceiver(StatEvent s)
    {
        print("Receiving stat event");
        // Stats should only accumulate during active game session.
        if (phase_system.GetCurrentPhase() == Phase.ACTIVE)
        {
            game_stats.AddToStat(s);
        }
    }

    // Entry point for the game mode to begin its session.
    public void StartGame()
    {
        team_structure.WipeTeams();
        phase_system.HardSet(Phase.PRELOAD);
    }

    // And to end a game mode session. Begin all cleanup, unloading operations.
    public void StopGame()
    {
        phase_system.HardSet(Phase.ENDGAME);
    }
    
    // Game mode can be selected at any time. player voting, admin selection, in-game host selection should all call from here.
    public void SelectNewMode(GameObject g)
    {
        if (g.TryGetComponent<TeamStructure>(out TeamStructure c))
        {
            TeamStructure old_component = gameObject.GetComponent<TeamStructure>();
            Destroy(old_component);
            gameObject.AddComponent(Instantiate(c));
            team_structure = c;
        }

        if (g.TryGetComponent<PhaseSystem>(out PhaseSystem c))
        {
            PhaseSystem old_component = gameObject.GetComponent<PhaseSystem>();
            Destroy(old_component);
            gameObject.AddComponent(Instantiate(c));
            phase_system = c;
        }

        if (g.TryGetComponent<GameStats>(out GameStats c))
        {
            GameStats old_component = gameObject.GetComponent<GameStats>();
            Destroy(old_component);
            gameObject.AddComponent(Instantiate(c));
            game_stats = c;
        }

        if (g.TryGetComponent<WinConditions>(out WinConditions c))
        {
            WinConditions old_component = gameObject.GetComponent<WinConditions>();
            Destroy(old_component);
            gameObject.AddComponent(Instantiate(c));
            win_conditions = c;
        }
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
    }

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
        print("On points changed");
        CheckScore(points[StatsGroup.TEAM]);
    }

    public void OnGameWon()
    {
        print("Game won!");
    }
    #endregion
}
