using System.Collections.Generic;
using System.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Dynamic;


public class GameManager : MonoBehaviour
{
    public bool debugMode = false;

    private int maxLobbySize = 20;

    public bool isInitialized = false;
    public static GameManager Instance { get; private set; } = null;
    private FacepunchTransport facepunchTransport;
    private ulong selectedLobbyId = 0;
	public Lobby? CurrentLobby { get; private set; } = null;
    public List<Lobby> Lobbies { get; private set; } = new List<Lobby>(capacity: 100);

    private UnityTransport unityTransport;
    private ushort unityTransportDesiredPort = 7777;

    public bool usingSteam = false;

    private bool wantToJoin = false;
    private bool wantToLeave = false;

    private string levelLoaded = "";
    private bool activeSceneChanged = false;
    private Scene previousScene;
    private bool sceneUnloaded = false;
    public UnityEvent OnLevelLoadedEvent = new();


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Find out if we are using either Facepunch or Unity transport
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType() == typeof(FacepunchTransport) &&
            NetworkManager.Singleton.transform.Find("FacepunchTransport").TryGetComponent(out facepunchTransport) &&
            facepunchTransport.enabled)
        {
            usingSteam = true;
            if (!SteamClient.IsValid)
            {
                if (debugMode) Debug.LogError("Steamworks not initialized!", this);
                return;
            }

            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
        }
        else unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        NetworkManager.Singleton.OnClientStopped += TransitionLobby;
        StartHost();

        // Load the home Lobby scene
        SceneManager.activeSceneChanged += SetInitialized;
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }
    
    private void Update()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsHost) return;
        if (levelLoaded != "")
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLevelLoaded;
            SceneManager.activeSceneChanged += ChangedActiveScene;

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(levelLoaded));

            levelLoaded = "";
        }
        else if (activeSceneChanged)
        {
            SceneManager.activeSceneChanged -= ChangedActiveScene;
            NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted += OnSceneUnloaded;

            OnLevelLoadedEvent.Invoke();

            NetworkManager.Singleton.SceneManager.UnloadScene(previousScene);

            activeSceneChanged = false;
        }
        else if (sceneUnloaded)
        {
            NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted -= OnSceneUnloaded;

            sceneUnloaded = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (usingSteam)
        {
            SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        }

        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
		NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnClientStopped -= TransitionLobby;
    }

    private void SetInitialized(Scene _, Scene scene)
    {
        if (scene.name != "Lobby") return;
        isInitialized = true;
        FindAnyObjectByType<GameStart>()?.IsLoaded();
        SceneManager.activeSceneChanged -= SetInitialized;
    }

    private void OnApplicationQuit() => Disconnect();


    // Set's desired port from the JoinGameUI
    public void SetUnityTransportDesiredPort(ushort port) => unityTransportDesiredPort = port;

    // Set's the selected lobby id from the JoinGameUI
    public void SetSelectedLobbyId(ulong id) => selectedLobbyId = id;

    // Whenever the local client disconnects, connect to host or start new server
    private void TransitionLobby(bool _)
    {
        if (wantToJoin)
        {
            wantToJoin = false;
            JoinOtherLobby();
        }
        else if (wantToLeave)
        {
            wantToLeave = false;
            GoToOwnLobby();
        }
    }

    public async void StartHost()
	{
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
		NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
		NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        
        if (!usingSteam)
        {
            // If using the Unity Transport, set a random port for the server
            System.Random random = new();
            unityTransportDesiredPort = (ushort)random.Next(7000, 8001);
            if (Unity.Multiplayer.PlayMode.CurrentPlayer.Tags.Contains("Host")) unityTransportDesiredPort = 2000;
            unityTransport.SetConnectionData("127.0.0.1", unityTransportDesiredPort, "0.0.0.0");
        }
        // Start the server
		NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;

        // If using Steam, create a lobby
		if (usingSteam) CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(maxLobbySize);
    }

    public void StartClient(ulong hostSteamId)
	{
		NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

        // Set the connection data for the client depending on the type of transport
        if (usingSteam) facepunchTransport.targetSteamId = hostSteamId;
        else unityTransport.SetConnectionData("127.0.0.1", unityTransportDesiredPort);

		if (NetworkManager.Singleton.StartClient() && debugMode) Debug.Log("Client has joined!", this);
	}

	public void Disconnect()
	{
		if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject()?.GetComponentInChildren<OldPlayer>()?.DisconnectCleanupRpc();

		if (usingSteam) CurrentLobby?.Leave();

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnPlayerLoaded;
        }
        // if (PlayerManager.Instance != null) PlayerManager.Instance.Clear();
		NetworkManager.Singleton.Shutdown();
	}


    public async Task<bool> RefreshLobbies(int maxResults = 50)
	{
        if (!usingSteam) return false;

        // Get a list of Steam lobbies for populating the JoinGameUI
		try
		{
			Lobbies.Clear();

            var lobbies = await SteamMatchmaking.LobbyList
                        .WithKeyValue("description", "LUTest")
                        .FilterDistanceClose()
                        .WithMaxResults(maxResults)
                        .RequestAsync();

            for (int i = 0; i < lobbies?.Length; i++) Lobbies.Add(lobbies[i]);

            return true;
		}
		catch (System.Exception ex)
		{
			Debug.Log("Error fetching lobbies", this);
			Debug.LogException(ex, this);
			return false;
		}
	}

    






    // Called when the local player wants to join another lobby
    public void PrepJoiningOtherLobby()
    {
        if (!Unity.Multiplayer.PlayMode.CurrentPlayer.Tags.Contains("Host")) unityTransportDesiredPort = 2000;
        if (!usingSteam && unityTransport.ConnectionData.Port == unityTransportDesiredPort) return;
        if (usingSteam && facepunchTransport.targetSteamId == selectedLobbyId) return;

        // Close the server on your own lobby
        wantToJoin = true;
        Disconnect();
    }

    // Called after the local player has successfully shut down their server
    private async void JoinOtherLobby()
    {
        // Load the loading scene
        await SceneManager.LoadSceneAsync("Loading");

        if (usingSteam) CurrentLobby = await SteamMatchmaking.JoinLobbyAsync(selectedLobbyId);
        else StartClient(0);
    }







    // Called when the local player wants to go back to their own lobby
    public void PrepGoToOwnLobby()
    {
        // Close the client you current have
        wantToLeave = true;
        Disconnect();
    }

    // Called after the local player has successfully disconnected from the other lobby
    private async void GoToOwnLobby()
    {
        // Load the loading scene
        await SceneManager.LoadSceneAsync("Loading");

        // Start new server
        StartHost();

        // Go back home...
        NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }





    
    // TODO: Not sure if this works properly
    // When you accept a Steam game invite from a friend
    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
	{
		bool isSame = lobby.Owner.Id.Equals(id);

        if (debugMode) 
        {
            Debug.Log($"Owner: {lobby.Owner}");
            Debug.Log($"Id: {id}");
            Debug.Log($"IsSame: {isSame}", this);
        }

		StartClient(id);
	}

	private void OnLobbyInvite(Friend friend, Lobby lobby) { if (debugMode) Debug.Log($"You got a invite from {friend.Name}", this); }

	private void OnLobbyMemberLeave(Lobby lobby, Friend friend) { }

	private void OnLobbyMemberJoined(Lobby lobby, Friend friend) { }

	private void OnLobbyEntered(Lobby lobby)
    {
		if (debugMode) Debug.Log($"You have entered in lobby, clientId={NetworkManager.Singleton.LocalClientId}", this);

		if (NetworkManager.Singleton.IsHost) return;

		StartClient(lobby.Owner.Id);
	}

    private void OnLobbyCreated(Result result, Lobby lobby)
	{
		if (result != Result.OK)
        {
			if (debugMode) Debug.LogError($"Lobby couldn't be created!, {result}", this);
			return;
		}

		// lobby.SetFriendsOnly(); // Set to friends only!
		lobby.SetData("name", SteamClient.Name + "'s Lobby");
        lobby.SetData("description", "LUTest");
		lobby.SetJoinable(true);

		if (debugMode) Debug.Log("Lobby has been created!");
	}

    private void ClientConnected(ulong clientId) { if (debugMode) Debug.Log($"I'm connected, clientId={clientId}"); }

    private void ClientDisconnected(ulong clientId)
	{
		if (debugMode) Debug.Log($"I'm disconnected, clientId={clientId}");

        NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
		NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
	}

	private void OnServerStarted() {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnPlayerLoaded;
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (debugMode) Debug.Log($"Client connected, clientId={clientId}", this);
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        if (debugMode) Debug.Log($"Client disconnected, clientId={clientId}", this);

        // if (NetworkManager.Singleton.IsHost) PlayerManager.Instance.RemovePlayer(clientId);
    }


    // TODO: Move to LevelManager script
    public void LoadLevel(string levelSceneName) {
        if (!NetworkManager.Singleton.IsHost) return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLevelLoaded;
        NetworkManager.Singleton.SceneManager.LoadScene(levelSceneName, LoadSceneMode.Additive);
    }

    // Done when a single player is loaded into the Level scene
    private void OnPlayerLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (debugMode) Debug.Log($"Player loaded, clientId={clientId}, sceneName={sceneName}, loadSceneMode={loadSceneMode}");
    }

    // Done when all players are loaded into the Level scene
    private void OnLevelLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        if (debugMode) Debug.Log($"Level loaded, sceneName={sceneName}, loadSceneMode={loadSceneMode}, clientsCompleted={clientsCompleted.Count}, clientsTimedOut={clientsTimedOut.Count}");
        levelLoaded = sceneName;
    }

    // When the server's active scene has finished changing
    private void ChangedActiveScene(Scene current, Scene next)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        if (debugMode) Debug.Log($"Changed active scene from {current.name} to {next.name}");
        previousScene = current;

        activeSceneChanged = true;
    }

    // When the previous scene has been unloaded for all clients and the server
    private void OnSceneUnloaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        if (debugMode) Debug.Log($"Scene unloaded, sceneName={sceneName}");
        sceneUnloaded = true;
    }
}
