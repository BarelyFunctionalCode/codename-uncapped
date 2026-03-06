using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using Unity.Netcode;


public enum GameModes
{
    NONE,
    FFA,
    CTF,
}

// Owns the current GameMode.
// Begins and ends the game.
// Must be notified to begin or end it (signalled by admin).
// GameModes will handle beginning/ending their own game session
// through PhaseSystem, once it has begun.
public class GameModeHandler : MonoBehaviour
{
    private static GameModeHandler _instance;
    public static GameModeHandler Instance
    {
        get
        {
            return _instance;
        }
    }

    public GameModeBase current_game_mode;

    #region Gamemode prefab cache
    public static Dictionary<GameModes, GameObject> game_mode_cache = new();
    #endregion


    #region Public Methods
    public void StatEventReceiver(StatEvent s)
    {
        current_game_mode.StatEventReceiver(s);
    }
    
    // Game mode can be selected at any time. player voting, admin selection, in-game host selection should all call from here.
    // Psuedo
    public void SelectNewMode(GameModes g)
    {
        // Delete the current one
        if (current_game_mode)
        {
            // Remember, a GameModeBase is just a component of a GameObject
            // so delete the game object, this deletes GameModeBase
            Destroy(current_game_mode.gameObject);
        }

        // Fetch the new one and assign it
        // GameObject game_mode = game_mode_cache[g];
        // Clone the prefab, add it as a child to the GameModeHandler
        GameObject cloned_game_mode_object = Instantiate(game_mode_cache[g], this.gameObject.transform);
        cloned_game_mode_object.GetComponent<NetworkObject>().Spawn();
        // Fetch the clones's GameModeBase component
        GameModeBase game_mode = cloned_game_mode_object.GetComponent<GameModeBase>();
        current_game_mode = game_mode;

        if (LevelManager.Instance) LevelManager.Instance.OnStageGenerated();
        print("Done loading game mode");
    }

    public void StartGame()
    {
        current_game_mode.StartGame();
    }
    #endregion

    #region Message Receivers
    private void Awake()
    {
        // Init singleton logic
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }

        else
        {
            _instance = this;
        }

        // current_game_mode = gameObject.GetComponent<GameModeBase>();
        // // Debugging
        // SelectNewMode(GameModes.FFA);

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
    }
    #endregion
}
