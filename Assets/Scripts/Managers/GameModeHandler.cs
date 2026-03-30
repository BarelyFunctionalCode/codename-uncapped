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
    // Is a game mode FFA? Are players going to have a team?
    public Dictionary<GameModes, TeamBasedType> GameModesTeamTypes = new Dictionary<GameModes, TeamBasedType>() {
        { GameModes.NONE,           TeamBasedType.NONE },
        { GameModes.DEATHMATCH,     TeamBasedType.SOLO },
        { GameModes.POWERSTRUGGLE,  TeamBasedType.TEAM },
        { GameModes.TEAMDEATHMATCH, TeamBasedType.TEAM },
        { GameModes.STEALTHEINTEL,  TeamBasedType.TEAM },
    };

    private static GameModeHandler _instance;
    public static GameModeHandler Instance
    {
        get
        {
            return _instance;
        }
    }

    public GameModeBase current_game_mode;

    public UnityEvent<StatEvent> OnStatUpdated = new();
    public UnityEvent<EventArgsPlayerChangedTeam> OnPlayerChangedTeam = new();
    public UnityEvent<GameModes> OnGameModeChanged = new();

    #region Gamemode prefab cache
    public static Dictionary<GameModes, GameObject> game_mode_cache = new();
    #endregion


    #region Public Methods
    public void StatEventReceiver(StatEvent s)
    {
        if (!IsHost || current_game_mode == null) return;
        current_game_mode.StatEventReceiver(s);
    }
    
    // Game mode can be selected at any time. player voting, admin selection, in-game host selection should all call from here.
    // Psuedo
    public void SelectNewMode(GameModes g)
    {
        if (!IsHost) return;

        // Delete the current one
        if (current_game_mode)
        {
            // Remember, a GameModeBase is just a component of a GameObject
            // so delete the game object, this deletes GameModeBase
            Destroy(current_game_mode.gameObject);
        }

        // Clone the prefab, add it as a child to the GameModeHandler
        GameObject cloned_game_mode_object = Instantiate(game_mode_cache[g], this.gameObject.transform);
        cloned_game_mode_object.GetComponent<NetworkObject>().Spawn();

        // Fetch the clone's GameModeBase component
        GameModeBase game_mode = cloned_game_mode_object.GetComponent<GameModeBase>();
        current_game_mode = game_mode;

        SelectNewModeRpc(g);

        if (LevelManager.Instance) LevelManager.Instance.OnStageGenerated();
    }

    [Rpc(SendTo.Everyone)]
    private void SelectNewModeRpc(GameModes g)
    {
        OnGameModeChanged.Invoke(g);
    }

    public void StartGame()
    {
        if (!IsHost || current_game_mode == null) return;
        current_game_mode.StartGame(new List<string>());
    }

    public TeamBasedType FetchTeamBasedType(GameModes g)
    {
        TeamBasedType type;
        bool success = GameModesTeamTypes.TryGetValue(g, out type);
        return success ? type : TeamBasedType.NONE;
    }

    #endregion

    #region Message Receivers
    private void Awake()
    {
        // Init singleton logic
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }

        else
        {
            _instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsHost)
        {
            if (game_mode_cache.Count == 0)
            {
                var gamemodeObjects = Resources.LoadAll<GameObject>("Prefabs/GameModes/Variants");
                game_mode_cache = new Dictionary<GameModes, GameObject>(gamemodeObjects.Length);
                foreach (GameObject obj in gamemodeObjects)
                {
                    if (Enum.TryParse(obj.name, out GameModes gameMode))game_mode_cache.Add(gameMode, obj);
                    else Debug.LogWarning($"GameModeHandler: Failed to parse GameMode from prefab name {obj.name}");
                }
            }
            GameManager.Instance.OnClientConnectedEvent.AddListener(OnClientJoined);
        }
    }

    public void OnClientJoined(ulong ClientID)
    {
        if (!IsHost || current_game_mode == null) return;
        if (GameModesTeamTypes[current_game_mode.game_mode_id] == TeamBasedType.SOLO)
        {
            // Player joined the server, auto-assign team if FFA style game mode
            ulong entityId = NetworkManager
                .Singleton
                .ConnectedClients[ClientID]
                .PlayerObject
                .GetComponent<PlayerController>()
                .EntityId;

            current_game_mode?.GetComponent<TeamStructure>().SetPlayerTeamFFA(entityId);
        }
    }

    #endregion
}
