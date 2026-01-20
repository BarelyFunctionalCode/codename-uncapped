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


public class PhaseSystem : MonoBehaviour
{
    // List of phases
    private List<Phase> Phases = new List<Phase> { Phase.WARMUP, Phase.ACTIVE, Phase.ENDGAME };

    // Current phase
    public Phase CurrentPhase = Phase.PRELOAD;

    private float Countdown;


    // Step phase forward
    public bool Step()
    {
        bool Success = true;
        int phase_count = Enum.GetNames(typeof(Phase)).Length;
        int index = Phases.IndexOf(CurrentPhase);

        Phase next_phase = Phases[phase_count % index];
        CurrentPhase = next_phase;

        return Success;
    }

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

    }

    private void ActivePhase()
    {
        // Enable all player damage
        // Begin a countdown timer until game ends

    }

    // Bypass intended stepping and set Phase directly.
    // Used for restarting a match, or early ending a match.
    public bool HardSet()
    {
        bool Success = true;


        return Success;
    }

}
