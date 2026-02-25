using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; } = null;

    private bool playersLoaded = false;
    private bool stageGenerated = true; // TODO: Set to false when we have actual stage generation

    Dictionary<int, List<Transform>> spawnPoints = new Dictionary<int, List<Transform>>();

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        transform.SetParent(null);
	}

    // Called after all the players have loaded into the scene
    public void OnPlayersLoaded()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        playersLoaded = true;

        OnLevelInitialized();
    }

    // Called after any runtime generation for the scene has finished
    public void OnStageGenerated()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        stageGenerated = true;

        OnLevelInitialized();
    }


    private void OnLevelInitialized()
    {
        if (!playersLoaded || !stageGenerated) return;

        spawnPoints.Clear();
        GameObject[] spawnPointsObjs = GameObject.FindGameObjectsWithTag("Respawn");
        foreach (var spawnPointObj in spawnPointsObjs)
        {
            int teamId = int.Parse(spawnPointObj.name);

            if (!spawnPoints.ContainsKey(teamId)) spawnPoints[teamId] = new List<Transform>();
            spawnPoints[teamId].Add(spawnPointObj.transform);
        }

        foreach (var player in NetworkManager.Singleton.SpawnManager.PlayerObjects)
        {
            PlayerController playerController = player.GetComponentInChildren<PlayerController>();
            if (playerController == null) continue;

            int teamId = 0; // TODO: Get team ID from player data
            
            Transform spawnPoint = GetSpawnPoint(teamId);
            if (spawnPoint == null) continue;
            
            playerController.TeleportRpc(spawnPoint.position, spawnPoint.rotation);
            playerController.SetPlayerControlsRpc(false);
        }

        OnLevelReady();
    }

    public void OnLevelReady()
    {
        // Do things to start the level
        foreach (var player in NetworkManager.Singleton.SpawnManager.PlayerObjects)
        {
            PlayerController playerController = player.GetComponentInChildren<PlayerController>();
            if (playerController == null) continue;
            playerController.SetPlayerControlsRpc(true);
        }
    }

    public Transform GetSpawnPoint(int teamId)
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
}


