using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(State))]
public class Health : EntityAttributes, IDamageable
{
    private State entityState;
    public UnityEvent<float> onHealthChanged = new();
    public UnityEvent<float> onAppliedDamage = new();

    [SerializeField] private NetworkVariable<float> _health = new(0.0f);
    [SerializeField] private float _maxHealth;

    public float CurrentHealth => _health.Value;
    public float MaxHealth => _maxHealth;
    public float HealthPercentage => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;


    public override void Initialize(ulong ParentNetworkObjectId)
    {
        base.Initialize(ParentNetworkObjectId);

        entityState = GetComponent<State>();
        entityState.onStateChange.AddListener(OnEntityStateChange);

        _health.Value = _maxHealth;
    }

    public void OnEntityStateChange(EntityStates s)
    {
        switch (s)
        {
            case EntityStates.RESPAWN:
                _health.Value = _maxHealth;
                break;
            default:
                break;
        }
    }

    public void TakeDamage(
        float damage,
        NetworkBehaviourReference attackerRef = default,
        NetworkBehaviourReference weaponRef = default
    ) {
        gameObject.TryGetComponent(out Identification entityIdentification);
        ulong entityId = entityIdentification != null ? entityIdentification.FetchEntityId() : ulong.MaxValue;

        Identification attackerIdentification = null;
        attackerRef.TryGet(out PlayerController attacker);
        if (attacker != null) attacker.gameObject.TryGetComponent(out attackerIdentification);
        ulong attackerEntityId = attackerIdentification != null ? attackerIdentification.FetchEntityId() : ulong.MaxValue;

        bool doStatUpdates = !(attackerEntityId.Equals(ulong.MaxValue) || entityId.Equals(ulong.MaxValue));
        if (doStatUpdates)
        {
            attacker.TryGetComponent(out Health attackerHealth);
            if (attackerHealth != null) attackerHealth.OnAppliedDamageRpc(damage);
            GameModeHandler.Instance.StatEventReceiver(new StatEvent(
                StatEventType.DAMAGE_DEALT,
                damage,
                attackerEntityId
            ));
        }

        ApplyhealthDelta(-damage);

        if (CurrentHealth <= 0.0f)
        {
            Die((attackerRef, weaponRef));
        }
    }

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
    [Rpc(SendTo.Owner)]
    private void OnAppliedDamageRpc(float damage) => onAppliedDamage.Invoke(damage);

    private void Die(
        ( NetworkBehaviourReference, /* Attacker */
          NetworkBehaviourReference  /* Weapon */ )msg
    ) {
        // Destructure the msg
        ( NetworkBehaviourReference attackerRef,
          NetworkBehaviourReference weaponRef
        ) = msg;

        if (!IsServer || entityState.IsDead) return;

        string lethalSource = "The Game";
        string weaponName = "mysterious ways";

        // Self identification
        gameObject.TryGetComponent(out Identification entityIdentification);
        string entityName = entityIdentification != null ? entityIdentification.FetchEntityName() : null;
        ulong entityId = entityIdentification != null ? entityIdentification.FetchEntityId() : ulong.MaxValue;

        // Attacker identification
        Identification attackerIdentification = null;
        attackerRef.TryGet(out PlayerController attacker);
        if (attacker != null) attacker.gameObject.TryGetComponent(out attackerIdentification);
        string attackerEntityName = attackerIdentification != null ? attackerIdentification.FetchEntityName() : null;
        ulong attackerEntityId = attackerIdentification != null ? attackerIdentification.FetchEntityId() : ulong.MaxValue;

        weaponRef.TryGet(out Weapon weapon);
        ThrowableManager throwable = null;
        if (weapon == null) weaponRef.TryGet(out throwable);

        bool doStatUpdates = !(attackerEntityId.Equals(ulong.MaxValue) || entityId.Equals(ulong.MaxValue));
        if (doStatUpdates)
        {
            GameModeHandler.Instance.StatEventReceiver(
                new StatEvent(
                    StatEventType.DEATHS,
                    1.0f,
                    entityId
            ));

            GameModeHandler.Instance.StatEventReceiver(
                new StatEvent(
                    StatEventType.KILL,
                    1.0f,
                    attackerEntityId
            ));
        }

        if (entityName != null)
        {
            if (attackerEntityName != null) lethalSource = attackerEntityName;
            if (weapon != null || throwable != null)
            {
                GameObject weaponObj = weapon != null ? weapon.gameObject : throwable.gameObject;
                LoadoutItemSO itemSO = PlayerLoadout.GetLoadoutItemSOFromPrefab(weaponObj);
                weaponName = itemSO.itemName;
            }
            lethalSource += "'s " + weaponName;
            NotificationManager.Instance.SendKillFeedNotificationRpc(entityName, lethalSource);
        }

        entityState.Die();
    }
}
