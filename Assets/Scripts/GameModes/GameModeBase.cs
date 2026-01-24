using UnityEngine;
using System.Collections.Generic;


/*
 *  - Manages game session -
 *
 *
 *  GameStats handles all StatEvents
 *      - All events in game will be tracked as a "StatEvent"
 *        - Bullets fired count toward a players' hitrate/missrate
 *        - Flag captures, drops, recoveries
 *        - Kills, deaths, assists
 *      - All game modes are point based, where points are given based on a certain type of StatEvent
 *        - Kills award points for Death match
 *        - Flag captures award points for CTF
 *        - Flag hold duration award points for rabbit
 *      - Points are awarded to players
 *        - Points are also awarded to teams based on which team the player is on
 *        - Player points do nothing but track stats
 *        - Team points trigger state change/win condition
 *          - This is to prevent players switching teams and triggering win condition with player points
 *      - Points accumulate to Team the player is on, if team based
 *      - On point change, check win condition
 *      - GameStat
 *
 */


public class GameModeBase : MonoBehaviour
{
    #region State
    private bool isInSession    = false;
    private bool isComplete     = false;
    private bool isDamageAllowed = false;
    #endregion

    #region Components
    [SerializeField] public TeamStructure team_structure;
    [SerializeField] public PhaseSystem phase_system;
    #endregion

    #region Public Methods
    public void LaunchSession()
    {
        phase_system.Step();
    }

    public void EndSession()
    {
        phase_system.HardSet(PhaseSystem.Phase.ENDGAME);
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
        phase_system.PhaseChanged += PhaseChangedEventHandler;
    }
    #endregion

    #region Event Handlers
    public void PhaseChangedEventHandler(object sender, EventArgsPhaseChanged e)
    {
		// if in active session, toggle state booleans appropriately
        switch( e.phase )
        {
            case PhaseSystem.Phase.ACTIVE:
				isInSession		= true;
				isComplete		= false;
				isDamageAllowed	= true;
				break;
            case _:
				isInSession		= false;
				isComplete		= true;
				isDamageAllowed	= false;
				break;
        }
    }
    #endregion
}
