using UnityEngine;
using System;
using System.Collections.Generic;


public enum Phase
{
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

public class PhaseSystem : MonoBehaviour
{
    #region Delegates & Events
    public event EventHandler? PhaseChanged;
    public delegate void PhaseChangedEventHandler(object sender, EventArgsPhaseChanged e);
    #endregion
    
    #region Properties
    // Current phase
    public Phase CurrentPhase = Phase.PRELOAD;
    private float Countdown;
    #endregion

    #region Public methods
    // Step phase forward
    public bool Step()
    {
        // set the next phase
        CurrentPhase switch
        {
            ( Phase.PRELOAD, Phase.ENDGAME )=> CurrentPhase = Phase.WARMUP,
            ( Phase.WARMUP )                => CurrentPhase = Phase.ACTIVE,
            ( Phase.ACTIVE )                => CurrentPhase = Phase.ENDGAME,
        }
            
        OnPhaseChanged(new EventArgsPhaseChanged(next_phase));
    }

    // Bypass intended stepping and set Phase directly.
    // Used for restarting a match, or early ending a match.
    public void HardSet(Phase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged(new EventArgsPhaseChanged(next_phase));
    }

    #region Pseudo
    private void FixedUpdate()
    {
        SetCountdown(GetCountdown() - Time.fixedDeltaTime)
    }

    public void Awake()
    {
        SetCountdown(15.0f);
    }
    #endregion
    #endregion

    #region Private methods

    private void ActivePhase()
    {
        // Enable all player damage
        // Begin a countdown timer until game ends
        SetCountdown(600.0f);
        // Signal to load players to spawn points
    }

    private void EndgamePhase()
    {
        // Disable all player damage
        // Begin a countdown timer until next warmup phase
        SetCountdown(30.0f);
    }

    private float GetCountdown()
    {
        return Countdown;
    }

    private void SetCountdown(float f)
    {
        countdown = f;
        if (countdown <= 0)
        {
            Step()
        }

    }

    private void EnterNewPhase()
    {
        CurrentPhase switch
        {
            ( Phase.PRELOAD )   => PreloadPhase(),
            ( Phase.WARMUP )    => WarmupPhase(),
            ( Phase.ACTIVE )    => ActivePhase(),
            ( Phase.ENDGAME )   => EndgamePhase(),
        }
    }

    private void PreloadPhase()
    {
        // Nothing is needed here, its only purpose is to be an empty step before WARMUP
    }

    private void WarmupPhase()
    {
        // Disable all player damage
        // Begin a countdown timer until game starts
        SetCountdown(30.0f);
        // Enable voting for next map?
    }

    #endregion
    
    #region Protected methods
    protected virtual void OnPhaseChanged(EventArgsPhaseChanged e)
    {
        PhaseChanged?.Invoke(this, e);
    }
    #endregion
}

public class EventArgsPhaseChanged : EventArgs
{
    public Phase phase { get; set; }
}