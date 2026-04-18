using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class Weapon : NetworkBehaviour
{
    public static List<string> interactionIgnoreTags = new() { "Projectile" };
    [Header("Visuals")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject modelObj;
    [SerializeField] private GameObject projectilePrefabObj;
    [SerializeField] public Sprite iconSprite;
    [SerializeField] public Sprite reticleSprite;
    [SerializeField] protected AudioClip fireSound;
    
    [Header("Attributes")]
    [SerializeField] public float maxAmmo = 10000;
    [SerializeField] private float damage = 1;
    [SerializeField] private float fireRate = 0.05f;
    
    [Header("Collision")]
    [SerializeField] private LayerMask ignoreLayers;


    [SerializeField] private bool canFire = true;

    private Projectile currentProjectile;
    protected AudioSource audioSource;
    protected Camera playerCamera;

    protected NetworkObject originalParentNetworkObject;
    private NetworkVariable<NetworkBehaviourReference> characterRef = new();

    public NetworkVariable<bool> isEquiped = new();
    public NetworkVariable<float> ammoCount = new();
    private NetworkVariable<float> fireRateTimer = new();
    private bool isInitialized = false;


    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (isInitialized) return;
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1;
        audioSource.dopplerLevel = 0;
        audioSource.minDistance = 1;
        audioSource.maxDistance = 10;

        if (IsServer)
        {
            characterRef.Value = null;
            isEquiped.Value = false;
            ammoCount.Value = maxAmmo;
            fireRateTimer.Value = 0;
        }

        // This is very important. This makes sure that when a late client joins, they get initialized properly.
        if (characterRef.Value.TryGet(out Character character)) InitializeRpc(character, RpcTarget.Me);
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
    }
    [Rpc(SendTo.Server)]
    private void WeaponLookRpc(Vector3 lookPosition)
    {
        projectileSpawnPoint.LookAt(lookPosition);
    }

    public void Initialize(Character character)
    {
        if (!IsServer) return;
        characterRef.Value = new NetworkBehaviourReference(character);

        InitializeRpc(character);
        isInitialized = true;
    }
    [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
    public void InitializeRpc(NetworkBehaviourReference characterRef, RpcParams rpcParams = default)
    {
        if (isInitialized) return;

        characterRef.TryGet(out Character character);
        originalParentNetworkObject = GetComponentInParent<NetworkObject>();
        transform.parent = character.localCharacterType.weaponMountPoint;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        if (IsOwner && !character.isAI)
        {
            if (!IsHost && character.characterPuppetObj)
            {
                Vector3 localPosition = modelObj.transform.localPosition;
                Quaternion localRotation = modelObj.transform.localRotation;
                modelObj.transform.parent = character.characterPuppetObj.GetComponent<CharacterPuppet>().weaponMountPoint;
                modelObj.transform.localPosition = localPosition;
                modelObj.transform.localRotation = localRotation;
            }
            
            playerCamera = Camera.main;
            Player.Instance.playerHUD.AddWeaponUI(this);
        }

        if (isEquiped.Value) EquipRpc(RpcTarget.Me);
        else UnequipRpc(RpcTarget.Me);
        isInitialized = true;
    }

    public void Deinitialize()
    {
        if (!IsServer) return;
        DeinitializeRpc();
        isInitialized = false;
    }
    [Rpc(SendTo.Everyone)]
    public void DeinitializeRpc()
    {
        modelObj.transform.parent = transform;
        transform.parent = originalParentNetworkObject.transform;
        isInitialized = false;
    }

    [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
    public void EquipRpc(RpcParams rpcParams = default)
    {
        modelObj.SetActive(true);
        if (IsServer) isEquiped.Value = true;
    }

    [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
    public void UnequipRpc(RpcParams rpcParams = default)
    {
        modelObj.SetActive(false);

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
            GameObject newProjectileObj = SpawnManager.Instance.Spawn(
                projectilePrefabObj,
                true,
                projectileSpawnPoint.position,
                projectileSpawnPoint.rotation,
                projectileSpawnPoint
            );
            // GameObject newProjectileObj = Instantiate(projectilePrefabObj, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            // NetworkObject networkObj = newProjectileObj.GetComponent<NetworkObject>();
            // networkObj.Spawn(true);
            // networkObj.TrySetParent(projectileSpawnPoint.GetComponentInParent<NetworkObject>());
            currentProjectile = newProjectileObj.GetComponent<Projectile>();
            currentProjectile.Fire(characterRef.Value, this, damage);
            FireRpc();
            PostFiredRpc();
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

    [Rpc(SendTo.Everyone)]
    protected void FireRpc()
    {
        if (fireSound != null) audioSource.PlayOneShot(fireSound);
    }

    [Rpc(SendTo.Everyone)]
    protected virtual void PostFiredRpc() { }

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
