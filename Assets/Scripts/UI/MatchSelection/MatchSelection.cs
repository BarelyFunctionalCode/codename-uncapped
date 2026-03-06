using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MatchSelection : NetworkBehaviour
{
    private Canvas canvas;
    [SerializeField] private CinemachineCamera pcCam;
    [SerializeField] private LayerMask noPlayerMask;

    [SerializeField] private TMP_Dropdown levelSelectDropdown;
    [SerializeField] private TMP_Dropdown gameModeSelectDropdown;
    [SerializeField] private TMP_Dropdown maxPlayersSelectDropdown;
    [SerializeField] private TMP_Dropdown timeLimitSelectDropdown;

    [SerializeField] private TMP_Text selectedLevelTitleText;
    [SerializeField] private TMP_Text selectedLevelDescriptionText;

    [SerializeField] private TMP_Text selectedGameModeTitleText;
    [SerializeField] private TMP_Text selectedGameModeDescriptionText;

    [SerializeField] private GameObject interactPromptObj;

    [SerializeField] private GameObject cursorObj;
    private Rect cursorBounds;

    private static List<string> levelNames = new();

    private float camToCanvasDistance;
    private bool showInteractPrompt = false;
    private bool isActive = false;

    private PlayerController interactingPlayerController;

    private string selectedLevel;
    private GameModes selectedGameMode;
    private int selectedMaxPlayers;
    private int selectedTimeLimit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        canvas = GetComponentInChildren<Canvas>();
        camToCanvasDistance = pcCam.GetComponent<CinemachinePositionComposer>().CameraDistance;
        cursorBounds = cursorObj.transform.parent.GetComponent<RectTransform>().rect;
    }

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        InitializeMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if (cursorObj != null && isActive)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, camToCanvasDistance));
            // Clamp cursor position to canvas bounds
            cursorPosition.x = Mathf.Clamp(cursorPosition.x, cursorBounds.xMin * canvas.scaleFactor, cursorBounds.xMax * canvas.scaleFactor);
            cursorPosition.y = Mathf.Clamp(cursorPosition.y, cursorBounds.yMin * canvas.scaleFactor, cursorBounds.yMax * canvas.scaleFactor);
            cursorObj.transform.position = cursorPosition; // TODO: This doesn't work, probably need to update anchored position
        }

        if (Keyboard.current.fKey.wasPressedThisFrame && showInteractPrompt && !isActive)
        {
            Interact();
        }
    }

    private void FixedUpdate()
    {
        interactPromptObj.SetActive(showInteractPrompt && !isActive);
        showInteractPrompt = false;
    }

    private void Interact()
    {
        NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().TryGetComponent(out PlayerController playerController);
        playerController.SetPlayerControlsRpc(false);
        interactingPlayerController = playerController;
        interactingPlayerController.playerHUD.ToggleHUD();

        // Sets priority to PC Cam and then unlocks the cursor
        Camera.main.cullingMask = noPlayerMask;
        pcCam.Priority.Value = 99;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        cursorObj.SetActive(true);
        isActive = true;
    }

    private void Reset()
    {
        // Resets priority to PC Cam and then locks the cursor
        isActive = false;
        cursorObj.SetActive(false);
        Camera.main.cullingMask = -1;
        pcCam.Priority.Value = 0;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        interactingPlayerController.SetPlayerControlsRpc(true);
        interactingPlayerController.playerHUD.ToggleHUD();
        interactingPlayerController = null;
    }

    private void InitializeMenu()
    {
        if (levelNames.Count == 0) // TODO: Change this out for LevelSO that will have nice title and description.
        {
            string fullPath = Path.Combine(Application.dataPath, "Scenes/Levels");
            string[] sceneFiles = Directory.GetFiles(fullPath, "*.unity");
            foreach (string file in sceneFiles)
            {
                string fileName = Path.GetFileName(file);
                Debug.Log("Scene file: " + fileName);
                levelNames.Add(Path.GetFileNameWithoutExtension(fileName));
            }
        }
        selectedLevel = levelNames[0];
        selectedLevelTitleText.text = selectedLevel; // TODO: Change this to be the title from the LevelSO
        selectedLevelDescriptionText.text = "This is a description for " + selectedLevel; // TODO: Change this to be the description from the LevelSO
        levelSelectDropdown.AddOptions(levelNames);

        List<string> gameModeNames = new();
        bool isFirst = true;
        foreach (GameModes gameMode in System.Enum.GetValues(typeof(GameModes)))
        {
            if (isFirst) 
            {
                isFirst = false;
                continue; // Skip None
            }
            
            gameModeNames.Add(gameMode.ToString());
        }
        selectedGameMode = (GameModes)1;
        selectedGameModeTitleText.text = selectedGameMode.ToString(); // TODO: Change this to be the title from the GameModeSO
        selectedGameModeDescriptionText.text = "This is a description for " + selectedGameMode; // TODO: Change this to be the description from the GameModeSO
        gameModeSelectDropdown.AddOptions(gameModeNames);

        List<string> maxPlayerOptions = new() { "2", "4", "8", "16" }; // TODO: Change this to be dynamic based on the selected level and gamemode
        selectedMaxPlayers = int.Parse(maxPlayerOptions[0]);
        maxPlayersSelectDropdown.AddOptions(maxPlayerOptions);

        List<string> timeLimitOptions = new() { "5", "10", "15", "20", "30", "60" }; // TODO: Change this to be dynamic based on the selected level and gamemode
        selectedTimeLimit = int.Parse(timeLimitOptions[0]);
        timeLimitSelectDropdown.AddOptions(timeLimitOptions);

        // TODO: For Objective based gamemodes, add dropdown for objective selection that is populated based on the selected level.

        if (!IsHost)
        {
            levelSelectDropdown.interactable = false;
            gameModeSelectDropdown.interactable = false;
            maxPlayersSelectDropdown.interactable = false;
            timeLimitSelectDropdown.interactable = false;
        }
    }

    public void OnLevelSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnLevelSelectValueChangedRpc(dropdown.value);
    }
    [Rpc(SendTo.Everyone)]
    private void OnLevelSelectValueChangedRpc(int index)
    {
        selectedLevel = levelNames[index];
        selectedLevelTitleText.text = selectedLevel; // TODO: Change this to be the title from the LevelSO
        selectedLevelDescriptionText.text = "This is a description for " + selectedLevel; // TODO: Change this to be the description from the LevelSO

        if (IsHost) return;
        levelSelectDropdown.value = index;
    }

    public void OnGameModeSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnGameModeSelectValueChangedRpc(dropdown.value + 1); // Account for None value in enum
    }
    [Rpc(SendTo.Everyone)]
    private void OnGameModeSelectValueChangedRpc(int index)
    {
        selectedGameMode = (GameModes)index;
        selectedGameModeTitleText.text = selectedGameMode.ToString(); // TODO: Change this to be the title from the GameModeSO
        selectedGameModeDescriptionText.text = "This is a description for " + selectedGameMode; // TODO: Change this to be the description from the GameModeSO

        if (IsHost) return;
        gameModeSelectDropdown.value = index;
    }

    public void OnMaxPlayersSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnMaxPlayersSelectValueChangedRpc(dropdown.value);
    }
    [Rpc(SendTo.Everyone)]
    private void OnMaxPlayersSelectValueChangedRpc(int index)
    {
        selectedMaxPlayers = int.Parse(maxPlayersSelectDropdown.options[index].text);

        if (IsHost) return;
        maxPlayersSelectDropdown.value = index;
    }

    public void OnTimeLimitSelectValueChanged(TMP_Dropdown dropdown)
    {
        if (!IsHost) return;
        OnTimeLimitSelectValueChangedRpc(dropdown.value);
    }
    [Rpc(SendTo.Everyone)]
    private void OnTimeLimitSelectValueChangedRpc(int index)
    {
        selectedTimeLimit = int.Parse(timeLimitSelectDropdown.options[index].text);

        if (IsHost) return;
        timeLimitSelectDropdown.value = index;
    }

    public void OnStartMatchButtonPressed()
    {
        if (!IsHost) return;

        Reset();
        GameManager.Instance.SetLevel(selectedLevel);
        GameManager.Instance.SetGameMode(selectedGameMode);
        GameManager.Instance.LoadLevel();
    }

    void OnTriggerStay(Collider other)
    {
        if (isActive) return;

        other.TryGetComponent(out PlayerController playerController);
        if (playerController != null && playerController.IsLocalPlayer)
        {
            interactPromptObj.SetActive(true);
            showInteractPrompt = true;
        }
    }
}
