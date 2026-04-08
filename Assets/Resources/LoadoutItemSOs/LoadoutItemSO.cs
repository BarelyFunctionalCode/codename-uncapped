using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Loadout Item", menuName = "Loadout/Loadout Item")]
public class LoadoutItemSO : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public LoadoutItemType itemType;
    public GameObject itemPrefab;
    public GameObject showcaseItemPrefab = null;
    public float showcaseAdditionalCameraDistance = 0f;


    public List<LoadoutArmorClass> applicableArmorClasses = new() { LoadoutArmorClass.Any };
    public bool isAvailable = true;
}
