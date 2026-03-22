using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Reflection;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    public bool devMode { get; private set; } = true;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button leaveButton;
    // [SerializeField] private Button testLevelButton;

    [SerializeField] private Button optionsTabButton;
    [SerializeField] private Transform optionsListObj;
    [SerializeField] private GameObject optionsContainerObj;
    [SerializeField] private GameObject optionPrefabObj;
    private List<PauseMenuOption> optionsList = new List<PauseMenuOption>();

    [SerializeField] private Button controlsTabButton;
    [SerializeField] private Transform controlsListObj;
    [SerializeField] private GameObject controlsContainerObj;
    [SerializeField] private GameObject controlPrefabObj;
    private List<PauseMenuControl> controlsList = new List<PauseMenuControl>();

    [SerializeField] private Button debugTabButton;
    [SerializeField] private Transform debugListObj;
    [SerializeField] private GameObject debugContainerObj;
    [SerializeField] private GameObject debugPrefabObj;
    private List<PauseMenuDebug> debugList = new List<PauseMenuDebug>();


    private void Awake() 
    { 
        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
        } 
        else 
        { 
            Instance = this; 
        } 

        quitButton.onClick.AddListener( delegate { OnQuitButtonClicked(); } );
        leaveButton.onClick.AddListener( delegate { OnLeaveButtonClicked(); } );
        // testLevelButton.onClick.AddListener( delegate { OnTestLevelButtonClicked(); } );
        optionsTabButton.onClick.AddListener( delegate { OnOptionsTabButtonClicked(); } );
        controlsTabButton.onClick.AddListener( delegate { OnControlsTabButtonClicked(); } );
        debugTabButton.onClick.AddListener( delegate { OnDebugTabButtonClicked(); } );
        optionsContainerObj.SetActive(false);
        controlsContainerObj.SetActive(false);
        debugContainerObj.SetActive(false);
        gameObject.SetActive(false);

        debugTabButton.gameObject.SetActive(devMode);
        leaveButton.gameObject.SetActive(!NetworkManager.Singleton.IsHost);
        // testLevelButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
    }

    public void Initialize(PlayerController playerController)
    {
        PlayerControls playerControls = playerController.playerControls;

        // Initialize player options in pause menu
        FieldInfo[] fields = playerController.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuOptionAttribute[] attribute = (PauseMenuOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuOptionAttribute), true);

            if (attribute.Length > 0)
            {
                if (!devMode && attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute)) continue;
                AddOption(
                    attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute) ? "dev - " + attribute[0].label : attribute[0].label,
                    (float)field.GetValue(playerController),
                    attribute[0].minValue,
                    attribute[0].maxValue,
                    (float value) => { field.SetValue(playerController, value); }
                );
            }
        }

        // Initialize player controls in pause menu
        List<string> controlIgnoreList = new() { "Pause","Move", "Look" };
        // InputActionMap movementMap = playerControls.Movement;
        foreach (var actionMap in playerControls.asset.actionMaps)
        {
            foreach (var action in actionMap)
            {
                if (controlIgnoreList.Contains(action.name)) continue;
                AddControl(action);
            }
        }

        // Initialize player debug settings in pause menu
        if (!devMode) return;
        fields = playerController.playerTelemetry.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuDevOptionAttribute[] attribute = (PauseMenuDevOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuDevOptionAttribute), true);

            if (attribute.Length > 0)
            {
                AddDebug(
                    field.Name,
                    attribute[0].label,
                    (bool)field.GetValue(playerController.playerTelemetry),
                    value => { field.SetValue(playerController.playerTelemetry, value); }
                );
            }
        }
    }

    public bool ToggleMenu()
    {
        gameObject.SetActive(!gameObject.activeSelf);
        return gameObject.activeSelf;
    }

    private void OnQuitButtonClicked() { Application.Quit(); }

    private void OnLeaveButtonClicked() { if (!NetworkManager.Singleton.IsHost) GameManager.Instance.PrepGoToOwnLobby(); }

    // private void OnTestLevelButtonClicked()
    // {
    //     testLevelButton.gameObject.SetActive(false);
    //     if (NetworkManager.Singleton.IsHost)
    //     {
    //         GameManager.Instance.SetLevel("MultiplayerTestLevel");
    //         GameManager.Instance.SetGameMode(GameModes.FFA);
    //         GameManager.Instance.LoadLevel();
    //     }
    // }

    private void OnOptionsTabButtonClicked()
    {
        optionsContainerObj.SetActive(true);
        controlsContainerObj.SetActive(false);
        debugContainerObj.SetActive(false);
    }

    private void OnControlsTabButtonClicked()
    {
        optionsContainerObj.SetActive(false);
        controlsContainerObj.SetActive(true);
        debugContainerObj.SetActive(false);
    }

    private void OnDebugTabButtonClicked()
    {
        optionsContainerObj.SetActive(false);
        controlsContainerObj.SetActive(false);
        debugContainerObj.SetActive(true);
    }

    public void AddOption(string label, float value, float minValue, float maxValue, Action<float> updater)
    {
        GameObject optionObj = Instantiate(optionPrefabObj, optionsListObj);
        optionObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -optionsList.Count * optionObj.GetComponent<RectTransform>().rect.height);
        PauseMenuOption option = optionObj.GetComponent<PauseMenuOption>();
        option.Initialize(label, value, minValue, maxValue, updater);
        optionsList.Add(option);
    }

    public void AddControl(InputAction action)
    {
        GameObject controlObj = Instantiate(controlPrefabObj, controlsListObj);
        controlObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -controlsList.Count * controlObj.GetComponent<RectTransform>().rect.height);
        PauseMenuControl control = controlObj.GetComponent<PauseMenuControl>();
        control.Initialize(action);
        controlsList.Add(control);
    }

    public void AddDebug(string name, string label, bool value, Action<bool> updater)
    {
        GameObject debugObj = Instantiate(debugPrefabObj, debugListObj);
        debugObj.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -debugList.Count * debugObj.GetComponent<RectTransform>().rect.height);
        PauseMenuDebug debug = debugObj.GetComponent<PauseMenuDebug>();
        debug.Initialize(name, label, value, updater);
        debugList.Add(debug);
    }
}
