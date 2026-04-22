using Unity.Netcode;
using UnityEngine;

public class Boost : Drive
{
    [SerializeField] GameObject boostEffectParticleSystemPrefab;
    private float effectValue = 15f;
    private float effectDuration = 2f;
    private float effectDurationTimer = 0f;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer) cooldown.Value = 10f;
    }

    protected sealed override void Update()
    {
        base.Update();

        if (effectDurationTimer > 0) 
        {
            effectDurationTimer -= Time.deltaTime;

            if (effectDurationTimer <= 0)
            {
                Deactivate();
            }
        }
    }

    protected sealed override bool CanTurnOnline()
    {
        return character.energy.CurrentEnergy >= 0f;
    }

    protected override bool OnActivated()
    {
        base.OnActivated();

        character.characterMovement.jetDirectionalForceXYMultiplier = effectValue;
        effectDurationTimer = effectDuration;

        return false;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        character.characterMovement.jetDirectionalForceXYMultiplier = 1f;
        effectDurationTimer = 0f;
    }

    [Rpc(SendTo.Everyone)]
    private void PlayBoostEffectClientRpc(Vector3 position)
    {
        GameObject particles = Instantiate(boostEffectParticleSystemPrefab, position, Quaternion.identity);
        particles.SetActive(true);
    }
}
