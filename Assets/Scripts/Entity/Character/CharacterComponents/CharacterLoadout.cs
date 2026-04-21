
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum LoadoutItemType
{
    ArmorClass,
    Weapon,
    HeavyWeapon,
    Throwable,
    Equipment,
    Drive
}

public enum LoadoutArmorClass
{
    Any,
    Light,
    Medium,
    Heavy
}

public class CharacterLoadout : INetworkSerializable, IEquatable<CharacterLoadout>
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref armorClass);
        if (serializer.IsWriter)
        {
            FastBufferWriter writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(weapon1SO != null ? WeaponLoadoutItems.IndexOf(WeaponLoadoutItems.First(w => w == weapon1SO)) : -1);
            writer.WriteValueSafe(weapon2SO != null ? WeaponLoadoutItems.IndexOf(WeaponLoadoutItems.First(w => w == weapon2SO)) : -1);
            writer.WriteValueSafe(heavyWeaponSO != null ? HeavyWeaponLoadoutItems.IndexOf(HeavyWeaponLoadoutItems.First(w => w == heavyWeaponSO)) : -1);
            writer.WriteValueSafe(throwableSO != null ? ThrowableLoadoutItems.IndexOf(ThrowableLoadoutItems.First(w => w == throwableSO)) : -1);
            writer.WriteValueSafe(equipmentSO != null ? EquipmentLoadoutItems.IndexOf(EquipmentLoadoutItems.First(w => w == equipmentSO)) : -1);
            writer.WriteValueSafe(driveSO != null ? DriveLoadoutItems.IndexOf(DriveLoadoutItems.First(w => w == driveSO)) : -1);
        }
        else
        {
            FastBufferReader reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out int weapon1Index);
            weapon1SO = weapon1Index != -1 ? WeaponLoadoutItems[weapon1Index] : null;
            reader.ReadValueSafe(out int weapon2Index);
            weapon2SO = weapon2Index != -1 ? WeaponLoadoutItems[weapon2Index] : null;
            reader.ReadValueSafe(out int heavyWeaponIndex);
            heavyWeaponSO = heavyWeaponIndex != -1 ? HeavyWeaponLoadoutItems[heavyWeaponIndex] : null;
            reader.ReadValueSafe(out int throwableIndex);
            throwableSO = throwableIndex != -1 ? ThrowableLoadoutItems[throwableIndex] : null;
            reader.ReadValueSafe(out int equipmentIndex);
            equipmentSO = equipmentIndex != -1 ? EquipmentLoadoutItems[equipmentIndex] : null;
            reader.ReadValueSafe(out int driveIndex);
            driveSO = driveIndex != -1 ? DriveLoadoutItems[driveIndex] : null;
        }
    }

    public bool Equals(CharacterLoadout other)
    {
        if (other == null) return false;

        return armorClass == other.armorClass &&
            weapon1SO == other.weapon1SO &&
            weapon2SO == other.weapon2SO &&
            heavyWeaponSO == other.heavyWeaponSO &&
            throwableSO == other.throwableSO &&
            equipmentSO == other.equipmentSO &&
            driveSO == other.driveSO;
    }

    private static List<LoadoutItemSO> _armorClassLoadoutItems;
    public static List<LoadoutItemSO> ArmorClassLoadoutItems
    {
        get
        {
            _armorClassLoadoutItems ??= Resources.LoadAll<LoadoutItemSO>("LoadoutItemSOs/ArmorClasses").ToList();
            return _armorClassLoadoutItems;
        }
    }

    private static List<LoadoutItemSO> _weaponLoadoutItems;
    public static List<LoadoutItemSO> WeaponLoadoutItems
    {
        get
        {
            _weaponLoadoutItems ??= Resources.LoadAll<LoadoutItemSO>("LoadoutItemSOs/Weapons").ToList();
            return _weaponLoadoutItems;
        }
    }

    private static List<LoadoutItemSO> _heavyWeaponLoadoutItems;
    public static List<LoadoutItemSO> HeavyWeaponLoadoutItems
    {
        get
        {
            _heavyWeaponLoadoutItems ??= Resources.LoadAll<LoadoutItemSO>("LoadoutItemSOs/HeavyWeapons").ToList();
            return _heavyWeaponLoadoutItems;
        }
    }

    private static List<LoadoutItemSO> _throwableLoadoutItems;
    public static List<LoadoutItemSO> ThrowableLoadoutItems
    {
        get
        {
            _throwableLoadoutItems ??= Resources.LoadAll<LoadoutItemSO>("LoadoutItemSOs/Throwables").ToList();
            return _throwableLoadoutItems;
        }
    }

    private static List<LoadoutItemSO> _equipmentLoadoutItems;
    public static List<LoadoutItemSO> EquipmentLoadoutItems
    {
        get
        {
            _equipmentLoadoutItems ??= Resources.LoadAll<LoadoutItemSO>("LoadoutItemSOs/Equipments").ToList();
            return _equipmentLoadoutItems;
        }
    }

    private static List<LoadoutItemSO> _driveLoadoutItems;
    public static List<LoadoutItemSO> DriveLoadoutItems
    {
        get
        {
            _driveLoadoutItems ??= Resources.LoadAll<LoadoutItemSO>("LoadoutItemSOs/Drives").ToList();
            return _driveLoadoutItems;
        }
    }

    public static LoadoutItemSO GetLoadoutItemSOFromPrefab(GameObject prefab)
    {
        string prefabName = prefab.name.Replace("(Clone)", "").Trim();
        LoadoutItemSO itemSO = null;
        if (prefab.GetComponentInChildren<Weapon>() != null)
            itemSO = WeaponLoadoutItems.FirstOrDefault(w => w != null && w.itemPrefab.name == prefabName);
        // else if (prefab.GetComponentInChildren<HeavyWeapon>() != null)
        //     itemSO = HeavyWeaponLoadoutItems.FirstOrDefault(w => w != null && w.itemPrefab.name == prefabName);
        else if (prefab.GetComponentInChildren<ThrowableManager>() != null)
            itemSO = ThrowableLoadoutItems.FirstOrDefault(w => w != null && w.itemPrefab.name == prefabName);
        // else if (prefab.GetComponentInChildren<Equipment>() != null)
        //     itemSO = EquipmentLoadoutItems.FirstOrDefault(w => w != null && w.itemPrefab.name == prefabName);
        else if (prefab.GetComponentInChildren<Drive>() != null)
            itemSO = DriveLoadoutItems.FirstOrDefault(w => w != null && w.itemPrefab.name == prefabName);

        return itemSO;
    }

    public LoadoutArmorClass armorClass;
    public LoadoutItemSO weapon1SO;
    public LoadoutItemSO weapon2SO;
    public LoadoutItemSO heavyWeaponSO;
    public LoadoutItemSO throwableSO;
    public LoadoutItemSO equipmentSO;
    public LoadoutItemSO driveSO;

    public CharacterLoadout()
    {
        armorClass = LoadoutArmorClass.Any;
        weapon1SO = null;
        weapon2SO = null;
        heavyWeaponSO = null;
        throwableSO = null;
        equipmentSO = null;
        driveSO = null;
    }

    public CharacterLoadout(CharacterLoadout other)
    {
        armorClass = other.armorClass;
        weapon1SO = other.weapon1SO;
        weapon2SO = other.weapon2SO;
        heavyWeaponSO = other.heavyWeaponSO;
        throwableSO = other.throwableSO;
        equipmentSO = other.equipmentSO;
        driveSO = other.driveSO;
    }

    public CharacterLoadout(LoadoutPresetSO loadoutSO)
    {
        armorClass = loadoutSO.armorClass;
        weapon1SO = loadoutSO.weapon1;
        weapon2SO = loadoutSO.weapon2;
        heavyWeaponSO = loadoutSO.heavyWeapon;
        throwableSO = loadoutSO.throwable;
        equipmentSO = loadoutSO.equipment;
        driveSO = loadoutSO.drive;
    }   
}