using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class Weapon : NetworkBehaviour
{
    public static List<string> interactionTags = new() { "Terrain", "Player", "Throwable" };
    [Header("Visuals")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject modelObj;
    [SerializeField] private GameObject projectilePrefabObj;
    [SerializeField] private Sprite reticleSprite;
    [SerializeField] private WeaponUI weaponUI;
    
    [Header("Attributes")]
    [SerializeField] private float maxAmmo = 10000;
    [SerializeField] private float damage = 1;
    [SerializeField] private float fireRate = 0.05f;
    
    [Header("Collision")]
    [SerializeField] private LayerMask ignoreLayers;


    [SerializeField] private bool canFire = true;

    private Projectile currentProjectile;
    
    protected Camera playerCamera;

    protected Transform ownerTransform;

    public NetworkVariable<bool> isEquiped = new(true);
    private NetworkVariable<float> ammoCount = new(0);
    private NetworkVariable<float> fireRateTimer = new(0);
    private bool isInitialized = false;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) ammoCount.Value = maxAmmo;
    }

    protected virtual void Update()
    {
        if (!isInitialized || !isEquiped.Value) return;

        if (IsOwner)
        {
            Vector3 newWeaponAimPosition = playerCamera.transform.position + playerCamera.transform.forward * 1000f;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, ~ignoreLayers))
                newWeaponAimPosition = hitInfo.point;

            if (weaponUI.gameObject.activeSelf) weaponUI.UpdateUI(ammoCount.Value, fireRateTimer.Value, fireRate);

            WeaponLookRpc(newWeaponAimPosition);
        }

        if (IsServer)
        {
            if (!canFire)
            {
                fireRateTimer.Value += Time.deltaTime;
                if (ammoCount.Value > 0 && fireRateTimer.Value >= fireRate)
                {
                    fireRateTimer.Value = 0;
                    canFire = true;
                }
            }
        }

        // Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red);
        // Debug.DrawRay(transform.position, transform.forward * 1000, Color.green);
        // Debug.DrawLine(transform.position, newWeaponAimPosition, Color.blue);
    }
    [Rpc(SendTo.Server)]
    private void WeaponLookRpc(Vector3 lookPosition)
    {
        transform.LookAt(lookPosition);
    }

    public void Initialize(PlayerController playerController)
    {
        if (!IsServer) return;
        ownerTransform = playerController.transform;

        InitializeRpc();
        isInitialized = true;
    }

    [Rpc(SendTo.Owner)]
    public void InitializeRpc()
    {
        playerCamera = Camera.main;
        weaponUI.Initialize(maxAmmo, reticleSprite);
        isInitialized = true;
    }

    [Rpc(SendTo.Everyone)]
    public void EquipRpc()
    {
        modelObj.SetActive(true);
        if (IsOwner) weaponUI.gameObject.SetActive(true);
        if (IsServer) isEquiped.Value = true;
    }

    [Rpc(SendTo.Everyone)]
    public void UnequipRpc()
    {
        modelObj.SetActive(false);

        if (IsOwner) weaponUI.gameObject.SetActive(false);
        if (IsServer) isEquiped.Value = false;
    }

    public void refillAmmo()
    {
        if (!IsServer) return;
        ammoCount.Value = maxAmmo;
    }

    public virtual void Fire()
    {
        if (!IsServer || !canFire) return;
        if (currentProjectile == null)
        {
            GameObject newProjectileObj = Instantiate(projectilePrefabObj, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            NetworkObject networkObj = newProjectileObj.GetComponent<NetworkObject>();
            networkObj.Spawn(true);
            networkObj.TrySetParent(projectileSpawnPoint.GetComponentInParent<NetworkObject>());
            currentProjectile = newProjectileObj.GetComponent<Projectile>();
            currentProjectile.Fire(ownerTransform, damage);
        }
        if (currentProjectile.hasHoldModifier)
        {
            DoHoldModifierStart(currentProjectile);
            return;
        };

        currentProjectile = null;
        ammoCount.Value--;
        canFire = false;
    }

    protected virtual void DoHoldModifierStart(Projectile currentProjectile) { }

    public virtual void StopFire()
    {
        if (!IsServer) return;
        if (currentProjectile == null || !currentProjectile.hasHoldModifier) return;

        DoHoldModifierEnd(currentProjectile);

        currentProjectile = null;
        ammoCount.Value--;
        canFire = false;
    }

    protected virtual void DoHoldModifierEnd(Projectile currentProjectile) {}
}
