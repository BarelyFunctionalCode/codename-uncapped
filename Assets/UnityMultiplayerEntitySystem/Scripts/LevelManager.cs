using Unity.Netcode;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; } = null;

    private bool playersLoaded = false;
    private bool stageGenerated = false;

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

        OnLevelReady();
    }

    public void OnLevelReady()
    {
        // Do things to start the level
    }
}


