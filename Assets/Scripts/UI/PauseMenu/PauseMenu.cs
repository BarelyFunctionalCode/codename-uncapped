using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[UxmlElement(libraryPath = "PauseMenu")]
public partial class PauseMenu : CustomUIElementBase
{
    private Button leaveButton;
    private Button quitButton;

    private Dictionary<string, VisualElement> tabContainers = new();
    private Dictionary<string, VisualElement> tabCategoryContainers = new();

    private bool isMenuActive = false;


    public void Initialize(Player player, Character character)
    {
        // Debug build or playing in editor
        bool devMode = Debug.isDebugBuild || Application.isEditor;
        bool isHost = NetworkManager.Singleton.IsHost;

        leaveButton = this.Q<Button>("LeaveButton");
        quitButton = this.Q<Button>("QuitButton");
        tabContainers["Gameplay"] = this.Query<VisualElement>("Gameplay").Children<VisualElement>("Content").First();;
        tabContainers["Video"] = this.Query<VisualElement>("Video").Children<VisualElement>("Content").First();;
        tabContainers["Audio"] = this.Query<VisualElement>("Audio").Children<VisualElement>("Content").First();;
        tabContainers["Controls"] = this.Query<VisualElement>("Controls").Children<VisualElement>("Content").First();;
        tabContainers["Debug"] = this.Query<VisualElement>("Debug").Children<VisualElement>("Content").First();;
        // if (!devMode) this.Q<Tab>("Debug").RemoveFromHierarchy();
        // else debugContentContainer = this.Query<VisualElement>("Debug").Children<VisualElement>("Content").First();

        if (!isHost) leaveButton.RegisterCallback<ClickEvent>(Leave);
        else leaveButton.style.display = DisplayStyle.None;
        quitButton.RegisterCallback<ClickEvent>(Quit);
        
        
        EnableInClassList("active-menu", false);


        PlayerControls playerControls = player.playerControls;

        // Initialize player options in pause menu
        FieldInfo[] fields = player.settings.GetType().GetFields();
        foreach (var field in fields)
        {
            PauseMenuOptionAttribute[] attribute = (PauseMenuOptionAttribute[])field.GetCustomAttributes(typeof(PauseMenuOptionAttribute), true);

            if (attribute.Length > 0)
            {
                if (!devMode && attribute[0].GetType() == typeof(PauseMenuDevOptionAttribute)) continue;
                if (field.FieldType == typeof(bool))
                {
                    AddBoolOption(
                        attribute[0].label,
                        attribute[0].tabName,
                        attribute[0].categoryName,
                        (bool)field.GetValue(player.settings),
                        value => { player.settings.UpdateSetting(field.Name, value); }
                    );
                }
                else if (field.Name.EndsWith("Index") && field.FieldType == typeof(int))
                {
                    List<string> options = fields.FirstOrDefault(f => f.Name == attribute[0].listOptionsVariableName)?.GetValue(player.settings) as List<string>;
                    AddListOption(
                        attribute[0].label,
                        attribute[0].tabName,
                        attribute[0].categoryName,
                        (int)field.GetValue(player.settings),
                        value => { player.settings.UpdateSetting(field.Name, value); },
                        options
                    );
                }
                else
                {
                    AddFloatOption(
                        attribute[0].label,
                        attribute[0].tabName,
                        attribute[0].categoryName,
                        (float)field.GetValue(player.settings),
                        value => { player.settings.UpdateSetting(field.Name, value); },
                        attribute[0].minValue,
                        attribute[0].maxValue
                    );
                }
            }
        }

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

        foreach (var container in tabContainers.Values)
        {
            if (container == null) continue;
            container.Clear();
        }
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
        PauseMenuControl newcontrol = (PauseMenuControl)UIManager.Spawn("UI/PauseMenu/PauseMenuControl", tabContainers["Controls"]);
        newcontrol.Initialize(action);
    }


    private VisualElement GetOptionContainer(string tabName, string categoryName)
    {
        if (!tabContainers.ContainsKey(tabName)) return null;
        VisualElement container = tabContainers[tabName];
        if (container == null) return null;

        if (!tabCategoryContainers.ContainsKey($"{tabName}_{categoryName}"))
        {
            PauseMenuOptionCategory newCategory = (PauseMenuOptionCategory)UIManager.Spawn("UI/PauseMenu/PauseMenuOptionCategory", container);
            newCategory.Initialize(categoryName);
            tabCategoryContainers[$"{tabName}_{categoryName}"] = newCategory;
        }
        VisualElement category = tabCategoryContainers[$"{tabName}_{categoryName}"];
        return category;
    }

    public void AddFloatOption(string name, string tabName, string categoryName, float value, Action<float> onValueChanged, float minValue = -1f, float maxValue = -1f)
    {
        VisualElement category = GetOptionContainer(tabName, categoryName);
        PauseMenuOptionSlider newOption = (PauseMenuOptionSlider)UIManager.Spawn("UI/PauseMenu/PauseMenuOptionSlider", category);
        newOption.Initialize(name, value, onValueChanged, minValue, maxValue);
    }

    public void AddBoolOption(string name, string tabName, string categoryName, bool value, Action<bool> onValueChanged)
    {
        VisualElement category = GetOptionContainer(tabName, categoryName);
        PauseMenuOptionToggle newOption = (PauseMenuOptionToggle)UIManager.Spawn("UI/PauseMenu/PauseMenuOptionToggle", category);
        newOption.Initialize(name, value, onValueChanged);
    }

    public void AddListOption(string name, string tabName, string categoryName, int value, Action<int> onValueChanged, List<string> options)
    {
        VisualElement category = GetOptionContainer(tabName, categoryName);
        ExpandableList newExpandableList = (ExpandableList)UIManager.Spawn("UI/ExpandableList/ExpandableList", category);
        newExpandableList.Initialize(name, (listName, itemValue) => { onValueChanged(int.Parse(itemValue)); }, true);

        for (int i = 0; i < options.Count; i++)
        {
            string option = options[i];
            newExpandableList.AddListItem(option, i.ToString(), true);
            Debug.Log($"Added list option: {option} with value {i}");
        }

        newExpandableList.SetSelectedItem(value.ToString());
    }

    public void AddDebugOption(string name, bool value, Action<bool> onValueChanged)
    {
        if (!tabContainers.ContainsKey("Debug")) return;
        PauseMenuDebug newOption = (PauseMenuDebug)UIManager.Spawn("UI/PauseMenu/PauseMenuDebug", tabContainers["Debug"]);
        newOption.Initialize(name, value, onValueChanged);
    }

    private void Leave(ClickEvent evt) => GameManager.Instance.PrepGoToOwnLobby();
    private void Quit(ClickEvent evt) => Application.Quit();
}