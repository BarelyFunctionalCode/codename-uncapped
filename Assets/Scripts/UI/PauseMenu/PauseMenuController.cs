using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PauseMenu : CustomUIElementBase
{
    private Button leaveButton;
    private Button quitButton;

    private VisualElement controlsContentContainer;
    private VisualElement debugContentContainer;


    public void Initialize(bool devMode, bool isHost, EventCallback<ClickEvent> onLeaveClicked, EventCallback<ClickEvent> onQuitClicked)
    {
        leaveButton = this.Q<Button>("LeaveButton");
        quitButton = this.Q<Button>("QuitButton");
        controlsContentContainer = this.Query<VisualElement>("Controls").Children<VisualElement>("Content").First();

        Debug.Log("Dev mode: " + devMode);
        debugContentContainer = this.Query<VisualElement>("Debug").Children<VisualElement>("Content").First();
        // if (!devMode) this.Q<Tab>("Debug").RemoveFromHierarchy();
        // else debugContentContainer = this.Query<VisualElement>("Debug").Children<VisualElement>("Content").First();

        if (!isHost) leaveButton.RegisterCallback(onLeaveClicked);
        else leaveButton.style.display = DisplayStyle.None;
        quitButton.RegisterCallback(onQuitClicked);
    }

    public void Deinitialize(EventCallback<ClickEvent> onLeaveClicked, EventCallback<ClickEvent> onQuitClicked)
    {
        leaveButton.UnregisterCallback(onLeaveClicked);
        quitButton.UnregisterCallback(onQuitClicked);

        controlsContentContainer.Clear();

        if (debugContentContainer == null) return;
        debugContentContainer.Clear();
    }

    public void AddControl(InputAction action)
    {
        PauseMenuControl newcontrol = (PauseMenuControl)UIManager.Spawn("UI/PauseMenu/PauseMenuControl", controlsContentContainer);
        newcontrol.Initialize(action);
    }

    public void AddDebugOption(string name, bool value, Action<bool> onValueChanged)
    {
        Debug.Log("Adding debug option to pause menu: " + name + " with value: " + value);
        if (debugContentContainer == null) return;
        Debug.Log("Spawning debug option UI element for: " + name);
        PauseMenuDebug newOption = (PauseMenuDebug)UIManager.Spawn("UI/PauseMenu/PauseMenuDebug", debugContentContainer);
        newOption.Initialize(name, value, onValueChanged);
    }
}

[UxmlElement]
public partial class PauseMenuControl : CustomUIElementBase
{
    private Label nameLabel;
    private Label valueLabel;
    private Button remapButton;


    public void Initialize(InputAction action)
    {
        nameLabel = this.Q<Label>("ControlName");
        valueLabel = this.Q<Label>("ControlValue");
        remapButton = this.Q<Button>("RemapButton");

        nameLabel.text = action.name;
        valueLabel.text = action.GetBindingDisplayString();

        remapButton.clicked += () => OnRemapButtonClicked(action);
    }

    private void OnRemapButtonClicked(InputAction action)
    {
        valueLabel.text = "Press a key...";

        action.PerformInteractiveRebinding()
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                operation.Dispose();
                valueLabel.text = action.GetBindingDisplayString();
            })
            .Start();
    }
}

[UxmlElement]
public partial class PauseMenuDebug : CustomUIElementBase
{
    private Toggle toggle;


    public void Initialize(string name, bool value, Action<bool> onValueChanged)
    {
        Debug.Log("Initializing debug option: " + name + " with value: " + value);
        toggle = this.Q<Toggle>();

        toggle.value = value;
        toggle.label = name;

        toggle.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
    }
}





public class PauseMenuController : MonoBehaviour
{
    UIDocument pauseMenuUIDocument;
    PauseMenu pauseMenu;


    public void Initialize(Player player, Character character)
    {
        // Debug build or playing in editor
        bool devMode = Debug.isDebugBuild || Application.isEditor;
        bool isHost = NetworkManager.Singleton.IsHost;

        pauseMenuUIDocument = GetComponent<UIDocument>();
        pauseMenu = pauseMenuUIDocument.rootVisualElement.Q<PauseMenu>();
        pauseMenu.Initialize(devMode, isHost, Leave, Quit);
        pauseMenu.style.display = DisplayStyle.None;


        PlayerControls playerControls = player.playerControls;

        // Initialize player options in pause menu
        FieldInfo[] fields = character.GetType().GetFields();
        // foreach (var field in fields)
        // {
        //     PauseMenuOptionAttribute[] attribute = (PauseMenuOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuOptionAttribute), true);

        //     if (attribute.Length > 0)
        //     {
        //         if (!devMode && attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute)) continue;
        //         AddOption(
        //             attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute) ? "dev - " + attribute[0].label : attribute[0].label,
        //             (float)field.GetValue(character),
        //             attribute[0].minValue,
        //             attribute[0].maxValue,
        //             (float value) => { field.SetValue(character, value); }
        //         );
        //     }
        // }

        // Initialize player controls in pause menu
        List<string> controlIgnoreList = new() { "Pause", "Move", "Look" };
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
        fields = character.characterTelemetry.GetType().GetFields();
        Debug.Log("Adding debug options for character telemetry. Total fields: " + fields.Length);
        foreach (var field in fields)
        {
            PauseMenuDevOptionAttribute[] attribute = (PauseMenuDevOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuDevOptionAttribute), true);

            if (attribute.Length > 0)
            {
                Debug.Log("Adding debug option: " + field.Name);
                AddDebug(
                    field.Name,
                    attribute[0].label,
                    (bool)field.GetValue(character.characterTelemetry),
                    value => { field.SetValue(character.characterTelemetry, value); }
                );
            }
        }
    }

    public void Deinitialize()
    {
        pauseMenu.Deinitialize(Leave, Quit);
    }

    public bool ToggleMenu()
    {
        bool currentState = pauseMenu.style.display == DisplayStyle.Flex;
        pauseMenu.style.display = currentState ? DisplayStyle.None : DisplayStyle.Flex;
        return pauseMenu.style.display == DisplayStyle.Flex;
    }

    private void Leave(ClickEvent evt) => GameManager.Instance.PrepGoToOwnLobby();
    private void Quit(ClickEvent evt) => Application.Quit();
    private void AddControl(InputAction action) => pauseMenu.AddControl(action);
    private void AddDebug(string name, string label, bool value, Action<bool> onValueChanged) => pauseMenu.AddDebugOption(label, value, onValueChanged);
}
