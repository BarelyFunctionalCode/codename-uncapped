using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;


public class GameModeBase : NetworkBehaviour
{
    #region Component references
    [SerializeField]
    private TeamStructure team_structure;
    [SerializeField]
    private PhaseSystem   phase_system;
    [SerializeField]
    private GameStats     game_stats;
    [SerializeField]
    private WinCondition win_conditions;
    #endregion

    #region State
    // private bool isInSession	   = false;
    // private bool isComplete		   = true;
    // private bool isDamageAllowed   = false;
    #endregion

    static readonly List<StatEventType> broadcastedStatEvents = new()
    {
        StatEventType.KILL,
        StatEventType.KILL_ASSIST,
        StatEventType.DEATHS,
        StatEventType.FLAG_CAPTURE,
    };

    private NetworkVariable<GameModes> _gameModeId = new();
    public GameModes game_mode_id {
        get => _gameModeId.Value;
        set {
            if (IsHost) _gameModeId.Value = value;
        }
    }

    #region Public Methods
    public void StatEventReceiver(StatEvent s)
    {
        if (!IsHost) return;

        // Stats should only accumulate during active game session.
        if (phase_system.GetCurrentPhase() == Phase.ACTIVE)
        {
            game_stats.AddToStat(s);
        }
    }

    // Entry point for the game mode to begin its session.
    public void StartGame(List<string> custom_team_names)
    {
        if (!IsHost) return;

        // Debugging
        print("Starting game");

        team_structure.WipeTeams();
        team_structure.InitializeTeamNames(custom_team_names);

        // Debugging
        team_structure.AddNewTeam("Red");
        team_structure.AddNewTeam("Blue");

        phase_system.HardSet(Phase.PRELOAD);
    }

    // And to end a game mode session. Begin all cleanup, unloading operations.
    public void StopGame()
    {
        if (!IsHost) return;

        phase_system.HardSet(Phase.ENDGAME);
    }
    #endregion

    #region Message Receivers
    public void OnPhaseChanged(EventArgsPhaseChanged e)
    {
        if (!IsHost) return;
        
        GameModeHandler.Instance.currentPhase.Value = e.phase;

        // if in active session, toggle state booleans appropriately
        switch( e.phase )
        {
            case Phase.ACTIVE:
                // isInSession		= true;
                // isComplete		= false;
                // isDamageAllowed	= true;
                break;
            default:
                // isInSession		= false;
                // isComplete		= true;
                // isDamageAllowed	= false;
                break;
        }
    }

    // Pseudo
    // Need to update the EventArgs to make sure its sending player id
    public void OnPlayerJoined(object sender, EventArgs e)
    {
        if (!IsHost) return;

        ulong player_id = 0; // e.player_id;

        game_stats.CheckAddEntry(player_id, StatsGroup.PLAYER);
    }

    public void OnPointsChanged(Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points, StatEvent updated_stat_event)
    {
        if (!IsHost) return;

        CheckScore(points[StatsGroup.TEAM]);
        // And send an RPC out to clients for stats data updates
        // PushStatsToClientsRPC(game_stats.FetchFlatStatsAndCleanState());

        // Call RPC that broadcasts the updated player stat to all clients
        OnStatChangeRPC(updated_stat_event);
    }

    // Pseudo. Refactor to properly announce team that won across network peers with an RPC
    public void OnGameWon(ulong id)
    {
        if (!IsHost) return;

        print("Game won! : " + id);
    }
    #endregion

    #region Private Methods
    // Check score against win condition, complete the game if score is met.
    private void CheckScore(Dictionary<ulong, StatTracker> team_points)
    {
        if (!IsHost) return;

        win_conditions.CheckAll(team_points);
    }
    #endregion

    #region Networking
    // [Rpc(SendTo.Everyone)]
    // private void PushStatsToClientsRPC(List<FlatStatData> s, RpcParams rpcparams = default)
    // {
    //     print("PushStatsToClientsRPC");
    //     game_stats.RebuildStats(s);
    // }

    [Rpc(SendTo.Everyone)]
    private void OnStatChangeRPC(StatEvent statEvent)
    {
        if (statEvent.StatType == win_conditions.GetWinConditionStat())
        {
            StatEvent winConditionEvent = new(
                StatEventType.WIN_CONDITION,
                statEvent.Value,
                statEvent.Source
            );
            GameModeHandler.Instance.OnStatUpdated.Invoke(winConditionEvent);
        }
        // Broadcast updated player stat to all clients, if the stat event is one we care about
        if (broadcastedStatEvents.Contains(statEvent.StatType))
        {
            GameModeHandler.Instance.OnStatUpdated.Invoke(statEvent);
        }
    }
    #endregion
}
