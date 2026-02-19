using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerLoadout : NetworkBehaviour
{
    [SerializeField] private List<GameObject> weaponPrefabObjList;
    private List<GameObject> currentWeaponsObjList;
    private int currentWeaponIndex = 0;
    private Weapon equippedPrimaryWeapon;
    private bool isPrimaryFiring = false;

    [SerializeField] private GameObject throwablePrefabObj;
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

    public void Initialize(PlayerController playerController)
    {
        if (!IsServer) return;

        currentWeaponsObjList = new List<GameObject>();
        foreach (GameObject weaponPrefabObj in weaponPrefabObjList)
        {
            AddWeapon(weaponPrefabObj, playerController);
        }
        currentWeaponsObjList[0].GetComponent<Weapon>().EquipRpc();
        equippedPrimaryWeapon = currentWeaponsObjList[0].GetComponent<Weapon>();

        AddThrowable(throwablePrefabObj, playerController);
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


    private void AddWeapon(GameObject weaponPrefabObj, PlayerController playerController)
    {
        if (!IsServer) return;

        GameObject newWeapon = SpawnManager.Spawn(
            weaponPrefabObj,
            false,
            playerController.weaponMountPoint.position,
            playerController.weaponMountPoint.rotation,
            playerController.transform,
            playerController.OwnerClientId
        );
        // GameObject newWeapon = Instantiate(
        //     weaponPrefabObj,
        //     playerController.weaponMountPoint.position,
        //     playerController.weaponMountPoint.rotation
        // );
        // NetworkObject networkObj = newWeapon.GetComponent<NetworkObject>();
        // networkObj.Spawn(true);
        // networkObj.TrySetParent(playerController.NetworkObject);
        // networkObj.ChangeOwnership(playerController.OwnerClientId);
        newWeapon = newWeapon.transform.GetComponentInChildren<Weapon>().gameObject;
        newWeapon.GetComponent<Weapon>().Initialize(playerController);

        currentWeaponsObjList.Add(newWeapon);
    }

    private void AddThrowable(GameObject throwablePrefabObj, PlayerController playerController)
    {
        if (!IsServer) return;

        GameObject newThrowable = SpawnManager.Spawn(
            throwablePrefabObj,
            false,
            playerController.throwableMountPoint.position,
            playerController.throwableMountPoint.rotation,
            playerController.transform,
            playerController.OwnerClientId
        );
        // GameObject newThrowable = Instantiate(
        //     throwablePrefabObj,
        //     playerController.throwableMountPoint.position,
        //     playerController.throwableMountPoint.rotation
        // );
        // NetworkObject networkObj = newThrowable.GetComponent<NetworkObject>();
        // networkObj.Spawn(true);
        // networkObj.TrySetParent(playerController.NetworkObject);
        // networkObj.ChangeOwnership(playerController.OwnerClientId);
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
    public void OnPrimaryFireStartedRpc() => isPrimaryFiring = true;
    [Rpc(SendTo.Server)]
    public void OnPrimaryFireCanceledRpc() => isPrimaryFiring = false;
    [Rpc(SendTo.Server)]
    public void OnThrowableStartedRpc() => throwableManager.StartThrow();
    [Rpc(SendTo.Server)]
    public void OnThrowableCanceledRpc() => throwableManager.ReleaseThrow();
}
