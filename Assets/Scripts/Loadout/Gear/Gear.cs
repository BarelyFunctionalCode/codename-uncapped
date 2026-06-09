using Unity.Netcode;
using UnityEngine;

public class Gear : LoadoutItem
{
    private NetworkVariable<NetworkBehaviourReference> characterRef = new();
    protected Character character;

    [SerializeField] private GameObject modelObj;

    private LoadoutItemUI gearUI;

    private NetworkVariable<bool> canUse = new();

    protected float rechargeRate = -1f;
    private float rechargeTimer = 0f;
    private NetworkVariable<float> rechargeRatio = new();

    [SerializeField] private bool isInitialized = false;
    protected bool IsActive { get; private set; }

    [SerializeField] private bool useHeld = false;
    [SerializeField] private float heldTime = 0f;
    private float buttonHoldThresholdTime = 0.2f;
    [SerializeField] private bool firstButtonUp = true;
    [SerializeField] private float maxIdleUseTime = 5f;
    [SerializeField] private float idleUseTimer = 0f;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        characterRef.OnValueChanged += OnCharacterRefUpdated;
        if (IsServer)
        {
            characterRef.Value = null;
            ammo.Value = MaxAmmo;
        }
    }

    public override void OnNetworkDespawn()
    {
        characterRef.OnValueChanged -= OnCharacterRefUpdated;
    }

    protected virtual void Update()
    {
        if (!IsServer || !isInitialized) return;
        if (character == null) return;

        if (cooldownTimer.Value > 0) cooldownTimer.Value -= Time.deltaTime;
        if (cooldownTimer.Value <= 0 && !canUse.Value) canUse.Value = true;

        if (ammo.Value < MaxAmmo && rechargeRate > 0)
        {
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer >= rechargeRate)
            {
                ammo.Value = Mathf.Min(MaxAmmo, ammo.Value + 1);
                rechargeTimer = 0f;
            }
            rechargeRatio.Value = rechargeTimer / rechargeRate;
        }
 
        if (!IsActive) return;
        if (useHeld)
        {
            heldTime += Time.deltaTime;
            if (heldTime >= buttonHoldThresholdTime) HeldUse(Time.deltaTime, heldTime);
        }
        else
        {
            idleUseTimer += Time.deltaTime;
            if (idleUseTimer >= maxIdleUseTime) StopUse();
        }
    }

    public float GetRechargeRatio()
    {
        if (rechargeRate <= 0) return 0;
        return rechargeRatio.Value;
    }

    private void OnCharacterRefUpdated(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
    {
        newValue.TryGet(out Character character);
        this.character = character;
    }

    public void Initialize(Character character)
    {
        if (!IsServer) return;

        characterRef.Value = new NetworkBehaviourReference(character);
        InitializeRpc(characterRef.Value);
        isEquiped.Value = true;
        OnInitialize(character);
        isInitialized = true;
    }
    [Rpc(SendTo.Everyone)]
    private void InitializeRpc(NetworkBehaviourReference characterRef)
    {
        characterRef.TryGet(out Character character);
        this.character = character;

        if (modelObj != null)
        {
            modelObj.transform.parent = character.localCharacterType.gearMountPoint;
            modelObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        if (IsOwner && character.IsPlayerCharacter)
        {
            gearUI = Player.Instance.playerHUD.SetGearUI(this);
        }
        isInitialized = true;
    }
    protected virtual void OnInitialize(Character character) {}

    public void Deinitialize()
    {
        if (!IsServer) return;

        characterRef.Value = null;
        DeinitializeRpc();
        OnDeinitialize();
        isInitialized = false;
    }
    [Rpc(SendTo.Everyone)]
    public void DeinitializeRpc()
    {
        gearUI?.Deinitialize();
        if (modelObj != null) modelObj.transform.parent = transform;
        isEquiped.Value = false;
        isInitialized = false;
    }
    protected virtual void OnDeinitialize() {}

    public void Use(bool isButtonDown)
    {
        useHeld = isButtonDown;
        if (!isButtonDown)
        {
            if (IsActive && !firstButtonUp && heldTime < buttonHoldThresholdTime) StopUse();
            else firstButtonUp = false;
            return;
        }
        else heldTime = 0f;

        if (!IsServer || ammo.Value <= 0 || !canUse.Value || IsActive) return;

        IsActive = true;
        bool isFinished = OnUse();
        ammo.Value = Mathf.Max(0, ammo.Value - 1);
        cooldownTimer.Value = Cooldown;
        if (isFinished) StopUse();
    }
    protected virtual bool OnUse() { return true; }
    private void HeldUse(float deltaTime, float heldDuration) => OnHeldUse(deltaTime, heldDuration);
    protected virtual void OnHeldUse(float deltaTime, float heldDuration) {}

    public void StopUse()
    {
        if (!IsServer || !IsActive) return;

        IsActive = false;
        firstButtonUp = true;
        heldTime = 0f;
        idleUseTimer = 0f;
        canUse.Value = false;
        OnStopUse();
    }
    protected virtual void OnStopUse() {}
}
