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
    #region Properties
    // List of phases
    private List<Phase> Phases = new List<Phase> { Phase.PRELOAD, Phase.WARMUP, Phase.ACTIVE, Phase.ENDGAME };
    // Current phase
    public Phase CurrentPhase = Phase.PRELOAD;
    private float Countdown;
    #endregion

    #region Public methods
    // Step phase forward
    public bool Step()
    {
        bool Success = true;

        #region Psuedo
        // set the next phase
        if ((Currentphase == Phase.PRELOAD) || (Currentphase == Phase.ENDGAME)):
            next_phase = Phases[Phase.WARMUP];
        if Currentphase == Phase.WARMUP:
            next_phase = Phases[Phase.ACTIVE];
        if (Currentphase == Phase.ACTIVE):
            next_phase = Phases[Phase.ENDGAME];

        CurrentPhase = next_phase;
        PhaseChanged();
        #endregion

        return Success;
    }


    // Bypass intended stepping and set Phase directly.
    // Used for restarting a match, or early ending a match.
    public bool HardSet(Phase phase)
    {
        bool Success = true;


        return Success;
    }

    #region Pseudo
    private void Update()
    {
        SetCountdown(GetCountdown() - Time.delta)
    }

    public void Start()
    {
        Countdown = 15.0f;

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

    #region Pseudo
    private void SetCountdown(float f)
    {
        countdown = f;
        if (countdown <= 0)
        {
            Step()
        }

    }
    #endregion

    private void PhaseChanged()
    {
        match CurrentPhase:
        Phase.PRELOAD:
        PreloadPhase()
        Phase.WARMUP:
        WarmupPhase()
        Phase.ACTIVE:
        ActivePhase()
        Phase.ENDGAME:
        EndgamePhase()
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

}
