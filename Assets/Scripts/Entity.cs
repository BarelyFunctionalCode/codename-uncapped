using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Entity : NetworkBehaviour, IDamageable
{
    private const float groundEnergyRegenRate = 12.5f;
    
    [Header("Entity Attributes")]
    [SerializeField] protected NetworkVariable<ulong> _entityId = new(0);
    [SerializeField] protected NetworkVariable<FixedString32Bytes> _entityName = new("");
    [SerializeField] protected NetworkVariable<uint> _teamId = new(0);
    [SerializeField] private NetworkVariable<float> _health = new(0.0f);
    [SerializeField] private float _maxHealth;
    public UnityEvent<float> onHealthChanged = new();
    public UnityEvent<float> onAppliedDamage = new();

    [SerializeField] private NetworkVariable<float> _energy = new(0.0f);
    [SerializeField] private float _maxEnergy;
    [SerializeField] private float _energyRegenRate;
    [Range(0.0f, 2.0f)]
    [SerializeField] private float _energyRegenFactor = 1.0f;

    private NetworkVariable<bool> _isDead = new(false);
    protected bool _isGrounded = false;

    public ulong EntityId => _entityId.Value;
    public string EntityName => _entityName.Value.ToString();
    public uint TeamId { get { return _teamId.Value; } set { _teamId.Value = value; } }

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

        if (_entityId.Value == 0)
            _entityId.Value = NetworkObjectId + 5000; // Arbitrary offset to avoid conflicts with player IDs, which are based on client IDs

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

        if (attacker != null && attacker.EntityId != EntityId) attacker.OnAppliedDamageRpc(damage);
        GameModeHandler.Instance.StatEventReceiver(new StatEvent(
            StatEventType.DAMAGE_DEALT,
            damage,
            attacker.EntityId
        ));

        if (_health.Value <= 0) Die(attackerRef, weaponRef);
    }
    [Rpc(SendTo.Owner)]
    private void OnAppliedDamageRpc(float damage) => onAppliedDamage.Invoke(damage);

    public void ApplyhealthDelta(float amount)
    {
        if (!IsServer) return;
        float oldHealth = _health.Value;
        _health.Value += amount;
        _health.Value = Mathf.Clamp(_health.Value, 0, _maxHealth);

        float healthDeltaRatio = (_health.Value - oldHealth) / _maxHealth;
        ApplyhealthDeltaRpc(healthDeltaRatio);
    }

    [Rpc(SendTo.Owner)]
    public void ApplyhealthDeltaRpc(float ratio) => onHealthChanged.Invoke(ratio);

    public void ApplyEnergyDelta(float amount)
    {
        if (!IsServer) return;
        _energy.Value += amount;
        _energy.Value = Mathf.Min(_energy.Value, _maxEnergy);
    }

    public void Suicide() => Die(null, null, true);

    private void Die(NetworkBehaviourReference attackerRef, NetworkBehaviourReference weaponRef, bool isSuicide = false)
    {
        if (!IsServer || _isDead.Value) return;

        if (!isSuicide)
        {
            string lethalSource = "gravity";
            string weaponName = null;

            attackerRef.TryGet(out PlayerController attacker);
            weaponRef.TryGet(out Weapon weapon);
            ThrowableManager throwable = null;
            if (weapon == null) weaponRef.TryGet(out throwable);
            if (attacker != null && (weapon != null || throwable != null))
            {
                GameModeHandler.Instance.StatEventReceiver(new StatEvent(StatEventType.DEATHS, 1.0f, EntityId));
                GameModeHandler.Instance.StatEventReceiver(new StatEvent(StatEventType.KILL, 1.0f, attacker.EntityId));

                GameObject weaponObj = weapon != null ? weapon.gameObject : throwable.gameObject;
                LoadoutItemSO itemSO = PlayerLoadout.GetLoadoutItemSOFromPrefab(weaponObj);
                weaponName = itemSO.itemName;

                lethalSource = attacker.EntityName + "'s " + weaponName;
            }
            NotificationManager.Instance.SendKillFeedNotificationRpc(EntityName, lethalSource);
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
