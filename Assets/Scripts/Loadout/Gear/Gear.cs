using Unity.Netcode;
using UnityEngine;

public class Gear : NetworkBehaviour
{
    private NetworkVariable<NetworkBehaviourReference> characterRef = new();
    protected Character character;

    [SerializeField] public Sprite iconSprite;

    protected float cooldown = 0f;
    public NetworkVariable<float> cooldownTimer = new();
    private NetworkVariable<bool> canUse = new();

    public int MaxAmmo { get; protected set; }
    public NetworkVariable<int> ammo = new();

    protected float rechargeRate = -1f;
    private float rechargeTimer = 0f;
    private NetworkVariable<float> rechargeRatio = new();

    private bool isActive = false;
    private bool isInitialized = false;

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
        if (character.IsPlayerCharacter.Value) SetGearUIClientRpc();
        isInitialized = true;
    }

    public void Deinitialize()
    {
        if (!IsServer) return;

        characterRef.Value = null;
        isInitialized = false;
    }

    [Rpc(SendTo.Owner)]
    private void SetGearUIClientRpc()
    {
        Player.Instance.playerHUD.SetGearUI(this);
    }

    public void Use()
    {
        if (!IsServer || ammo.Value <= 0 || !canUse.Value || isActive) return;

        isActive = true;
        bool isFinished = OnUse();
        ammo.Value = Mathf.Max(0, ammo.Value - 1);
        cooldownTimer.Value = cooldown;
        if (isFinished) StopUse();
    }
    protected virtual bool OnUse() { return true; }

    public void StopUse()
    {
        if (!IsServer || !isActive) return;

        isActive = false;
        canUse.Value = false;
        OnStopUse();
    }
    protected virtual void OnStopUse() {}
}
