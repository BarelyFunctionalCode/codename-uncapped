using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerLoadout : NetworkBehaviour
{
    [SerializeField] private List<GameObject> weaponPrefabObjList;
    [SerializeField] private GameObject throwablePrefabObj;
    private List<GameObject> currentWeaponsObjList;
    private int currentWeaponIndex = 0;
    private Weapon equippedPrimaryWeapon;

    private bool isPrimaryFiring = false;

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

        // AddThrowable(throwablePrefabObj, playerController);
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
    }


    private void AddWeapon(GameObject weaponPrefabObj, PlayerController playerController)
    {
        if (!IsServer) return;

        GameObject newWeapon = Instantiate(
            weaponPrefabObj,
            playerController.weaponMountPoint.position,
            playerController.weaponMountPoint.rotation
        );
        NetworkObject networkObj = newWeapon.GetComponent<NetworkObject>();
        networkObj.Spawn(true);
        networkObj.TrySetParent(playerController.NetworkObject);
        networkObj.ChangeOwnership(playerController.OwnerClientId);
        newWeapon = newWeapon.transform.GetComponentInChildren<Weapon>().gameObject;
        newWeapon.GetComponent<Weapon>().Initialize(playerController);

        currentWeaponsObjList.Add(newWeapon);
    }

    private void AddThrowable(GameObject throwablePrefabObj, Transform throwableMountPoint)
    {
        if (!IsServer) return;

        GameObject newThrowable = Instantiate(
            throwablePrefabObj,
            throwableMountPoint.position,
            throwableMountPoint.rotation,
            throwableMountPoint
        );
        NetworkObject networkObj = newThrowable.GetComponent<NetworkObject>();
        networkObj.Spawn(true);
        newThrowable.GetComponent<ThrowableManager>().Initialize(transform);
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
}
