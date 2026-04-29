using Unity.Netcode;
using UnityEngine;

public class Drive : NetworkBehaviour
{
    private NetworkVariable<NetworkBehaviourReference> characterRef = new();
    protected Character character;

    protected float cooldown = 0f;
    private float cooldownTimer = 0f;
    public NetworkVariable<float> cooldownRatio = new();

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

        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
        cooldownRatio.Value = cooldownTimer / cooldown;

        if (cooldownTimer <= 0 && !isOnline.Value && CanTurnOnline()) isOnline.Value = true;
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
    }

    [Rpc(SendTo.Owner)]
    private void SetDriveUIClientRpc()
    {
        Player.Instance.playerHUD.SetDrive(this);
    }

    protected virtual bool CanTurnOnline() => false;

    public void Activate()
    {
        if (!IsServer || !isOnline.Value || isActivated) return;

        isActivated = true;
        bool isFinished = OnActivated();
        if (isFinished) Deactivate();
    }
    protected virtual bool OnActivated() { return true; }

    public void Deactivate()
    {
        if (!IsServer || !isActivated) return;

        isActivated = false;
        isOnline.Value = false;
        cooldownTimer = cooldown;
        OnDeactivated();
    }
    protected virtual void OnDeactivated() {}
}
