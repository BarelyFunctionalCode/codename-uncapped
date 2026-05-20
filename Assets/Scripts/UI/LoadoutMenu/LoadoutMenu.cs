using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement(libraryPath = "LoadoutMenu/LoadoutMenu")]
public partial class LoadoutMenu : CustomUIElementBase
{
    // Used to display a 3D preview of the currently selected item.
    static private GameObject showcasePrefabObj;
    private Showcase showcaseInstance;

    private VisualElement optionsListsContainer;
    private Label selectedItemNameLabel;
    private Label selectedItemDescriptionLabel;

    // Data used to populate the loadout option lists.
    private struct LoadoutListData
    {
        public LoadoutItemType itemType;
        public string listName;
        public Func<List<LoadoutItemSO>> items;
    }
    private readonly List<LoadoutListData> loadoutListsData = new()
    {
        new LoadoutListData { itemType = LoadoutItemType.Weapon, listName = "PRIMARY WEAPON", items = () => CharacterLoadout.WeaponLoadoutItems },
        new LoadoutListData { itemType = LoadoutItemType.Weapon, listName = "SECONDARY WEAPON", items = () => CharacterLoadout.WeaponLoadoutItems },
        new LoadoutListData { itemType = LoadoutItemType.Throwable, listName = "THROWABLE", items = () => CharacterLoadout.ThrowableLoadoutItems },
        new LoadoutListData { itemType = LoadoutItemType.Gear, listName = "GEAR", items = () => CharacterLoadout.GearLoadoutItems },
        new LoadoutListData { itemType = LoadoutItemType.Drive, listName = "DRIVE", items = () => CharacterLoadout.DriveLoadoutItems }
    };

    // References to the player using the loadout menu.
    private CharacterLoadoutManager playerLoadoutManager;
    private CharacterLoadout tempLoadout;
    private HUDController hud;

    private bool isInitialized = false;
    private bool isMenuActive = false;


    // Set references to the player's loadout and HUD
    public void Initialize(CharacterLoadoutManager playerLoadoutManager, HUDController hud)
    {
        if (isInitialized) return;

        if (showcasePrefabObj == null) showcasePrefabObj = Resources.Load<GameObject>("UI/LoadoutMenu/Showcase");

        optionsListsContainer = this.Q("options-lists");
        selectedItemNameLabel = this.Query("item-info").Children<Label>("item-name").First();
        selectedItemDescriptionLabel = this.Query("item-info").Children<Label>("item-description").First();

        this.playerLoadoutManager = playerLoadoutManager;
        this.hud = hud;

        // Initialize menu and showcase instance.
        BuildLoadoutLists();
        EnableInClassList("active-menu", isMenuActive);

        // Register callbacks for confirm and cancel buttons.
        this.Q<Button>("confirm-button").clicked += OnConfirmClicked;
        this.Q<Button>("cancel-button").clicked += OnCancelClicked;

        isInitialized = true;
    }

    // Clear references to player loadout and HUD
    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;

        if (showcaseInstance != null) UnityEngine.Object.Destroy(showcaseInstance.gameObject);
        showcaseInstance = null;

        playerLoadoutManager = null;
        hud = null;
    }

    // Toggle the visibility of the loadout menu
    public bool ToggleMenu()
    {
        isMenuActive = !isMenuActive;
        EnableInClassList("active-menu", isMenuActive);
        if (isMenuActive)
        {
            tempLoadout = new CharacterLoadout(playerLoadoutManager.currentLoadout.Value);
            if (showcaseInstance == null) showcaseInstance = UnityEngine.Object.Instantiate(showcasePrefabObj).GetComponent<Showcase>();
            else showcaseInstance.Clear();
            if (tempLoadout.weapon1SO != null) showcaseInstance.AddObject(
                tempLoadout.weapon1SO.showcaseItemPrefab != null ?
                tempLoadout.weapon1SO.showcaseItemPrefab :
                tempLoadout.weapon1SO.itemPrefab,
                tempLoadout.weapon1SO.showcaseAdditionalCameraDistance
            );
        }
        else
        {
            if (showcaseInstance != null) UnityEngine.Object.Destroy(showcaseInstance.gameObject);
            showcaseInstance = null;
        }
        return isMenuActive;
    }

    // Iterate through the lists of loadout items, spawning a list for each category, and populating each list with the category's respective items.
    private void BuildLoadoutLists()
    {
        for (int i = 0; i < loadoutListsData.Count; i++)
        {
            ExpandableList newExpandableList = (ExpandableList)UIManager.Spawn("UI/ExpandableList/ExpandableList", optionsListsContainer);
            newExpandableList.Initialize(loadoutListsData[i].listName, OnListItemSelected);

            foreach (LoadoutItemSO item in loadoutListsData[i].items())
            {
                newExpandableList.AddListItem(item.itemName, item.itemName, item.isAvailable);
            }
        }
    }

    // Update player's loadout based on the selected item, and update the showcase preview and item description accordingly.
    private void OnListItemSelected(string listName, string itemValue)
    {
        LoadoutItemType itemType = loadoutListsData.Find(data => data.listName == listName).itemType;
        LoadoutItemSO selectedItem = CharacterLoadout.LoadoutItemsByType[itemType].Find(item => item.itemName == itemValue);
        selectedItemNameLabel.text = selectedItem.itemName;
        selectedItemDescriptionLabel.text = selectedItem.itemDescription;

        if (showcaseInstance != null)
        {
            showcaseInstance.Clear();
            showcaseInstance.AddObject(
                selectedItem.showcaseItemPrefab != null ? selectedItem.showcaseItemPrefab : selectedItem.itemPrefab,
                selectedItem.showcaseAdditionalCameraDistance
            );
        }

        if (tempLoadout == null) return;
        switch (itemType)
        {
            case LoadoutItemType.Weapon:
                if (listName == "PRIMARY WEAPON") tempLoadout.weapon1SO = selectedItem;
                else if (listName == "SECONDARY WEAPON") tempLoadout.weapon2SO = selectedItem;
                break;
            case LoadoutItemType.HeavyWeapon when tempLoadout.armorClass == LoadoutArmorClass.Heavy:
                tempLoadout.heavyWeaponSO = selectedItem;
                break;
            case LoadoutItemType.Throwable:
                tempLoadout.throwableSO = selectedItem;
                break;
            case LoadoutItemType.Gear:
                tempLoadout.gearSO = selectedItem;
                break;
            case LoadoutItemType.Drive:
                tempLoadout.driveSO = selectedItem;
                break;
        }
        
    }

    // Apply the changes to the player's loadout and close the menu
    private void OnConfirmClicked()
    {
        bool applyImmediately = GameModeHandler.Instance.currentPhase.Value != Phase.ACTIVE;
        playerLoadoutManager.UpdateLoadoutRpc(tempLoadout, applyImmediately);
        hud.ToggleMenu(HUDMenu.LoadoutMenu);
    }

    // Close the menu without applying changes
    private void OnCancelClicked()
    {
        hud.ToggleMenu(HUDMenu.LoadoutMenu);
    }
}