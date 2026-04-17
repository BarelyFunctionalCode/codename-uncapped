using Unity.Netcode;
using UnityEngine;

public class Blink : Drive
{
    [SerializeField] GameObject blinkEffectParticleSystemPrefab;
    private float range = 50;
    private float energyCost = 20;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer) cooldown.Value = 5f;
    }

    protected sealed override bool CanTurnOnline()
    {
        return playerController.energy.CurrentEnergy >= energyCost;
    }

    protected override bool OnActivated()
    {
        base.OnActivated();

        // Determine target position
        float verticalDir = 0;
        if (playerController.playerInputs.IsUpJetting) verticalDir += 1;
        if (playerController.playerInputs.IsDownJetting) verticalDir -= 1;
        Vector3 targetDirection = (playerController.playerInputs.MovementDirection + Vector3.up * verticalDir).normalized;
        if (targetDirection == Vector3.zero) targetDirection = Vector3.up;

        Vector3 startPosition = playerController.localPlayerType.playerCollider.bounds.center;
        Vector3 targetPosition = startPosition + targetDirection * range;

        // Raycast to check for obstacles
        if (Physics.Raycast(startPosition, targetDirection, out RaycastHit hit, range))
        {
            targetPosition = hit.point; // Truncate target position to hit point if an obstacle is in the way
        }

        // Teleport and apply energy cost
        PlayBlinkEffectClientRpc(startPosition);
        playerController.Teleport(targetPosition);
        PlayBlinkEffectClientRpc(targetPosition);
        playerController.energy.ApplyEnergyDelta(-energyCost);

        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayBlinkEffectClientRpc(Vector3 position)
    {
        GameObject particles = Instantiate(blinkEffectParticleSystemPrefab, position, Quaternion.identity);
        particles.SetActive(true);
    }
}
