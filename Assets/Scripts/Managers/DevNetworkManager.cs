using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DevNetworkManager : MonoBehaviour
{
    [SerializeField] private GameObject networkManagerPrefab;

    public bool doAutoStart = true;
    private float playerWaitTimer = 10f;

    private string desiredSceneName;
    private GameModes desiredGameMode;

    private void Awake()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        desiredSceneName = SceneManager.GetActiveScene().name;
        desiredGameMode = GameModes.DEATHMATCH;

        if (FindFirstObjectByType<NetworkManager>() == null) Instantiate(networkManagerPrefab);
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening &&
            GameManager.Instance && GameManager.Instance.isInitialized)
        {
            if (Unity.Multiplayer.PlayMode.CurrentPlayer.Tags.ToList().Contains("Host") || !Application.isEditor)
            {
                // Wait for all players are joined.
                if (playerWaitTimer <= 0f)
                {
                    if (doAutoStart)
                    {
                        GameManager.Instance.SetLevel(desiredSceneName);
                        GameManager.Instance.SetGameModeData(GameModeHandler.availableGameModes[desiredGameMode].GetGameModeData());
                        GameManager.Instance.LoadLevel();
                    }
                    
                    Destroy(gameObject);
                }
                else
                {
                    playerWaitTimer -= Time.deltaTime;
                }
            }
            else
            {
                if (Player.Instance && Player.Instance.Character && Player.Instance.Character.isInitialized)
                {
                    GameManager.Instance.PrepJoiningOtherLobby();
                    Destroy(gameObject);
                } 
            }
        }
    }
}
