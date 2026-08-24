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

    Dictionary<int, List<Transform>> spawnPoints = new();

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
        if (IsHost) {
            GameModeHandler.Instance.currentPhase.OnValueChanged += OnGameModePhaseChange;
            CharacterManager.Instance.OnCharacterAdded.AddListener(OnNewCharacterAdded);
        }
    }

    public sealed override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (GameModeHandler.Instance != null) GameModeHandler.Instance.currentPhase.OnValueChanged -= OnGameModePhaseChange;
        if (CharacterManager.Instance != null) CharacterManager.Instance.OnCharacterAdded.RemoveListener(OnNewCharacterAdded);
    }

    public override void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnLevelLoadedEvent.RemoveListener(OnLevelLoadedEvent);
        if (Instance == this) Instance = null;
    }

    private void OnNewCharacterAdded(ulong characterId)
    {
        if (!IsHost) return;

        Character character = CharacterManager.Instance.GetCharacterByEntityId(characterId);
        if (character == null) return;

        Identification entityIdentification = character.identification;

        GameModeHandler.Instance.OnCharacterJoined(entityIdentification.FetchEntityId());
        if (GameManager.Instance.debugMode) Debug.Log("LevelManager: New character added to Level: " + entityIdentification.FetchEntityName() + " (ID: " + entityIdentification.FetchEntityId() + ")");
        
        Transform spawnPoint = GetSpawnPoint(entityIdentification.FetchTeamId());
        if (spawnPoint == null) return;

        character.Teleport(spawnPoint.position, spawnPoint.rotation);
        character.characterInputs.SetHUDActiveRpc(true);
        character.characterInputs.SetCharacterControlsRpc(true);
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
        if (!IsServer || isInitialized) return;
        playersLoaded = true;

        OnLevelInitialized();
    }

    // Called after any runtime generation for the scene has finished
    public void OnStageGenerated()
    {
        if (!IsServer || isInitialized) return;
        stageGenerated = true;

        OnStageGenerationRpc(GameModeHandler.Instance.gameModesTeamTypes[GameModeHandler.Instance.currentGameMode.GameModeId] == TeamBasedType.SOLO);

        OnLevelInitialized();
    }

    [Rpc(SendTo.Everyone)]
    private void OnStageGenerationRpc(bool soloGameMode)
    {
        if (soloGameMode)
        {
            GameObject[] teamBaseObjs = GameObject.FindGameObjectsWithTag("TeamBase");
            foreach (var teamBaseObj in teamBaseObjs)            {
                teamBaseObj.SetActive(false);
            }
        }
    }

    private void OnGameModePhaseChange(Phase previousPhase, Phase newPhase)
    {
        if (newPhase == Phase.ACTIVE)
        {
            foreach (Character character in CharacterManager.Instance.characters)
            {
                if (character == null || character.state == null) continue;
                character.state.Die();
            }


            if (!IsServer) return;

            GameObject[] powerCores = GameObject.FindGameObjectsWithTag("PowerCore");
            foreach (var powerCore in powerCores)
            {
                PowerCorePickup pickup = powerCore.GetComponent<PowerCorePickup>();
                if (pickup == null) continue;
                pickup.CanBePickedUp.Value = true;
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
        if (GameManager.Instance.debugMode) Debug.Log("LevelManager: OnLevelInitialized() called. PlayersLoaded: " + playersLoaded + ", StageGenerated: " + stageGenerated);
        if (!playersLoaded || !stageGenerated) return;

        spawnPoints.Clear();
        GameObject[] spawnPointsObjs = GameObject.FindGameObjectsWithTag("Respawn");
        foreach (var spawnPointObj in spawnPointsObjs)
        {
            int teamId = int.Parse(spawnPointObj.name);

            if (!spawnPoints.ContainsKey(teamId)) spawnPoints[teamId] = new List<Transform>();
            spawnPoints[teamId].Add(spawnPointObj.transform);
        }

        if (GameManager.Instance.debugMode) Debug.Log("LevelManager: OnLevelInitialized() called. Characters in scene: " + CharacterManager.Instance.characters.Count);
        foreach (Character character in CharacterManager.Instance.characters)
        {
            if (character == null) continue;

            Identification entityIdentification = character.identification;

            GameModeHandler.Instance.OnCharacterJoined(entityIdentification.FetchEntityId());
            
            Transform spawnPoint = GetSpawnPoint(entityIdentification.FetchTeamId());
            if (spawnPoint == null) spawnPoint = transform; // If no spawn point is found, just use the LevelManager's position as a fallback
            
            character.Teleport(spawnPoint.position, spawnPoint.rotation);
            character.characterInputs.SetHUDActiveRpc(true);
            character.characterInputs.SetCharacterControlsRpc(false);
        }

        OnLevelReady();
        isInitialized = true;
    }

    private void OnLevelReady()
    {
        // Do things to start the level

        // Give characters control
        foreach (Character character in CharacterManager.Instance.characters)
        {
            if (character == null) continue;
            character.characterInputs.SetCharacterControlsRpc(true);
        }

        // Start the game mode
        GameModeHandler.Instance.StartGame();
    }

    public Transform GetSpawnPoint(int teamId)
    {
        if (GameModeHandler.Instance.gameModesTeamTypes[GameModeHandler.Instance.currentGameMode.GameModeId] == TeamBasedType.TEAM)
        {
            return GetTeamSpawnPoint(teamId);
        }
        else
        {
            return GetSoloSpawnPoint();
        }
    }

    private Transform GetTeamSpawnPoint(int teamId)
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
                        if (hits[j].collider.gameObject.TryGetComponent<Character>(out _))
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
        if (terrain == null)
        {
            GameObject spawnPoint = new GameObject("SoloSpawnPoint");
                spawnPoint.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Destroy(spawnPoint, 5f); // Cleanup spawn point after 5 seconds
                return spawnPoint.transform;
        }
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainCenter = terrain.transform.position + terrainSize / 2f;

        // Get list of current characters and their positions
        List<Vector3> characterPositions = new List<Vector3>();
        foreach (Character character in CharacterManager.Instance.characters)
        {
            if (character == null) continue;
            characterPositions.Add(character.transform.position);
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
                    foreach (var characterPos in characterPositions)
                    {
                        if (Vector3.Distance(hit.point, characterPos) < 5f)
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
