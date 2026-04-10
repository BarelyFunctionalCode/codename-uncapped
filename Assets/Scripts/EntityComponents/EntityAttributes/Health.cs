using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class DamageTracker
{
    public NetworkBehaviourReference attackerRef;
    public NetworkBehaviourReference lastWeaponRef;
    private float TotalDamage;

    private const float DecayRate = 2f; // Example decay rate per second
    private float decayTimer = 0f;
    public float relevance => TotalDamage > 0f ? decayTimer / TotalDamage : 0f;


    public DamageTracker(NetworkBehaviourReference attackerRef, NetworkBehaviourReference lastWeaponRef, float initialDamage)
    {
        this.attackerRef = attackerRef;
        this.lastWeaponRef = lastWeaponRef;
        TotalDamage = initialDamage;
        decayTimer = initialDamage;
    }

    public void Update(NetworkBehaviourReference newWeaponRef, float additionalDamage)
    {
        lastWeaponRef = newWeaponRef;
        TotalDamage += additionalDamage;
        decayTimer = TotalDamage; // Reset decay timer on new damage
    }

    public void Decay(float deltaTime)
    {
        if (TotalDamage <= 0f) return;

        decayTimer -= deltaTime * DecayRate;
        if (decayTimer <= 0f)
        {
            attackerRef = default;
            lastWeaponRef = default;
            TotalDamage = 0f;
            decayTimer = 0f;
        }
    }
}

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

    List<DamageTracker> damageTrackers = new();

    private void Update()
    {
        if (!IsServer) return;

        float deltaTime = Time.deltaTime;
        foreach (DamageTracker dt in damageTrackers)
        {
            dt.Decay(deltaTime);
        }
        damageTrackers.RemoveAll(dt => dt.relevance <= 0f);
    }


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

        if (!entityId.Equals(attackerEntityId))
        {
            if (attackerEntityId != ulong.MaxValue)
            {
                DamageTracker existingTracker = damageTrackers.Find(dt => dt.attackerRef.Equals(attackerRef));
                if (existingTracker != null) existingTracker.Update(weaponRef, damage);
                else damageTrackers.Add(new DamageTracker(attackerRef, weaponRef, damage));
            }

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
        }

        ApplyhealthDelta(-damage);

        if (CurrentHealth <= 0.0f)
        {
            Die();
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

    private void Die() {
        if (!IsServer || entityState.IsDead) return;

        // Self identification
        gameObject.TryGetComponent(out Identification entityIdentification);
        string entityName = entityIdentification != null ? entityIdentification.FetchEntityName() : null;
        ulong entityId = entityIdentification != null ? entityIdentification.FetchEntityId() : ulong.MaxValue;

        // Get most relevant damage tracker
        damageTrackers.Sort((a, b) => b.relevance.CompareTo(a.relevance));
        DamageTracker mostRelevantDamage = damageTrackers.FirstOrDefault();
        damageTrackers.Remove(mostRelevantDamage);

        // Give assists to the rest
        foreach (DamageTracker dt in damageTrackers)
        {
            NetworkBehaviourReference assistAttackerRef = dt.attackerRef;
            assistAttackerRef.TryGet(out PlayerController assistAttacker);
            if (assistAttacker != null)
            {
                assistAttacker.TryGetComponent(out Identification assistAttackerIdentification);
                ulong assistAttackerEntityId = assistAttackerIdentification != null ? assistAttackerIdentification.FetchEntityId() : ulong.MaxValue;

                bool doAssistStatUpdates = !(assistAttackerEntityId.Equals(ulong.MaxValue) || entityId.Equals(ulong.MaxValue));
                if (doAssistStatUpdates)
                {
                    GameModeHandler.Instance.StatEventReceiver(new StatEvent(
                        StatEventType.KILL_ASSIST,
                        1.0f,
                        assistAttackerEntityId
                    ));
                }
            }
        }
        damageTrackers.Clear();

        // Attacker identification
        NetworkBehaviourReference attackerRef = mostRelevantDamage?.attackerRef ?? default;
        Identification attackerIdentification = null;
        attackerRef.TryGet(out PlayerController attacker);
        if (attacker != null) attacker.gameObject.TryGetComponent(out attackerIdentification);
        string attackerEntityName = attackerIdentification != null ? attackerIdentification.FetchEntityName() : null;
        ulong attackerEntityId = attackerIdentification != null ? attackerIdentification.FetchEntityId() : ulong.MaxValue;

        // Weapon identification
        NetworkBehaviourReference weaponRef = mostRelevantDamage?.lastWeaponRef ?? default;
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
            string lethalSource = "";
            if (attackerEntityName != null)
            {
                if (mostRelevantDamage.relevance > 0.3f)
                {
                    lethalSource += $"was killed by {attackerEntityName}";
                    if (weapon != null || throwable != null)
                    {
                        GameObject weaponObj = weapon != null ? weapon.gameObject : throwable.gameObject;
                        LoadoutItemSO itemSO = PlayerLoadout.GetLoadoutItemSOFromPrefab(weaponObj);
                        lethalSource += $"'s {itemSO.itemName}";
                    }
                }
                else lethalSource = $"died while trying to escape {attackerEntityName}";
            }
            else lethalSource = "was killed by The Game's mysterious ways";
            NotificationManager.Instance.SendKillFeedNotificationRpc(entityName, lethalSource);
        }

        entityState.Die();
    }
}
