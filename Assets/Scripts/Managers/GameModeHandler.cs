using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using UnityEngine.Events;


public enum GameModes
{
    NONE,
    DEATHMATCH,
    POWERSTRUGGLE,
    TEAMDEATHMATCH,
    STEALTHEINTEL,
}

public enum TeamBasedType
{
    NONE,
    SOLO,
    TEAM,
}

// Owns the current GameMode.
// Begins and ends the game.
// Must be notified to begin or end it (signalled by admin).
// GameModes will handle beginning/ending their own game session
// through PhaseSystem, once it has begun.
public class GameModeHandler : NetworkBehaviour
{
    public static GameModeHandler Instance { get; private set; } = null;

    // Is a game mode FFA? Are players going to have a team?
    public Dictionary<GameModes, TeamBasedType> gameModesTeamTypes = new()
    {
        { GameModes.NONE,           TeamBasedType.NONE },
        { GameModes.DEATHMATCH,     TeamBasedType.SOLO },
        { GameModes.POWERSTRUGGLE,  TeamBasedType.TEAM },
        { GameModes.TEAMDEATHMATCH, TeamBasedType.TEAM },
        { GameModes.STEALTHEINTEL,  TeamBasedType.TEAM },
    };
    public static Dictionary<GameModes, GameModeSO> availableGameModes = new();
    public GameModeBase currentGameMode;
    [SerializeField] private GameObject gameModeBasePrefabObj;

    public UnityEvent<StatEvent> OnStatUpdated = new();
    public UnityEvent<GameModes> OnGameModeChanged = new();
    public NetworkVariable<float> currentPhaseCountdown = new();
    public NetworkVariable<Phase> currentPhase = new();


    #region Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        OnStatUpdated.RemoveAllListeners();
        OnGameModeChanged.RemoveAllListeners();

        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsHost)
        {
            if (availableGameModes.Count == 0)
            {
                var gamemodeSOs = Resources.LoadAll<GameModeSO>("GameModeSOs");
                availableGameModes = new Dictionary<GameModes, GameModeSO>(gamemodeSOs.Length);
                
                foreach (GameModeSO obj in gamemodeSOs) if (obj.isAvailable) availableGameModes.Add(obj.gameModeName, obj);
            }
        }
    }
    #endregion


    public void StatEventReceiver(StatEvent s)
    {
        if (!IsHost || currentGameMode == null) return;
        currentGameMode.StatEventReceiver(s);
    }
    
    // Game mode can be selected at any time. player voting, admin selection, in-game host selection should all call from here.
    public void SelectNewMode(GameModeData gameModeData)
    {
        if (!IsHost) return;

        // Delete the current one
        if (currentGameMode != null)
        {
            // Remember, a GameModeBase is just a component of a GameObject
            // so delete the game object, this deletes GameModeBase
            Destroy(currentGameMode.gameObject);
        }
        currentGameMode = null;

        if (gameModeData == null)
        {
            currentPhaseCountdown.Value = 0f;
            currentPhase.Value = Phase.NULL;
            return;
        }

        // Clone the prefab, add it as a child to the GameModeHandler
        GameObject newGameModeObj = Instantiate(gameModeBasePrefabObj, gameObject.transform);
        newGameModeObj.GetComponent<NetworkObject>().Spawn();

        // Fetch the clone's GameModeBase component
        GameModeBase gameMode = newGameModeObj.GetComponent<GameModeBase>();
        currentGameMode = gameMode;

        if (gameModesTeamTypes[gameModeData.gameModeSO.gameModeName] == TeamBasedType.TEAM)
        {
            foreach (string teamName in gameModeData.gameModeSO.defaultTeamNames)
                currentGameMode.TeamStructure.AddTeam(teamName);
        }
        gameMode.GameModeId = gameModeData.gameModeSO.gameModeName;
        gameMode.WinConditions.Initialize(gameModeData.winConditionStatType, gameModeData.winConditionValue);
        gameMode.PhaseSystem.SetActivePhaseTimeLimit(gameModeData.timeLimitMinutes * 60);

        if (LevelManager.Instance) LevelManager.Instance.OnStageGenerated();
    }

    public void StartGame()
    {
        if (!IsHost || currentGameMode == null) return;
        currentGameMode.StartGame();
    }

    public TeamBasedType FetchTeamBasedType(GameModes g)
    {
        bool success = gameModesTeamTypes.TryGetValue(g, out TeamBasedType type);
        return success ? type : TeamBasedType.NONE;
    }

    public void OnCharacterJoined(ulong characterId)
    {
        if (!IsHost || currentGameMode == null) return;
        // Add character to stats
        currentGameMode.GameStats.CheckAddEntry(characterId, StatsGroup.PLAYER);

        Character character = CharacterManager.Instance.GetCharacterByEntityId(characterId);
        // Assign character to team
        if (gameModesTeamTypes[currentGameMode.GameModeId] == TeamBasedType.SOLO)
        {
            currentGameMode.TeamStructure.AssignCharacterToTeam(character);
        }
        else
        {
            int teamId = currentGameMode.TeamStructure.GetTeamWithFewestPlayers();
            currentGameMode.TeamStructure.AssignCharacterToTeam(character, teamId);
        }
    }

    [Rpc(SendTo.Server)]
    public void TriggerCharactersStatsDumpRpc(ulong clientId)
    {
        if (currentGameMode == null) return;
        currentGameMode.TriggerCharactersStatsDump(clientId); 
    }

    [Rpc(SendTo.Server)]
    public void TriggerGameModeUpdateRpc(ulong clientId)
    {
        if (currentGameMode == null) return;
        currentGameMode.TriggerGameModeUpdateRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }
}
