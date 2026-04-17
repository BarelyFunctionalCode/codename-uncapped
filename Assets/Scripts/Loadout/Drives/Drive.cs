using Unity.Netcode;
using UnityEngine;

public class Drive : NetworkBehaviour
{
    private NetworkVariable<NetworkBehaviourReference> playerRef = new();
    protected PlayerController playerController;

    protected NetworkVariable<float> cooldown = new();
    private NetworkVariable<float> cooldownTimer = new();

    public NetworkVariable<bool> isOnline = new();
    private bool isActivated = false;
    private bool isInitialized = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        playerRef.OnValueChanged += OnPlayerRefUpdated;
        playerRef.Value = null;
    }

    public override void OnNetworkDespawn()
    {
        playerRef.OnValueChanged -= OnPlayerRefUpdated;
    }

    private void Update()
    {
        if (!IsServer || !isInitialized) return;
        if (playerController == null) return;

        if (cooldownTimer.Value > 0) cooldownTimer.Value -= Time.deltaTime;

        if (cooldownTimer.Value <= 0 && !isOnline.Value && CanTurnOnline()) isOnline.Value = true;
    }

    public float GetCooldownRatio()
    {
        if (cooldown.Value <= 0) return 0;
        return Mathf.Clamp01(cooldownTimer.Value / cooldown.Value);
    }

    private void OnPlayerRefUpdated(NetworkBehaviourReference previousValue, NetworkBehaviourReference newValue)
    {
        newValue.TryGet(out PlayerController playerController);
        this.playerController = playerController;
    }

    public void Initialize(PlayerController playerController)
    {
        if (!IsServer) return;

        playerRef.Value = new NetworkBehaviourReference(playerController);
        SetDriveUIClientRpc();
        isInitialized = true;
    }

    [Rpc(SendTo.Owner)]
    private void SetDriveUIClientRpc()
    {
        playerController.playerHUD.SetDrive(this);
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
        cooldownTimer.Value = cooldown.Value;
        OnDeactivated();
    }
    protected virtual void OnDeactivated() {}
}
