using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DevNetworkManager : MonoBehaviour
{
    [SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private Transform playerSpawnTransform;

    private float playerWaitTimer = 10f;

    private string desiredSceneName;

    private void Awake()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        desiredSceneName = SceneManager.GetActiveScene().name;

        if (FindFirstObjectByType<NetworkManager>() == null)
        {
            Instantiate(networkManagerPrefab);
            if (Unity.Multiplayer.PlayMode.CurrentPlayer.Tags.ToList().Contains("Host"))
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }
        }
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening &&
            GameManager.Instance && GameManager.Instance.isInitialized)
        {
            if (Unity.Multiplayer.PlayMode.CurrentPlayer.Tags.ToList().Contains("Host"))
            {
                // Wait for all players are joined.
                if (playerWaitTimer <= 0f) // TODO: Also check to see if gamemode is set?
                {
                    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
                    GameManager.Instance.LoadLevel(desiredSceneName);
                    playerWaitTimer = float.MaxValue; // Prevents multiple calls
                }
                else
                {
                    playerWaitTimer -= Time.deltaTime;
                }
            }
            else
            {
                if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponentInChildren<PlayerController>().isInitialized)
                {
                    Debug.Log("Player is initialized, preparing to join lobby...");
                    GameManager.Instance.PrepJoiningOtherLobby();
                    Destroy(gameObject);
                } 
            }
        }
    }

    private void OnServerStarted()
    {
        // TODO: Probably set the gamemode here
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        LevelManager.Instance.OnPlayersLoaded();
        
        Destroy(gameObject);
    }
}
