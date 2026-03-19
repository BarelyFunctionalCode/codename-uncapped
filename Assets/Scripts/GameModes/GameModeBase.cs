using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;


public class GameModeBase : MonoBehaviour
{
    #region Component references
    [SerializeField]
    private TeamStructure team_structure;
    [SerializeField]
    private PhaseSystem   phase_system;
    [SerializeField]
    private GameStats     game_stats;
    [SerializeField]
    private WinConditions win_conditions;
    #endregion

    #region State
    private bool isInSession	   = false;
    private bool isComplete		   = true;
    private bool isDamageAllowed   = false;
    #endregion

    [SerializeField]
    public GameModes game_mode_id;

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
        // And send an RPC out to clients for stats data updates
        PushStatsToClients(game_stats.FetchFlatStats());
    }

    // Pseudo. Refactor to properly announce team that won across network peers with an RPC
    public void OnGameWon(ulong id)
    {
        print("Game won! : " + id);
    }
    #endregion

    #region Private Methods
    // Check score against win condition, complete the game if score is met.
    private void CheckScore(Dictionary<ulong, StatTracker> team_points)
    {
        win_conditions.CheckAll(team_points);
    }
    #endregion

    #region Networking
    [Rpc(SendTo.Everyone)]
    private void PushStatsToClients(List<FlatStatData> s, RpcParams rpcparams = default)
    {

    }
    #endregion
}
