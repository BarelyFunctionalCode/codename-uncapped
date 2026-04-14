using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Identification))]
public class PlayerState : State
{
    private PlayerController playerController;
    private PlayerInputs playerInputs;
    [SerializeField] private AudioSource respawnAudioSource;

    private void Awake()
    {
        base.Initialize(entity);
        
        playerController = GetComponent<PlayerController>();
        playerInputs = playerController.playerInputs;
    }

    protected override void OnDie()
    {
        if (!IsServer) return;

        playerInputs.SetPlayerControlsRpc(false);
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

        Transform respawnPoint = LevelManager.Instance.GetSpawnPoint(entity.identification.FetchTeamId());

        if (respawnPoint) playerController.Teleport(respawnPoint.position, respawnPoint.rotation);
        else playerController.Teleport(Vector3.zero, Quaternion.identity);

        playerController.localPlayerCollider.enabled = true;
        playerController.localRb.isKinematic = false;
        playerController.playerLoadout.Deinitialize();
        playerController.playerLoadout.Initialize();

        OnRespawnRpc();
        playerInputs.SetPlayerControlsRpc(true);
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
