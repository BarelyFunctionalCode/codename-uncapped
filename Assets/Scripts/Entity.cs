using Unity.Netcode;
using UnityEngine;

public class Entity : NetworkBehaviour, IDamageable
{
    private const float groundEnergyRegenRate = 12.5f;
    
    [Header("Entity Attributes")]
    [SerializeField] protected ulong _entityId = 0;
    [SerializeField] protected string _entityName;
    [SerializeField] protected uint _teamId = 0;
    [SerializeField] private NetworkVariable<float> _health = new(0.0f);
    [SerializeField] private float _maxHealth;

    [SerializeField] private NetworkVariable<float> _energy = new(0.0f);
    [SerializeField] private float _maxEnergy;
    [SerializeField] private float _energyRegenRate;
    [Range(0.0f, 2.0f)]
    [SerializeField] private float _energyRegenFactor = 1.0f;

    private NetworkVariable<bool> _isDead = new(false);
    protected bool _isGrounded = false;

    public ulong EntityId => _entityId;
    public string EntityName => _entityName;
    public uint TeamId { get { return _teamId; } set { _teamId = value; } }

    public float Health => _health.Value;
    public float MaxHealth => _maxHealth;
    public float HealthPercentage => _maxHealth > 0f ? _health.Value / _maxHealth : 0f;
    public float Energy => _energy.Value;
    public float MaxEnergy => _maxEnergy;
    public float EnergyPercentage => _maxEnergy > 0f ? _energy.Value / _maxEnergy : 0f;

    public bool IsDead => _isDead.Value;
    public bool IsGrounded => _isGrounded;


    #region Virtual Overrides
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        if (_entityId == 0)
            _entityId = NetworkObjectId + 5000; // Arbitrary offset to avoid conflicts with player IDs, which are based on client IDs

        _health.Value = _maxHealth;
        _energy.Value = _maxEnergy;
    }

    protected virtual void Update()
    {
        if (!IsServer || _isDead.Value) return;
		
        ApplyEnergyDelta((IsGrounded ? groundEnergyRegenRate : _energyRegenRate) * _energyRegenFactor * Time.deltaTime);
    }
    #endregion
    

    public void TakeDamage(float damage, NetworkBehaviourReference attackerRef = default, NetworkBehaviourReference weaponRef = default)
    {
        ApplyhealthDelta(-damage);
        attackerRef.TryGet(out PlayerController attacker);

        GameModeHandler.Instance.StatEventReceiver(new StatEvent(
            StatEventType.DAMAGE_DEALT,
            damage,
            attacker.EntityId
        ));

        if (_health.Value <= 0) Die(attackerRef, weaponRef);
    }
    public void ApplyhealthDelta(float amount)
    {
        if (!IsServer) return;
        _health.Value += amount;
        _health.Value = Mathf.Clamp(_health.Value, 0, _maxHealth);
    }

    public void ApplyEnergyDelta(float amount)
    {
        if (!IsServer) return;
        _energy.Value += amount;
        _energy.Value = Mathf.Min(_energy.Value, _maxEnergy);
    }

    private void Die(NetworkBehaviourReference attackerRef, NetworkBehaviourReference weaponRef)
    {
        if (!IsServer || _isDead.Value) return;
        attackerRef.TryGet(out PlayerController attacker);
        weaponRef.TryGet(out Weapon weapon);
        if (attacker != null && weapon != null)
        {
            // TODO: Call stats manager singleton to log damage dealt
            // StatsManager.Instance.LogDamageDealt(ulong attackerClientId, ulong victimClientId, string weaponName, float damageAmount, bool isFatal);
            // StatsManager.Instance.LogDamageDealt(attacker.OwnerClientId, OwnerClientId, weapon.Name, damage, health.Value <= 0);
            print("Sending stat event");
            GameModeHandler.Instance.StatEventReceiver(new StatEvent(StatEventType.KILL, 1.0f, (ulong)0));
            NotificationManager.Instance.SendKillFeedNotificationRpc( EntityName, attacker.EntityName);
        }


        _isDead.Value = true;
        OnDie();
        
        Invoke(nameof(Respawn), 3f);
    }
    protected virtual void OnDie() {}

    private void Respawn()
    {
        if (!IsServer) return;
        OnRespawn();
        _health.Value = _maxHealth;
        _energy.Value = _maxEnergy;
        _isDead.Value = false;
    }
    protected virtual void OnRespawn() {}
}
