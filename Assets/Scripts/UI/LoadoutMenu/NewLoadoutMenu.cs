using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class NewLoadoutMenu : MonoBehaviour
{
    // Used to display a 3D preview of the currently selected item.
    [SerializeField] private GameObject showcasePrefabObj;
    private Showcase showcaseInstance;

    // References to UI elements.
    private UIDocument loadoutUIDocument;
    private VisualElement OptionsListsContainer => loadoutUIDocument.rootVisualElement.Q("OptionsLists");
    private Label SelectedItemNameLabel =>loadoutUIDocument.rootVisualElement.Query("ItemInfo").Children<Label>("ItemName").First();
    private Label SelectedItemDescriptionLabel =>loadoutUIDocument.rootVisualElement.Query("ItemInfo").Children<Label>("ItemDescription").First();

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


    void Awake()
    {
        loadoutUIDocument = GetComponent<UIDocument>();
    }
    
    void Start()
    {
        // Initialize menu and showcase instance.
        BuildLoadoutLists();
        if (showcaseInstance == null) showcaseInstance = Instantiate(showcasePrefabObj).GetComponent<Showcase>();
        else showcaseInstance.Clear();
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
}
