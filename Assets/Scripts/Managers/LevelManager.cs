using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; } = null;

    public static List<LevelSO> availableLevels = new();

    private bool playersLoaded = false;
    private bool stageGenerated = false;

    private bool isInitialized = false;

    Dictionary<uint, List<Transform>> spawnPoints = new();

    private void Awake()
    {
        if (Instance != null || GameManager.Instance == null || !GameManager.Instance.isInitialized) Destroy(gameObject);
        else Instance = this;

        GameManager.Instance.OnLevelLoadedEvent.AddListener(OnLevelLoadedEvent);

        transform.SetParent(null);
	}

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (GameManager.Instance == null || !GameManager.Instance.isInitialized) return;
        if (IsHost) GameModeHandler.Instance.currentPhase.OnValueChanged += OnGameModePhaseChange;
    }

    public sealed override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (GameModeHandler.Instance != null) GameModeHandler.Instance.currentPhase.OnValueChanged -= OnGameModePhaseChange;
    }

    public override void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnLevelLoadedEvent.RemoveListener(OnLevelLoadedEvent);
        if (Instance == this) Instance = null;
    }


    private void OnLevelLoadedEvent(string levelName)
    {
        if (levelName == gameObject.scene.name)
        {
            OnPlayersLoaded();
        }
    }

    public static void GenerateAvailableLevelsList()
    {
        availableLevels.Clear();
        LevelSO[] levelSOs = Resources.LoadAll<LevelSO>("LevelSOs");
        availableLevels.AddRange(levelSOs);
    }

    // Called after all the players have loaded into the scene
    private void OnPlayersLoaded()
    {
        if (!NetworkManager.Singleton.IsHost || isInitialized) return;
        playersLoaded = true;

        OnLevelInitialized();
    }

    // Called after any runtime generation for the scene has finished
    public void OnStageGenerated()
    {
        if (!NetworkManager.Singleton.IsHost || isInitialized) return;
        stageGenerated = true;

        if (GameModeHandler.Instance.GameModesTeamTypes[GameModeHandler.Instance.current_game_mode.game_mode_id] == TeamBasedType.SOLO)
        {
            GameObject[] teamBaseObjs = GameObject.FindGameObjectsWithTag("TeamBase");
            foreach (var teamBaseObj in teamBaseObjs)            {
                teamBaseObj.SetActive(false);
            }
        }

        OnLevelInitialized();
    }

    private void OnGameModePhaseChange(Phase previousPhase, Phase newPhase)
    {
        if (newPhase == Phase.ACTIVE)
        {
            foreach (var player in NetworkManager.Singleton.SpawnManager.PlayerObjects)
            {
                PlayerController playerController = player.GetComponentInChildren<PlayerController>();
                if (playerController == null) continue;
                playerController.Suicide();
            }
        }

        if (previousPhase == Phase.ENDGAME)
        {
            // TODO: Post Match UI
            GameManager.Instance.SetLevel("Lobby");
            GameManager.Instance.LoadLevel();
        }
    }


    private void OnLevelInitialized()
    {
        if (!playersLoaded || !stageGenerated) return;

        spawnPoints.Clear();
        GameObject[] spawnPointsObjs = GameObject.FindGameObjectsWithTag("Respawn");
        foreach (var spawnPointObj in spawnPointsObjs)
        {
            uint teamId = uint.Parse(spawnPointObj.name);

            if (!spawnPoints.ContainsKey(teamId)) spawnPoints[teamId] = new List<Transform>();
            spawnPoints[teamId].Add(spawnPointObj.transform);
        }

        foreach (var player in NetworkManager.Singleton.SpawnManager.PlayerObjects)
        {
            PlayerController playerController = player.GetComponentInChildren<PlayerController>();
            if (playerController == null) continue;

            GameModeHandler.Instance.OnClientJoined(playerController.EntityId);
            
            Transform spawnPoint = GetSpawnPoint(playerController.TeamId);
            if (spawnPoint == null) continue;
            
            playerController.Teleport(spawnPoint.position, spawnPoint.rotation);
            playerController.SetPlayerControlsRpc(false);
            playerController.SetHUDActiveRpc(true);
            // playerController.OpenLoadoutMenuRpc();
        }

        OnLevelReady();
        isInitialized = true;
    }

    private void OnLevelReady()
    {
        // Do things to start the level

        // Give players control
        foreach (var player in NetworkManager.Singleton.SpawnManager.PlayerObjects)
        {
            PlayerController playerController = player.GetComponentInChildren<PlayerController>();
            if (playerController == null) continue;
            playerController.SetPlayerControlsRpc(true);
        }

        // Start the game mode
        GameModeHandler.Instance.StartGame();
    }

    public Transform GetSpawnPoint(uint teamId)
    {
        if (GameModeHandler.Instance.GameModesTeamTypes[GameModeHandler.Instance.current_game_mode.game_mode_id] == TeamBasedType.TEAM)
        {
            return GetTeamSpawnPoint(teamId);
        }
        else
        {
            return GetSoloSpawnPoint();
        }
    }

    private Transform GetTeamSpawnPoint(uint teamId)
    {
        if (spawnPoints.ContainsKey(teamId) && spawnPoints[teamId].Count > 0)
        {
            for (int i = 0; i < spawnPoints[teamId].Count; i++)
            {
                Transform spawnPoint = spawnPoints[teamId][i];
                RaycastHit[] hits = new RaycastHit[3];
                int hitCount = Physics.CapsuleCastNonAlloc(
                    spawnPoint.position + Vector3.up * 2f,
                    spawnPoint.position,
                    0.5f,
                    Vector3.down,
                    hits,
                    0.001f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                );
                if (hitCount > 0)
                {
                    bool occupied = false;
                    for (int j = 0; j < hitCount; j++)  
                    {
                        if (hits[j].collider.gameObject.TryGetComponent<PlayerController>(out _))
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (occupied) continue;
                }
                
                return spawnPoint;
            }
        }
        return null;
    }

    private Transform GetSoloSpawnPoint()
    {
        float maxTries = 10;
        float spawnRadiusRatio = 0.75f; // How close to the center of the map the spawn points should be, between 0 and 1

        // Get map size and center from terrain in level
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return null;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainCenter = terrain.transform.position + terrainSize / 2f;

        // Get list of current players and their positions
        List<Vector3> playerPositions = new List<Vector3>();
        foreach (var player in NetworkManager.Singleton.SpawnManager.PlayerObjects)
        {
            PlayerController playerController = player.GetComponentInChildren<PlayerController>();
            if (playerController == null) continue;
            playerPositions.Add(playerController.transform.position);
        }

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(terrainCenter.x - terrainSize.x / 2f * spawnRadiusRatio, terrainCenter.x + terrainSize.x / 2f * spawnRadiusRatio),
                terrainCenter.y + terrainSize.y,
                Random.Range(terrainCenter.z - terrainSize.z / 2f * spawnRadiusRatio, terrainCenter.z + terrainSize.z / 2f * spawnRadiusRatio)
            );

            RaycastHit hit;
            if (Physics.Raycast(randomPos, Vector3.down, out hit, terrainSize.y * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject.TryGetComponent<Terrain>(out _))
                {
                    bool occupied = false;
                    foreach (var playerPos in playerPositions)
                    {
                        if (Vector3.Distance(hit.point, playerPos) < 5f)
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (occupied) continue;

                    GameObject spawnPoint = new GameObject("SoloSpawnPoint");
                    spawnPoint.transform.SetPositionAndRotation(hit.point + Vector3.up * 2f, Quaternion.identity);
                    Destroy(spawnPoint, 5f); // Cleanup spawn point after 5 seconds
                    return spawnPoint.transform;
                }
            }
        }
        return null;
    }
}
