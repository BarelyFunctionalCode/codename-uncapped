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
    public event EventHandler<EventArgsPhaseChanged> PhaseChanged;
    #endregion
    
    #region Properties
    // Current phase
    public Phase CurrentPhase = Phase.PRELOAD;
    [SerializeField]
    private float Countdown;
    #endregion

    #region Public methods
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
            
        OnPhaseChanged(new EventArgsPhaseChanged(CurrentPhase));
    }

    // Bypass intended stepping and set Phase directly.
    // Used for restarting a match, or early ending a match.
    public void HardSet(Phase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged(new EventArgsPhaseChanged(CurrentPhase));
    }

    #region Pseudo
    private void FixedUpdate()
    {
        SetCountdown(GetCountdown() - Time.fixedDeltaTime);
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
        Countdown = f;
        if (Countdown <= 0)
        {
            Step();
        }

    }

    private void EnterNewPhase()
    {
        switch(CurrentPhase )
        {
            case Phase.PRELOAD:
                PreloadPhase();
                break;
            case Phase.WARMUP:
                WarmupPhase();
                break;
            case Phase.ACTIVE:
                ActivePhase();
                break;
            case Phase.ENDGAME:
                EndgamePhase();
                break;
        };
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
    public virtual void OnPhaseChanged(EventArgsPhaseChanged e)
    {
        PhaseChanged?.Invoke(this, e);
    }
    #endregion
}

public class EventArgsPhaseChanged : EventArgs
{
    public Phase phase;
    public EventArgsPhaseChanged(Phase p) => phase = p;
}
