using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[UxmlElement(libraryPath = "PauseMenu/PauseMenu")]
public partial class PauseMenu : CustomUIElementBase
{
    private Button leaveButton;
    private Button quitButton;

    private VisualElement controlsContentContainer;
    private VisualElement debugContentContainer;

    private bool isMenuActive = false;


    public void Initialize(Player player, Character character)
    {
        // Debug build or playing in editor
        bool devMode = Debug.isDebugBuild || Application.isEditor;
        bool isHost = NetworkManager.Singleton.IsHost;

        leaveButton = this.Q<Button>("LeaveButton");
        quitButton = this.Q<Button>("QuitButton");
        controlsContentContainer = this.Query<VisualElement>("Controls").Children<VisualElement>("Content").First();
        debugContentContainer = this.Query<VisualElement>("Debug").Children<VisualElement>("Content").First();
        // if (!devMode) this.Q<Tab>("Debug").RemoveFromHierarchy();
        // else debugContentContainer = this.Query<VisualElement>("Debug").Children<VisualElement>("Content").First();

        if (!isHost) leaveButton.RegisterCallback<ClickEvent>(Leave);
        else leaveButton.style.display = DisplayStyle.None;
        quitButton.RegisterCallback<ClickEvent>(Quit);
        
        
        EnableInClassList("active-menu", false);


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
        foreach (var field in fields)
        {
            PauseMenuDevOptionAttribute[] attribute = (PauseMenuDevOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuDevOptionAttribute), true);

            if (attribute.Length > 0)
            {
                AddDebugOption(
                    attribute[0].label,
                    (bool)field.GetValue(character.characterTelemetry),
                    value => { field.SetValue(character.characterTelemetry, value); }
                );
            }
        }
    }

    public void Deinitialize()
    {
        leaveButton.UnregisterCallback<ClickEvent>(Leave);
        quitButton.UnregisterCallback<ClickEvent>(Quit);

        controlsContentContainer.Clear();

        if (debugContentContainer == null) return;
        debugContentContainer.Clear();
    }

    public bool ToggleMenu()
    {
        isMenuActive = !isMenuActive;
        EnableInClassList("active-menu", isMenuActive);
        pickingMode = isMenuActive ? PickingMode.Position : PickingMode.Ignore;
        if (isMenuActive) BringToFront();
        return isMenuActive;
    }

    public void AddControl(InputAction action)
    {
        PauseMenuControl newcontrol = (PauseMenuControl)UIManager.Spawn("UI/PauseMenu/PauseMenuControl", controlsContentContainer);
        newcontrol.Initialize(action);
    }

    public void AddDebugOption(string name, bool value, Action<bool> onValueChanged)
    {
        if (debugContentContainer == null) return;
        PauseMenuDebug newOption = (PauseMenuDebug)UIManager.Spawn("UI/PauseMenu/PauseMenuDebug", debugContentContainer);
        newOption.Initialize(name, value, onValueChanged);
    }

    private void Leave(ClickEvent evt) => GameManager.Instance.PrepGoToOwnLobby();
    private void Quit(ClickEvent evt) => Application.Quit();
}