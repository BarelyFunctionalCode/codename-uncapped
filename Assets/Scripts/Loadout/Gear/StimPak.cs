using UnityEngine;

public class StimPak : Gear
{
    private int totalHealAmount = 40;
    private int totalEnergyAmount = 40;

    private float effectInterval = 0.25f;
    private float effectIntervalTimer = 0f;
    private float effectDuration = 5f;
    private float effectDurationTimer = 0f;

    private bool isUsing = false;


    public sealed override void OnNetworkSpawn()
    {
        cooldown = 10f;
        rechargeRate = 60f;
        MaxAmmo = 2;
        base.OnNetworkSpawn();
        
    }

    protected sealed override void Update()
    {
        base.Update();

        if (!IsServer || !isUsing || character == null) return;

        effectIntervalTimer += Time.deltaTime;
        effectDurationTimer += Time.deltaTime;

        if (effectIntervalTimer >= effectInterval)
        {
            effectIntervalTimer = 0f;
            if (character.health.CurrentHealth < character.health.MaxHealth)
            {
                float healAmountPerTick = totalHealAmount / (effectDuration / effectInterval);
                character.health.ApplyhealthDelta(healAmountPerTick);
            }
            if (character.energy.CurrentEnergy < character.energy.MaxEnergy)
            {
                float energyAmountPerTick = totalEnergyAmount / (effectDuration / effectInterval);
                character.energy.ApplyEnergyDelta(energyAmountPerTick);
            }
        }

        if (effectDurationTimer >= effectDuration)
        {
            StopUse();
        }
    }

    protected override bool OnUse()
    {
        base.OnUse();
        isUsing = true;
        return false;
    }

    protected override void OnStopUse()
    {
        base.OnStopUse();
        isUsing = false;
        effectIntervalTimer = 0f;
        effectDurationTimer = 0f;
    }
}
