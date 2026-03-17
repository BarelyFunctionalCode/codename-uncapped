using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DevNetworkManager : MonoBehaviour
{
    [SerializeField] private GameObject networkManagerPrefab;

    private float playerWaitTimer = 10f;

    private string desiredSceneName;
    private GameModes desiredGameMode;

    private void Awake()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        desiredSceneName = SceneManager.GetActiveScene().name;
        desiredGameMode = GameModes.FFA;

        if (FindFirstObjectByType<NetworkManager>() == null) Instantiate(networkManagerPrefab);
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
                if (playerWaitTimer <= 0f)
                {
                    GameManager.Instance.SetLevel(desiredSceneName);
                    GameManager.Instance.SetGameMode(desiredGameMode);
                    GameManager.Instance.LoadLevel();
                    
                    Destroy(gameObject);
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
}
