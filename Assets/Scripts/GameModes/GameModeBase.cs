using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;


public class GameModeBase : NetworkBehaviour
{
    [SerializeField] private TeamStructure teamStructure;
    [SerializeField] private PhaseSystem phaseSystem;
    [SerializeField] private GameStats gameStats;
    [SerializeField] private WinCondition winConditions;
    public TeamStructure TeamStructure => teamStructure;
    public PhaseSystem PhaseSystem => phaseSystem;
    public GameStats GameStats => gameStats;
    public WinCondition WinConditions => winConditions;

    static readonly List<StatEventType> broadcastedStatEvents = new()
    {
        StatEventType.KILL,
        StatEventType.KILL_ASSIST,
        StatEventType.DEATHS,
        StatEventType.FLAG_CAPTURE,
    };

    private NetworkVariable<GameModes> _gameModeId = new();
    public GameModes GameModeId {
        get => _gameModeId.Value;
        set {
            if (IsHost) _gameModeId.Value = value;
        }
    }
    private void OnGameModeIdChanged(GameModes oldValue, GameModes newValue)
    {
        GameModeHandler.Instance.OnGameModeChanged.Invoke(newValue);
    }
    [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
    public void TriggerGameModeUpdateRpc(RpcParams rpcParams = default)
    {
        GameModeHandler.Instance.OnGameModeChanged.Invoke(GameModeId);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _gameModeId.OnValueChanged += OnGameModeIdChanged;
    }

    public override void OnNetworkDespawn()
    {
        _gameModeId.OnValueChanged -= OnGameModeIdChanged;

        base.OnNetworkDespawn();
    }

    public void StatEventReceiver(StatEvent s)
    {
        if (!IsHost) return;
        // Stats should only accumulate during active game session.
        if (phaseSystem.CurrentPhase == Phase.ACTIVE) gameStats.AddToStat(s);
    }

    // Entry point for the game mode to begin its session.
    public void StartGame()
    {
        if (!IsHost) return;
        phaseSystem.HardSet(Phase.PRELOAD);
    }

    // And to end a game mode session. Begin all cleanup, unloading operations.
    public void StopGame()
    {
        if (!IsHost) return;
        phaseSystem.HardSet(Phase.ENDGAME);
    }

    public void OnGameWon(ulong id)
    {
        if (!IsHost) return;
        StopGame();
    }

    public void OnPhaseChanged(Phase phase)
    {
        if (!IsHost) return;
        GameModeHandler.Instance.currentPhase.Value = phase;
    }

    public void OnPointsChanged(Dictionary<StatsGroup, Dictionary<ulong, StatTracker>> points, StatEvent updatedStatEvent)
    {
        if (!IsHost) return;

        winConditions.CheckAll(points[StatsGroup.TEAM]);

        // Call RPC that broadcasts the updated player stat to all clients
        OnStatChangeRPC(updatedStatEvent);
    }
    [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
    private void OnStatChangeRPC(StatEvent statEvent, RpcParams rpcParams = default)
    {
        if (statEvent.StatType == winConditions.GetWinConditionStat())
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
    public void TriggerCharactersStatsDump(ulong clientId)
    {
        if (!IsHost) return;

        foreach (KeyValuePair<ulong, StatTracker> entry in gameStats.FetchStats()[StatsGroup.PLAYER])
        {
            foreach (KeyValuePair<StatEventType, float> stat in entry.Value.stats)
            {
                StatEvent statEvent = new(
                    stat.Key,
                    stat.Value,
                    entry.Key
                );
                OnStatChangeRPC(statEvent, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            }
        }
    }
}
