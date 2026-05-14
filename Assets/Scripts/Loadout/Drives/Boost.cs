using Unity.Netcode;
using UnityEngine;

public class Boost : Drive
{
    [SerializeField] GameObject boostEffectParticleSystemPrefab;
    private float effectValue = 15f;

    public sealed override void OnNetworkSpawn()
    {
        cooldown = 10f;
        effectDuration = 3f;
        base.OnNetworkSpawn();
    }

    protected sealed override bool CanTurnOnline()
    {
        return character.energy.CurrentEnergy >= 0f;
    }

    protected override bool OnActivated()
    {
        base.OnActivated();

        character.characterMovement.jetDirectionalForceXYMultiplier = effectValue;
        // PlayBoostEffectClientRpc(character.transform.position);

        return false;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        character.characterMovement.jetDirectionalForceXYMultiplier = 1f;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayBoostEffectClientRpc(Vector3 position)
    {
        GameObject particles = Instantiate(boostEffectParticleSystemPrefab, position, Quaternion.identity);
        particles.SetActive(true);
    }
}
