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
    private NetworkVariable<NetworkBehaviourReference> characterRef = new();

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
            characterRef.Value = null;
            ammoCount.Value = maxAmmo;
            fireRateTimer.Value = 0;
        }

        // This is very important. This makes sure that when a late client joins, they get initialized properly.
        if (characterRef.Value.TryGet(out Character character)) InitializeRpc(character, RpcTarget.Me);
    }

    public sealed override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isInitialized = false;
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

    public void Initialize(Character character)
    {
        if (!IsServer) return;
        characterRef.Value = new NetworkBehaviourReference(character);

        InitializeRpc(characterRef.Value);
        isInitialized = true;
    }
    [Rpc(SendTo.Everyone, AllowTargetOverride = true)]
    public void InitializeRpc(NetworkBehaviourReference characterRef, RpcParams rpcParams = default)
    {
        if (isInitialized) return;

        characterRef.TryGet(out Character character);
        originalParentNetworkObject = GetComponentInParent<NetworkObject>();
        transform.parent = character.localCharacterType.throwableMountPoint;
        if (IsOwner && !character.isAI.Value)
        {
            playerCamera = Camera.main;
            Player.Instance.playerHUD.SetThrowableUI(this);
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
        throwableObj.GetComponent<Throwable>().Throw(characterRef.Value, this, throwForceFactor.Value);
        ammoCount.Value--;
        canThrow = false;
        startedThrow = false;
        throwForceFactor.Value = 0;
        holdThrowDebounceTimer = 0;
    }
}
