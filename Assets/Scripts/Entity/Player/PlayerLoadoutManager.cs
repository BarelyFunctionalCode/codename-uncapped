using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class PlayerLoadoutManager : NetworkBehaviour
{
    [SerializeField] private LoadoutPresetSO defaultLoadoutPresetSO;
    private PlayerLoadout tempLoadout = null;
    public NetworkVariable<PlayerLoadout> currentLoadout;
    private PlayerController playerController;
    private List<GameObject> currentWeaponsObjList;
    private int currentWeaponIndex = 0;
    private Weapon equippedPrimaryWeapon;
    private bool isPrimaryFiring = false;
    private bool isRestocked = false;

    private ThrowableManager throwableManager;

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
    public void UpdateLoadoutRpc(PlayerLoadout newLoadout, bool applyImmediately = false)
    {
        tempLoadout = newLoadout;
        if (applyImmediately && playerController != null)
        {
            Deinitialize();
            Initialize(true);
        }
    }

    public void Initialize(bool allowLoadoutChange = true, PlayerController newPlayerController = null)
    {
        if (!IsServer) return;

        if (newPlayerController != null) playerController = newPlayerController;
        else if (playerController == null) Debug.LogError("PlayerLoadoutManager: Initialize called without PlayerController set!");

        if (allowLoadoutChange && tempLoadout != null)
        {
            currentLoadout.Value = new PlayerLoadout(tempLoadout);
            tempLoadout = null;
        }
        if (currentLoadout.Value == null)
        {
            currentLoadout.Value = new PlayerLoadout(defaultLoadoutPresetSO);
        }

        currentWeaponsObjList = new List<GameObject>();

        if (currentLoadout.Value.weapon1SO) AddWeapon(currentLoadout.Value.weapon1SO.itemPrefab, playerController);
        if (currentLoadout.Value.weapon2SO) AddWeapon(currentLoadout.Value.weapon2SO.itemPrefab, playerController);
        if (currentLoadout.Value.heavyWeaponSO) AddWeapon(currentLoadout.Value.heavyWeaponSO.itemPrefab, playerController);

        currentWeaponsObjList[0].GetComponent<Weapon>().EquipRpc();
        equippedPrimaryWeapon = currentWeaponsObjList[0].GetComponent<Weapon>();

        AddThrowable(currentLoadout.Value.throwableSO.itemPrefab, playerController);
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
    }

    public void Restock()
    {
        if (!IsServer || isRestocked) return;

        Deinitialize();
        Initialize(false);
        isRestocked = true;
    }


    private void AddWeapon(GameObject weaponPrefabObj, PlayerController playerController)
    {
        if (!IsServer) return;

        GameObject newWeapon = SpawnManager.Instance.Spawn(
            weaponPrefabObj,
            false,
            playerController.weaponMountPoint.position,
            playerController.weaponMountPoint.rotation,
            playerController.transform,
            playerController.OwnerClientId
        );
        newWeapon = newWeapon.transform.GetComponentInChildren<Weapon>().gameObject;
        newWeapon.GetComponent<Weapon>().Initialize(playerController);

        currentWeaponsObjList.Add(newWeapon);
    }

    private void AddThrowable(GameObject throwablePrefabObj, PlayerController playerController)
    {
        if (!IsServer) return;

        GameObject newThrowable = SpawnManager.Instance.Spawn(
            throwablePrefabObj,
            false,
            playerController.throwableMountPoint.position,
            playerController.throwableMountPoint.rotation,
            playerController.transform,
            playerController.OwnerClientId
        );
        throwableManager = newThrowable.GetComponentInChildren<ThrowableManager>();
        throwableManager.Initialize(playerController);
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
}
