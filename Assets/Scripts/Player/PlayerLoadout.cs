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

    public void Initialize(PlayerController playerController, Transform weaponMountPoint, Transform throwableMountPoint)
    {
        if (!IsServer) return;

        currentWeaponsObjList = new List<GameObject>();
        foreach (GameObject weaponPrefabObj in weaponPrefabObjList)
        {
            AddWeapon(weaponPrefabObj, playerController, weaponMountPoint);
        }

        // AddThrowable(throwablePrefabObj, throwableMountPoint);
    }


    private void AddWeapon(GameObject weaponPrefabObj, PlayerController playerController, Transform weaponMountPoint)
    {
        if (!IsServer) return;

        GameObject newWeapon = Instantiate(
            weaponPrefabObj,
            weaponMountPoint.position,
            weaponMountPoint.rotation
        );
        NetworkObject networkObj = newWeapon.GetComponent<NetworkObject>();
        networkObj.Spawn(true);
        networkObj.TrySetParent(weaponMountPoint.GetComponentInParent<NetworkObject>());
        networkObj.ChangeOwnership(playerController.OwnerClientId);
        newWeapon = newWeapon.transform.GetComponentInChildren<Weapon>().gameObject;
        newWeapon.transform.parent = weaponMountPoint;
        // TODO: track weapon network object for despawn on player death or removal.
        newWeapon.GetComponent<Weapon>().Initialize(playerController);

        currentWeaponsObjList.Add(newWeapon);

        if (currentWeaponsObjList.Count - 1 != currentWeaponIndex) newWeapon.GetComponent<Weapon>().UnequipRpc();
        else
        {
            newWeapon.GetComponent<Weapon>().EquipRpc();
            equippedPrimaryWeapon = newWeapon.GetComponent<Weapon>();
        }
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
