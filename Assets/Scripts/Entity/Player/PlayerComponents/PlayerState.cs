using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Identification))]
public class PlayerState : State
{
    private Identification playerIdentification;
    private PlayerController playerController;
    [SerializeField] private AudioSource respawnAudioSource;

    public override void Initialize(ulong ParentNetworkObjectId, bool isServer)
    {
        base.Initialize(ParentNetworkObjectId, isServer);
        
        playerController = GetComponent<PlayerController>();
        playerIdentification = GetComponent<Identification>();
    }

    protected override void OnDie()
    {
        if (!IsServer) return;

        playerController.SetPlayerControlsRpc(false);
        playerController.localRb.isKinematic = true;
        playerController.localPlayerCollider.enabled = false;
        OnDieRpc();
    }
    [Rpc(SendTo.Everyone)]
    private void OnDieRpc()
    {
        if (IsOwner)
        {
            playerController.localRb.isKinematic = true;
            playerController.localPlayerCollider.enabled = false;
        }
        playerController.localPlayerType.OnDie();
    }

    protected override void OnRespawn()
    {
        if (!IsServer) return;

        Transform respawnPoint = LevelManager.Instance.GetSpawnPoint(playerIdentification.FetchTeamId());

        if (respawnPoint) playerController.Teleport(respawnPoint.position, respawnPoint.rotation);
        else playerController.Teleport(Vector3.zero, Quaternion.identity);

        playerController.localPlayerCollider.enabled = true;
        playerController.localRb.isKinematic = false;
        playerController.playerLoadout.Deinitialize();
        playerController.playerLoadout.Initialize();

        OnRespawnRpc();
        playerController.SetPlayerControlsRpc(true);
    }
    [Rpc(SendTo.Everyone)]
    private void OnRespawnRpc()
    {
        if (IsOwner)
        {
            playerController.localRb.isKinematic = false;
            playerController.localPlayerCollider.enabled = true;
            respawnAudioSource.Play();
        }
        playerController.localPlayerType.OnRespawn();
    }
}
