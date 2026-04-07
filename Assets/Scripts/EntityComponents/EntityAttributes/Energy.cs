using Unity.Netcode;
using UnityEngine;
using System;

public class Energy : EntityAttributes
{
    private const float groundEnergyRegenRate = 12.5f;

    [SerializeField] private NetworkVariable<float> _energy = new(0.0f);
    [SerializeField] private float _maxEnergy;
    [SerializeField] private float _ungroundedEnergyRegenRate;
    [Range(0.0f, 2.0f)]
    [SerializeField] private float _energyRegenFactor = 1.0f;

    private float _currentEnergyRegenRate = groundEnergyRegenRate;

    public float CurrentEnergy => _energy.Value;
    public float MaxEnergy => _maxEnergy;
    public float EnergyPercentage => MaxEnergy > 0f ? CurrentEnergy / MaxEnergy : 0f;


    public override void Initialize(ulong ParentNetworkObjectId)
    {
        base.Initialize(ParentNetworkObjectId);

        _energy.Value = _maxEnergy;

        TryGetComponent(out State stateComponent);
        if (stateComponent != null) stateComponent.onStateChange.AddListener(OnEntityStateChange);
    }

    public void OnEntityStateChange(EntityStates s)
    {
        // Listening for state changes from States to alter regen rate
        switch (s)
        {
            case EntityStates.GROUNDED:
            case EntityStates.ALIVE:
                _currentEnergyRegenRate = groundEnergyRegenRate;
                break;
            case EntityStates.UNGROUNDED:
                _currentEnergyRegenRate = _ungroundedEnergyRegenRate;
                break;
            case EntityStates.DEAD:
                _currentEnergyRegenRate = 0.0f;
                break;
            case EntityStates.RESPAWN:
                _energy.Value = _maxEnergy;
                break;
            default:
                break;
        }
    }

    public void ApplyEnergyDelta(float amount)
    {
        if (!IsServer) return;
        _energy.Value += amount;
        _energy.Value = Mathf.Min(_energy.Value, _maxEnergy);
    }

    protected virtual void Update()
    {
        if (!IsServer) return;

        ApplyEnergyDelta(_currentEnergyRegenRate * _energyRegenFactor * Time.deltaTime);
    }
}
