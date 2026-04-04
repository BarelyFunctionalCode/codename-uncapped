using TMPro;
using UnityEngine;

public class LoadoutMenu : MonoBehaviour
{
    [SerializeField] private GameObject showcasePrefabObj;
    private Showcase showcaseInstance;
    [SerializeField] private ExpandableList armorClassesList;
    [SerializeField] private ExpandableList weapons1List;
    [SerializeField] private ExpandableList weapons2List;
    [SerializeField] private ExpandableList heavyWeaponsList;
    [SerializeField] private ExpandableList throwablesList;
    [SerializeField] private ExpandableList equipmentsList;
    [SerializeField] private ExpandableList coresList;

    [SerializeField] private TMP_Text itemDetailsPanelTitle;
    [SerializeField] private TMP_Text itemDetailsPanelDescription;

    private PlayerLoadoutManager playerLoadoutManager;
    private PlayerLoadout tempLoadout;
    private LoadoutArmorClass selectedArmorClass = LoadoutArmorClass.Any;

    private HUD hud;

    private bool isInitialized = false;

    private void Awake()
    {
        gameObject.SetActive(false);
        BuildLoadoutLists();
        armorClassesList.ToggleList();
    }

    public void Initialize(PlayerLoadoutManager playerLoadoutManager, HUD hud)
    {
        if (isInitialized) return;

        this.playerLoadoutManager = playerLoadoutManager;
        this.hud = hud;
        isInitialized = true;
    }

    public bool ToggleMenu()
    {
        gameObject.SetActive(!gameObject.activeSelf);
        if (gameObject.activeSelf)
        {
            tempLoadout = new PlayerLoadout(playerLoadoutManager.currentLoadout.Value);
            selectedArmorClass = tempLoadout.armorClass;
            BuildLoadoutLists();
            if (showcaseInstance == null) showcaseInstance = Instantiate(showcasePrefabObj).GetComponent<Showcase>();
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
            if (showcaseInstance != null) Destroy(showcaseInstance.gameObject);
            showcaseInstance = null;
        }
        return gameObject.activeSelf;
    }

    private void BuildLoadoutLists()
    {
        if (armorClassesList.itemCount == 0)
        {
            foreach (LoadoutItemSO item in PlayerLoadout.ArmorClassLoadoutItems)
            {
                armorClassesList.AddListItem(item.itemName, item.isAvailable, item).AddListener((itemSO) => OnLoadoutItemSelected(itemSO, "ArmorClasses"));
            }
        }

        weapons1List.ClearList();
        weapons2List.ClearList();
        heavyWeaponsList.ClearList();
        throwablesList.ClearList();
        equipmentsList.ClearList();
        coresList.ClearList();

        foreach (LoadoutItemSO item in PlayerLoadout.WeaponLoadoutItems)
        {
            if (item.applicableArmorClasses.Contains(selectedArmorClass) || item.applicableArmorClasses.Contains(LoadoutArmorClass.Any))
            {
                weapons1List.AddListItem(item.itemName, item.isAvailable, item).AddListener((itemSO) => OnLoadoutItemSelected(itemSO, "Weapons1"));
                weapons2List.AddListItem(item.itemName, item.isAvailable, item).AddListener((itemSO) => OnLoadoutItemSelected(itemSO, "Weapons2"));
            }
        }

        heavyWeaponsList.gameObject.SetActive(selectedArmorClass == LoadoutArmorClass.Heavy);
        if (selectedArmorClass == LoadoutArmorClass.Heavy)
        {
            foreach (LoadoutItemSO item in PlayerLoadout.HeavyWeaponLoadoutItems)
            {
                heavyWeaponsList.AddListItem(item.itemName, item.isAvailable, item).AddListener((itemSO) => OnLoadoutItemSelected(itemSO, "HeavyWeapons"));
            }
        }

        foreach (LoadoutItemSO item in PlayerLoadout.ThrowableLoadoutItems)
        {
            if (item.applicableArmorClasses.Contains(selectedArmorClass) || item.applicableArmorClasses.Contains(LoadoutArmorClass.Any))
            {
                throwablesList.AddListItem(item.itemName, item.isAvailable, item).AddListener((itemSO) => OnLoadoutItemSelected(itemSO, "Throwables"));
            }
        }

        foreach (LoadoutItemSO item in PlayerLoadout.EquipmentLoadoutItems)
        {
            if (item.applicableArmorClasses.Contains(selectedArmorClass) || item.applicableArmorClasses.Contains(LoadoutArmorClass.Any))
            {
                equipmentsList.AddListItem(item.itemName, item.isAvailable, item).AddListener((itemSO) => OnLoadoutItemSelected(itemSO, "Equipment"));
            }
        }

        foreach (LoadoutItemSO item in PlayerLoadout.CoreLoadoutItems)
        {
            if (item.applicableArmorClasses.Contains(selectedArmorClass) || item.applicableArmorClasses.Contains(LoadoutArmorClass.Any))
            {
                coresList.AddListItem(item.itemName, item.isAvailable, item).AddListener((itemSO) => OnLoadoutItemSelected(itemSO, "Cores"));
            }
        }
    }   

    private void OnLoadoutItemSelected(ScriptableObject itemSO, string listName)
    {
        LoadoutItemSO loadoutItem = itemSO as LoadoutItemSO;
        itemDetailsPanelTitle.text = loadoutItem.itemName;
        itemDetailsPanelDescription.text = loadoutItem.itemDescription;

        if (loadoutItem.itemType == LoadoutItemType.ArmorClass)
        {
            selectedArmorClass = (LoadoutArmorClass)System.Enum.Parse(typeof(LoadoutArmorClass), loadoutItem.itemName);
            tempLoadout.armorClass = selectedArmorClass;
            BuildLoadoutLists();
        }
        else if (loadoutItem.itemType == LoadoutItemType.Weapon)
        {
            if (listName == "Weapons1") tempLoadout.weapon1SO = loadoutItem;
            else if (listName == "Weapons2") tempLoadout.weapon2SO = loadoutItem;
        }
        else if (loadoutItem.itemType == LoadoutItemType.HeavyWeapon && tempLoadout.armorClass == LoadoutArmorClass.Heavy)
        {
            tempLoadout.heavyWeaponSO = loadoutItem;
        }
        else if (loadoutItem.itemType == LoadoutItemType.Throwable)
        {
            tempLoadout.throwableSO = loadoutItem;
        }
        else if (loadoutItem.itemType == LoadoutItemType.Equipment)
        {
            tempLoadout.equipmentSO = loadoutItem;
        }
        else if (loadoutItem.itemType == LoadoutItemType.Core)
        {
            tempLoadout.coreSO = loadoutItem;
        }

        if (showcaseInstance != null)
        {
            showcaseInstance.Clear();
            showcaseInstance.AddObject(
                loadoutItem.showcaseItemPrefab != null ? loadoutItem.showcaseItemPrefab : loadoutItem.itemPrefab,
                loadoutItem.showcaseAdditionalCameraDistance
            );
        }
    }

    public void OnConfirmClicked()
    {
        bool doKill = GameModeHandler.Instance.currentPhase.Value != Phase.ACTIVE;
        playerLoadoutManager.UpdateLoadoutRpc(tempLoadout, doKill);
        hud.ToggleMenu(HUDMenu.LoadoutMenu);
    }

    public void OnCancelClicked()
    {
        hud.ToggleMenu(HUDMenu.LoadoutMenu);
    }
}
