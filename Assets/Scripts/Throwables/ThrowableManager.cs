using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ThrowableManager : NetworkBehaviour
{
    public static List<string> interactionTags = new() { "Terrain", "Player", "Projectile" };

    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] public GameObject throwablePrefabObj;
    [SerializeField] public Sprite iconSprite;

    public float maxAmmo = 5;
    private float fireRate = 2;

    protected Camera playerCamera;
    protected NetworkObject originalParentNetworkObject;
    private NetworkVariable<NetworkBehaviourReference> playerRef = new();

    private bool canThrow = true;
    private bool startedThrow = false;

    private float holdThrowDebounce = 0.05f;
    private float holdThrowDebounceTimer = 0f;
    private float throwForceFactorIncreaseRate = 0.01f;

    private NetworkVariable<float> throwForceFactor = new();
    public NetworkVariable<float> ammoCount = new();
    private NetworkVariable<float> fireRateTimer = new();

    private bool isInitialized = false;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (isInitialized) return;

        if (IsServer)
        {
            playerRef.Value = null;
            ammoCount.Value = maxAmmo;
            fireRateTimer.Value = 0;
        }

        // This is very important. This makes sure that when a late client joins, they get initialized properly.
        if (playerRef.Value.TryGet(out PlayerController playerController)) InitializeRpc(playerController, RpcTarget.Me);
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (IsOwner)
        {
            Vector3 newWeaponAimPosition = playerCamera.transform.position + playerCamera.transform.forward * 1000f;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, ~ignoreLayers))
                newWeaponAimPosition = hitInfo.point;

            ThrowableLookRpc(newWeaponAimPosition);
        }

        if (IsServer)
        {
            if (startedThrow)
            {
                holdThrowDebounceTimer += Time.deltaTime;

                if (holdThrowDebounceTimer >= holdThrowDebounce)
                {
                    throwForceFactor.Value += Mathf.Clamp01(throwForceFactorIncreaseRate);
                }
            }

            if (!canThrow)
            {
                fireRateTimer.Value += Time.deltaTime;

                if (ammoCount.Value > 0 && fireRateTimer.Value >= fireRate)
                {
                    fireRateTimer.Value = 0;
                    canThrow = true;
                }
            }
        }
    }
    [Rpc(SendTo.Server)]
    private void ThrowableLookRpc(Vector3 lookPosition)
    {
        transform.LookAt(lookPosition);
    }

    public void Initialize(PlayerController playerController)
    {
        if (!IsServer) return;
        playerRef.Value = new NetworkBehaviourReference(playerController);

        InitializeRpc(playerRef.Value);
        isInitialized = true;
    }
    [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
    public void InitializeRpc(NetworkBehaviourReference playerRef, RpcParams rpcParams = default)
    {
        if (isInitialized) return;

        playerRef.TryGet(out PlayerController playerController);
        originalParentNetworkObject = GetComponentInParent<NetworkObject>();
        transform.parent = playerController.throwableMountPoint;
        if (IsOwner)
        {
            playerCamera = Camera.main;
            playerController.playerHUD.SetThrowableUI(this);
        }

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
        transform.parent = originalParentNetworkObject.transform;
        isInitialized = false;
    }

    public void refillAmmo()
    {
        if (!IsServer) return;
        ammoCount.Value = maxAmmo;
    }

    public void StartThrow()
    {
        if (!IsServer || !canThrow) return;
        startedThrow = true;
    }

    public void ReleaseThrow()
    {
        if (!IsServer || !canThrow || !startedThrow) return;

        GameObject throwableObj = SpawnManager.Instance.Spawn(
            throwablePrefabObj,
            true,
            transform.position + transform.forward,
            transform.rotation,
            transform
        );
        // GameObject throwableObj = Instantiate(throwablePrefabObj, transform.position + transform.forward, transform.rotation);
        // NetworkObject networkObj = throwableObj.GetComponent<NetworkObject>();
        // networkObj.Spawn(true);
        // networkObj.TrySetParent(transform.GetComponentInParent<NetworkObject>());
        throwableObj.GetComponent<Throwable>().Throw(playerRef.Value, this, throwForceFactor.Value);
        ammoCount.Value--;
        canThrow = false;
        startedThrow = false;
        throwForceFactor.Value = 0;
        holdThrowDebounceTimer = 0;
    }
}
