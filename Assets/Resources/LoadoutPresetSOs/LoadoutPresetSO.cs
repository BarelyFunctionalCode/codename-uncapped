using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Loadout Preset", menuName = "Loadout/Loadout Preset")]
public class LoadoutPresetSO : ScriptableObject
{
    public string loadoutName;
    public string loadoutDescription;

    public LoadoutArmorClass armorClass;
    public LoadoutItemSO weapon1;
    public LoadoutItemSO weapon2;
    public LoadoutItemSO heavyWeapon;
    public LoadoutItemSO throwable;
    public LoadoutItemSO equipment;
    public LoadoutItemSO core;
}
