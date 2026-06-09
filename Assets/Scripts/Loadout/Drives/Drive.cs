using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;


public enum DriveState
{
    Ready,
    Active,
    Cooldown
}

public class Drive : NetworkBehaviour
{
    private NetworkVariable<NetworkBehaviourReference> characterRef = new();
    protected Character character;

    private DriveUI driveUI;

    public NetworkVariable<DriveState> driveState = new();

    private bool isCooldownActive = false;
    protected float cooldown = 0f;
    private float cooldownTimer = 0f;
    public NetworkVariable<float> cooldownRatio = new();
    public NetworkVariable<int> cooldownSeconds = new();

    protected float effectDuration = 0f;
    private float effectDurationTimer = 0f;
    public NetworkVariable<float> effectDurationRatio = new();

    public NetworkVariable<bool> isOnline = new();
    private bool isActivated = false;
    private bool isInitialized = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        characterRef.OnValueChanged += OnCharacterRefUpdated;
        if (IsServer) characterRef.Value = null;
    }

    public override void OnNetworkDespawn()
    {
        characterRef.OnValueChanged -= OnCharacterRefUpdated;
    }

    protected virtual void Update()
    {
        if (!IsServer || !isInitialized) return;
        if (character == null) return;

        if (!isActivated)
        {
            if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                if (isCooldownActive)
                {
                    isCooldownActive = false;
                    driveState.Value = DriveState.Ready;
                }
                if (!isOnline.Value && CanTurnOnline()) isOnline.Value = true;
            }
            cooldownRatio.Value = cooldownTimer / cooldown;
            cooldownSeconds.Value = Mathf.CeilToInt(cooldownTimer);
        }
        else
        {
            if (effectDuration > 0)
            {
                if (effectDurationTimer > 0) effectDurationTimer -= Time.deltaTime;
                if (effectDurationTimer <= 0) Deactivate();
                effectDurationRatio.Value = effectDurationTimer / effectDuration;
            }
        }
    }

    public float GetCooldownRatio()
    {
        if (cooldown <= 0) return 0;
        return cooldownRatio.Value;
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
        if (character.IsPlayerCharacter) SetDriveUIClientRpc();
        isInitialized = true;
    }

    public void Deinitialize()
    {
        if (!IsServer) return;

        characterRef.Value = null;
        isInitialized = false;
        RemoveDriveUIClientRpc();
    }

    [Rpc(SendTo.Owner)]
    private void SetDriveUIClientRpc()
    {
        driveUI = Player.Instance.playerHUD.SetDrive(this);
    }

    [Rpc(SendTo.Owner)]
    private void RemoveDriveUIClientRpc()
    {
        driveUI?.Deinitialize();
        driveUI = null;
    }

    protected virtual bool CanTurnOnline() => false;

    public void Activate()
    {
        if (!IsServer || !isOnline.Value || isActivated) return;

        isActivated = true;
        bool isFinished = OnActivated();
        if (effectDuration > 0) effectDurationTimer = effectDuration;
        driveState.Value = DriveState.Active;
        if (isFinished) Deactivate();
    }
    protected virtual bool OnActivated() { return true; }

    public void Deactivate()
    {
        if (!IsServer || !isActivated) return;

        isActivated = false;
        cooldownTimer = cooldown;
        isCooldownActive = true;
        driveState.Value = DriveState.Cooldown;
        isOnline.Value = false;
        effectDurationTimer = 0f;
        effectDurationRatio.Value = 0f;
        OnDeactivated();
    }
    protected virtual void OnDeactivated() {}
}
