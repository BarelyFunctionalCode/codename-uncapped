using Unity.Netcode;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    private const float groundEnergyRegenRate = 12.5f;
    
    [Header("Entity Attributes")]
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;

    [SerializeField] private float energy = 0.0f;
    [SerializeField] private float maxEnergy;
    [SerializeField] private float energyRegenRate;
    [Range(0.0f, 2.0f)]
    [SerializeField] private float energyRegenFactor = 1.0f;

    private bool isDead = false;
    protected bool isGrounded = false;


    #region Virtual Overrides
    protected virtual void Awake()
    {
        health = maxHealth;
        energy = maxEnergy;
    }

    protected virtual void Update()
    {
        if (!IsServer || isDead) return;
		
        ApplyEnergyDelta((GetIsGrounded() ? groundEnergyRegenRate : energyRegenRate) * energyRegenFactor * Time.deltaTime);
    }
    #endregion
    
    #region Getters
    public float GetHealth()				{ return health; }
    public float GetHealthPercentage()	{ return health / maxHealth; }
    public bool GetIsGrounded() 			{ return isGrounded; }
    #endregion
    

    public void TakeDamage(float damage)
    {
        ApplyhealthDelta(-damage);
        if (health <= 0) Die();
    }
    
    public void ApplyhealthDelta(float amount)
    {
        if (!IsServer) return;
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    public float GetEnergy() { return energy; }
    public float GetEnergyPercentage() { return energy / maxEnergy; }
    public void ApplyEnergyDelta(float amount)
    {
        if (!IsServer) return;
        energy += amount;
        energy = Mathf.Min(energy, maxEnergy);
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
