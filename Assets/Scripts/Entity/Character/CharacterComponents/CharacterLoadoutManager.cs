using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class CharacterLoadoutManager : NetworkBehaviour
{
    [SerializeField] private LoadoutPresetSO defaultLoadoutPresetSO;
    private CharacterLoadout tempLoadout = null;
    public NetworkVariable<CharacterLoadout> currentLoadout;
    private Character character;
    private List<GameObject> currentWeaponsObjList;
    private int currentWeaponIndex = 0;
    private Weapon equippedPrimaryWeapon;
    private bool isPrimaryFiring = false;
    private bool isRestocked = false;

    private ThrowableManager throwableManager;

    private Drive equippedDrive;
    private Gear equippedGear;

    protected virtual void Update()
    {
        if (IsServer)
        {
            if (equippedPrimaryWeapon != null)
            {
                if (isPrimaryFiring) equippedPrimaryWeapon.Fire();
                if (!isPrimaryFiring) equippedPrimaryWeapon.StopFire();
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void UpdateLoadoutRpc(CharacterLoadout newLoadout, bool applyImmediately = false)
    {
        tempLoadout = newLoadout;
        if (applyImmediately && character != null)
        {
            Deinitialize();
            Initialize(true);
        }
    }

    public void Initialize(bool allowLoadoutChange = true, Character newCharacter = null)
    {
        if (!IsServer) return;

        if (newCharacter != null) character = newCharacter;
        else if (character == null) Debug.LogError("CharacterLoadoutManager: Initialize called without Character set!");

        if (allowLoadoutChange && tempLoadout != null)
        {
            currentLoadout.Value = new CharacterLoadout(tempLoadout);
            tempLoadout = null;
        }
        if (currentLoadout.Value == null)
        {
            currentLoadout.Value = new CharacterLoadout(defaultLoadoutPresetSO);
        }

        currentWeaponsObjList = new List<GameObject>();

        if (currentLoadout.Value.weapon1SO) AddWeapon(currentLoadout.Value.weapon1SO.itemPrefab, character);
        if (currentLoadout.Value.weapon2SO) AddWeapon(currentLoadout.Value.weapon2SO.itemPrefab, character);
        if (currentLoadout.Value.heavyWeaponSO) AddWeapon(currentLoadout.Value.heavyWeaponSO.itemPrefab, character);

        currentWeaponsObjList[0].GetComponent<Weapon>().EquipRpc();
        equippedPrimaryWeapon = currentWeaponsObjList[0].GetComponent<Weapon>();

        AddThrowable(currentLoadout.Value.throwableSO.itemPrefab, character);

        AddDrive(currentLoadout.Value.driveSO.itemPrefab, character);
        AddGear(currentLoadout.Value.gearSO.itemPrefab, character);
        isRestocked = true;
    }

    public void Deinitialize()
    {
        if (!IsServer) return;

        foreach (GameObject weaponObj in currentWeaponsObjList)
        {
            weaponObj.GetComponent<Weapon>().Deinitialize();
            NetworkObject networkObj = weaponObj.GetComponentInParent<NetworkObject>();
            networkObj.Despawn();
            Destroy(weaponObj);
        }
        currentWeaponsObjList.Clear();
        currentWeaponIndex = 0;
        equippedPrimaryWeapon = null;

        if (throwableManager != null)
        {
            throwableManager.Deinitialize();
            NetworkObject networkObj = throwableManager.GetComponentInParent<NetworkObject>();
            networkObj.Despawn();
            Destroy(throwableManager.gameObject);
            throwableManager = null;
        }
        if (equippedDrive != null)
        {
            equippedDrive.Deinitialize();
            NetworkObject networkObj = equippedDrive.GetComponentInParent<NetworkObject>();
            networkObj.Despawn();
            Destroy(equippedDrive.gameObject);
            equippedDrive = null;
        }
        if (equippedGear != null)
        {
            equippedGear.Deinitialize();
            NetworkObject networkObj = equippedGear.GetComponentInParent<NetworkObject>();
            networkObj.Despawn();
            Destroy(equippedGear.gameObject);
            equippedGear = null;
        }
    }

    public void Restock()
    {
        if (!IsServer || isRestocked) return;

        Deinitialize();
        Initialize(false);
        isRestocked = true;
    }


    private void AddWeapon(GameObject weaponPrefabObj, Character character)
    {
        if (!IsServer) return;

        GameObject newWeapon = SpawnManager.Instance.Spawn(
            weaponPrefabObj,
            false,
            character.localCharacterType.weaponMountPoint.position,
            character.localCharacterType.weaponMountPoint.rotation,
            character.transform,
            character.OwnerClientId
        );
        newWeapon = newWeapon.transform.GetComponentInChildren<Weapon>().gameObject;
        newWeapon.GetComponent<Weapon>().Initialize(character);

        currentWeaponsObjList.Add(newWeapon);
    }

    private void AddThrowable(GameObject throwablePrefabObj, Character character)
    {
        if (!IsServer) return;

        GameObject newThrowable = SpawnManager.Instance.Spawn(
            throwablePrefabObj,
            false,
            character.localCharacterType.throwableMountPoint.position,
            character.localCharacterType.throwableMountPoint.rotation,
            character.transform,
            character.OwnerClientId
        );
        throwableManager = newThrowable.GetComponentInChildren<ThrowableManager>();
        throwableManager.Initialize(character);
    }

    private void AddDrive(GameObject drivePrefabObj, Character character)
    {
        if (!IsServer) return;

        GameObject newDrive = SpawnManager.Instance.Spawn(
            drivePrefabObj,
            false,
            character.transform.position,
            character.transform.rotation,
            character.transform,
            character.OwnerClientId
        );
        equippedDrive = newDrive.GetComponentInChildren<Drive>();
        equippedDrive.Initialize(character);
    }

    private void AddGear(GameObject gearPrefabObj, Character character)
    {
        if (!IsServer) return;

        GameObject newGear = SpawnManager.Instance.Spawn(
            gearPrefabObj,
            false,
            character.transform.position,
            character.transform.rotation,
            character.transform,
            character.OwnerClientId
        );
        equippedGear = newGear.GetComponentInChildren<Gear>();
        equippedGear.Initialize(character);
    }

    [Rpc(SendTo.Server)]
    public void NextWeaponRpc()
    {
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().UnequipRpc();
        currentWeaponIndex = (currentWeaponIndex + 1) % currentWeaponsObjList.Count;
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().EquipRpc();
        equippedPrimaryWeapon = currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>();
    }
    [Rpc(SendTo.Server)]
    public void PreviousWeaponRpc()
    {
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().UnequipRpc();
        currentWeaponIndex = (currentWeaponIndex - 1 + currentWeaponsObjList.Count) % currentWeaponsObjList.Count;
        currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>().EquipRpc();
        equippedPrimaryWeapon = currentWeaponsObjList[currentWeaponIndex].GetComponent<Weapon>();
    }

    [Rpc(SendTo.Server)]
    public void OnPrimaryFireStartedRpc()
    {
        isPrimaryFiring = true;
        isRestocked = false;
    }
    [Rpc(SendTo.Server)]
    public void OnPrimaryFireCanceledRpc() => isPrimaryFiring = false;
    [Rpc(SendTo.Server)]
    public void OnThrowableStartedRpc()
    {
        throwableManager.StartThrow();
        isRestocked = false;
    }
    [Rpc(SendTo.Server)]
    public void OnThrowableCanceledRpc() => throwableManager.ReleaseThrow();

    [Rpc(SendTo.Server)]
    public void ActivateDriveRpc() => equippedDrive.Activate();

    [Rpc(SendTo.Server)]
    public void UseGearRpc() => equippedGear.Use();
}
