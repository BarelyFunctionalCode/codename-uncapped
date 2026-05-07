using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoadoutMenu : MonoBehaviour
{
    // Used to display a 3D preview of the currently selected item.
    [SerializeField] private GameObject showcasePrefabObj;
    private Showcase showcaseInstance;

    // References to UI elements.
    private UIDocument loadoutUIDocument;
    private VisualElement OptionsListsContainer => loadoutUIDocument.rootVisualElement.Q("OptionsLists");
    private Label SelectedItemNameLabel =>loadoutUIDocument.rootVisualElement.Query("ItemInfo").Children<Label>("ItemName").First();
    private Label SelectedItemDescriptionLabel =>loadoutUIDocument.rootVisualElement.Query("ItemInfo").Children<Label>("ItemDescription").First();

    [SerializeField] private bool isOpen = false;

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
    private HUD hud;

    private bool isInitialized = false;


    void Awake()
    {
        loadoutUIDocument = GetComponent<UIDocument>();
    }
    
    // Set references to the player's loadout and HUD
    public void Initialize(CharacterLoadoutManager playerLoadoutManager, HUD hud)
    {
        if (isInitialized) return;

        this.playerLoadoutManager = playerLoadoutManager;
        this.hud = hud;

        // Initialize menu and showcase instance.
        BuildLoadoutLists();
        loadoutUIDocument.enabled = isOpen;
        if (showcaseInstance == null) showcaseInstance = Instantiate(showcasePrefabObj).GetComponent<Showcase>();
        else showcaseInstance.Clear();

        isInitialized = true;
    }

    // Clear references to player loadout and HUD
    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;

        if (showcaseInstance != null) Destroy(showcaseInstance.gameObject);
        showcaseInstance = null;

        playerLoadoutManager = null;
        hud = null;
    }

    // Toggle the visibility of the loadout menu
    public bool ToggleMenu()
    {
        loadoutUIDocument.enabled = !loadoutUIDocument.enabled;
        if (loadoutUIDocument.enabled)
        {
            tempLoadout = new CharacterLoadout(playerLoadoutManager.currentLoadout.Value);
            BuildLoadoutLists();
            if (showcaseInstance == null) showcaseInstance = Instantiate(showcasePrefabObj).GetComponent<Showcase>();
            else showcaseInstance.Clear();
            if (tempLoadout.weapon1SO != null) showcaseInstance.AddObject(
                tempLoadout.weapon1SO.showcaseItemPrefab != null ?
                tempLoadout.weapon1SO.showcaseItemPrefab :
                tempLoadout.weapon1SO.itemPrefab,
                tempLoadout.weapon1SO.showcaseAdditionalCameraDistance
            );

            // Register callbacks for confirm and cancel buttons.
            var confirmButton = loadoutUIDocument.rootVisualElement.Q<Button>("ConfirmButton");
            if (confirmButton != null) confirmButton.clicked += OnConfirmClicked;
            var cancelButton = loadoutUIDocument.rootVisualElement.Q<Button>("CancelButton");
            if (cancelButton != null) cancelButton.clicked += OnCancelClicked;
        }
        else
        {
            if (showcaseInstance != null) Destroy(showcaseInstance.gameObject);
            showcaseInstance = null;

            if (loadoutUIDocument.rootVisualElement != null)
            {
                var confirmButton = loadoutUIDocument.rootVisualElement.Q<Button>("ConfirmButton");
                if (confirmButton != null) confirmButton.clicked -= OnConfirmClicked;
                var cancelButton = loadoutUIDocument.rootVisualElement.Q<Button>("CancelButton");
                if (cancelButton != null) cancelButton.clicked -= OnCancelClicked;
            }
        }
        return loadoutUIDocument.enabled;
    }

    // Iterate through the lists of loadout items, spawning a list for each category, and populating each list with the category's respective items.
    private void BuildLoadoutLists()
    {
        for (int i = 0; i < loadoutListsData.Count; i++)
        {
            ExpandableList newExpandableList = (ExpandableList)UIManager.Spawn("UI/ExpandableList/ExpandableList", OptionsListsContainer);
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
        SelectedItemNameLabel.text = selectedItem.itemName;
        SelectedItemDescriptionLabel.text = selectedItem.itemDescription;

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
