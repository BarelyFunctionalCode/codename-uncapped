using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;


public enum Phase
{
    NULL,
    PRELOAD,
    WARMUP,
    ACTIVE,
    ENDGAME
}

 /*  PhaseSystem handles state of the game mode
 *      - States:
 *        - Preload is the initial value, possibly unnecessary and could simply use Warmup
 *        - Warmup pre-game
 *        - Starting a game
 *        - Cleanup post-game
 *      - Receives signal to change state from server host (force game start/game end),
 *        or the states' Step condition
 *        - Pregame warmup has an idle timer of 30 seconds to allow everyone to load into the session,
 *          and select a team, after which it changes state to "In Session"
 *        - In-session phase step condition is # of points
 *        - Cleanup post game phase has a timer of 30 seconds, allows for voting on the next map
 */

public class PhaseSystem : NetworkBehaviour
{
    #region Properties
    [SerializeField]
    private Phase _currentphase = Phase.NULL;
    public Phase CurrentPhase
    {
        get => _currentphase;
        set
        {
            _currentphase = value;
            // Phase duration is set at the same time as we're setting current phase
            SetCountdown(countdowns[value]);
        }
    }

    private NetworkVariable<float> _activePhaseTimeLimit = new();

    public Dictionary<Phase, float> countdowns = new()
    {
        { Phase.PRELOAD,    15.0f  },
        { Phase.WARMUP,     15.0f  },
        { Phase.ACTIVE,     600.0f },
        { Phase.ENDGAME,    15.0f  },
    };

    [SerializeField]
    private float _countdown;
    private float Countdown
    {
        get => _countdown;
        set
        {
            _countdown = value;
            if (Countdown <= 0 && CurrentPhase != Phase.NULL)
            {
                Step();
            }
        }
    }

    // private bool stepping = true;
    #endregion

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _activePhaseTimeLimit.OnValueChanged += OnActivePhaseTimeLimitChanged;
        _activePhaseTimeLimit.SetDirty(true);
    }

    private void OnActivePhaseTimeLimitChanged(float oldValue, float newValue)
    {
        countdowns[Phase.ACTIVE] = newValue;
    }

    #region Public methods
    public void SetActivePhaseTimeLimit(float time) => _activePhaseTimeLimit.Value = time;

    public Phase GetCurrentPhase() => CurrentPhase;

    // Step phase forward
    public void Step()
    {
        // set the next phase
        switch ( CurrentPhase )
        {
            case Phase.PRELOAD:
                CurrentPhase = Phase.WARMUP;
                break;
            case Phase.WARMUP:
                CurrentPhase = Phase.ACTIVE;
                break;
            case Phase.ACTIVE:
                CurrentPhase = Phase.ENDGAME;
                break;
            case Phase.ENDGAME:
                CurrentPhase = Phase.WARMUP;
                break;
        };

        EmitPhaseChanged(new EventArgsPhaseChanged(CurrentPhase));
    }

    // Bypass intended stepping and set Phase directly.
    // Used for restarting a match, or early ending a match.
    public void HardSet(Phase phase)
    {
        CurrentPhase = phase;
        EmitPhaseChanged(new EventArgsPhaseChanged(CurrentPhase));
    }

    #endregion

    #region Private methods
    private void EmitPhaseChanged(EventArgsPhaseChanged e)
    {
        gameObject.BroadcastMessage("OnPhaseChanged", e);
    }

    private float GetCountdown()
    {
        return Countdown;
    }

    private void AddCountdown(float f)
    {
        SetCountdown(f + GetCountdown());
    }

    private void SetCountdown(float f)
    {
        Countdown = f;
        GameModeHandler.Instance.currentPhaseCountdown.Value = f;
    }
    #endregion
    
    #region Message Receivers
    private void FixedUpdate()
    {
        if (CurrentPhase != Phase.NULL) SetCountdown(GetCountdown() - Time.fixedDeltaTime);
    }

    // public void Awake()
    // {
    //     SetCountdown(1.0f);
    // }

    public void OnGameWon()
    {
        HardSet(Phase.ENDGAME);
    }
    #endregion
}

public class EventArgsPhaseChanged : EventArgs
{
    public Phase phase;
    public EventArgsPhaseChanged(Phase p) => phase = p;
}
