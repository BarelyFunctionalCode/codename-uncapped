using Unity.Netcode;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    private const float groundEnergyRegenRate = 12.5f;
    
    [Header("Entity Attributes")]
    [SerializeField] private NetworkVariable<float> health;
    [SerializeField] private float maxHealth;

    [SerializeField] private NetworkVariable<float> energy = new NetworkVariable<float>(0.0f);
    [SerializeField] private float maxEnergy;
    [SerializeField] private float energyRegenRate;
    [Range(0.0f, 2.0f)]
    [SerializeField] private float energyRegenFactor = 1.0f;

    private bool isDead = false;
    protected bool isGrounded = false;


    #region Virtual Overrides
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        health.Value = maxHealth;
        energy.Value = maxEnergy;
    }

    protected virtual void Update()
    {
        if (!IsServer || isDead) return;
		
        ApplyEnergyDelta((GetIsGrounded() ? groundEnergyRegenRate : energyRegenRate) * energyRegenFactor * Time.deltaTime);
    }
    #endregion
    
    #region Getters
    public float GetHealth()				{ return health.Value; }
    public float GetHealthPercentage()	{ return health.Value / maxHealth; }
    public bool GetIsGrounded() 			{ return isGrounded; }
    #endregion
    

    public void TakeDamage(float damage)
    {
        ApplyhealthDelta(-damage);
        if (health.Value <= 0) Die();
    }
    
    public void ApplyhealthDelta(float amount)
    {
        if (!IsServer) return;
        health.Value += amount;
        health.Value = Mathf.Clamp(health.Value, 0, maxHealth);
    }

    public float GetEnergy() { return energy.Value; }
    public float GetEnergyPercentage() { return energy.Value / maxEnergy; }
    public void ApplyEnergyDelta(float amount)
    {
        if (!IsServer) return;
        energy.Value += amount;
        energy.Value = Mathf.Min(energy.Value, maxEnergy);
    }

    private void Die()
    {
        if (!IsServer ||isDead) return;
        isDead = true;
        OnDie();
        Destroy(gameObject);
    }

    protected virtual void OnDie() {}
}
