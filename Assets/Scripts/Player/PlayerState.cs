using Unity.Netcode;
using UnityEngine;

public class PlayerState : State
{
    private PlayerController playerController;
    [SerializeField] private AudioSource respawnAudioSource;

    public override void Initialize(ulong ParentNetworkObjectId)
    {
        base.Initialize(ParentNetworkObjectId);
        TryGetComponent(out playerController);
        if (playerController == null) Debug.LogError("PlayerState requires a PlayerController component on the same GameObject.");
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

            // Disable the camera
            if (playerController.cineCam) playerController.cineCam.Priority.Value = 0;

            // TODO: Go to some other camera angle?
        }
        playerController.playerType.OnDie();
    }

    protected override void OnRespawn()
    {
        if (!IsServer) return;
        Identification id_component = gameObject.GetComponent<Identification>();

        Transform respawnPoint = LevelManager.Instance.GetSpawnPoint(id_component.FetchTeamId());

        if (respawnPoint)
        {
            playerController.Teleport(respawnPoint.position, respawnPoint.rotation);
        }

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
            // Enable the camera
            if (playerController.cineCam) playerController.cineCam.Priority.Value = 99;

            playerController.localRb.isKinematic = false;
            playerController.localPlayerCollider.enabled = true;
        }
        playerController.playerType.OnRespawn();
        respawnAudioSource.Play();
    }
}
