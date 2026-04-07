using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Health : EntityAttributes
{
    public UnityEvent<float> onHealthChanged = new();
    public UnityEvent<float> onAppliedDamage = new();

    [SerializeField] private NetworkVariable<float> _health = new(0.0f);
    [SerializeField] private float _maxHealth;

    public float CurrentHealth => _health.Value;
    public float MaxHealth => _maxHealth;
    public float HealthPercentage => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;


    public void OnDamageTaken(
        float damage,
        NetworkBehaviourReference attackerRef = default,
        NetworkBehaviourReference weaponRef = default
    ) {
        ulong entityId = gameObject.GetComponent<Identification>().FetchEntityId();
        attackerRef.TryGet(out PlayerController attacker);
        ulong attackerEntityId = attacker.gameObject.GetComponent<Identification>().FetchEntityId();

        if (attacker != null && attackerEntityId != entityId)
        {
            attacker.GetComponent<Health>()?.OnAppliedDamageRpc(damage);
            GameModeHandler.Instance.StatEventReceiver(new StatEvent(
                StatEventType.DAMAGE_DEALT,
                damage,
                attackerEntityId
            ));
        }

        ApplyhealthDelta(-damage);

        if (CurrentHealth <= 0.0f)
        {
            Die((attackerRef, weaponRef, false));
        }
    }

    public void InitializeComponents()
    {
        _health.Value = _maxHealth;
    }

    public void OnEntityRespawn()
    {
        _health.Value = _maxHealth;
    }

    public void ApplyhealthDelta(float amount)
    {
        if (!IsServer) return;
        float oldHealth = _health.Value;
        _health.Value += amount;
        _health.Value = Mathf.Clamp(_health.Value, 0, _maxHealth);

        float healthDeltaRatio = (_health.Value - oldHealth) / _maxHealth;

        // Omitted - not necessary?
        // SendMessage("HealthValueChanged", _health.Value);

        ApplyhealthDeltaRpc(healthDeltaRatio);
    }

    [Rpc(SendTo.Owner)]
    public void ApplyhealthDeltaRpc(float ratio)
    {
        onHealthChanged.Invoke(ratio);
    }

    [Rpc(SendTo.Owner)]
    private void OnAppliedDamageRpc(float damage) => onAppliedDamage.Invoke(damage);

    public void StateChanged()
    {

    }

    public void Die(
        ( NetworkBehaviourReference, /* Attacker */
          NetworkBehaviourReference, /* Weapon */
          bool /* IsSuicide */ )msg
    ) {
        // Destructure the msg
        ( NetworkBehaviourReference attackerRef,
          NetworkBehaviourReference weaponRef,
          bool                      isSuicide
        ) = msg;

        if (!IsServer || gameObject.GetComponent<State>().IsDead) return;

        if (!isSuicide)
        {
            string lethalSource = "gravity";
            string weaponName = null;

            // Self identification
            Identification entity_identification = gameObject.GetComponent<Identification>();

            string EntityName = entity_identification.FetchEntityName();
            ulong EntityId = entity_identification.FetchEntityId();

            // Attacker identification
            attackerRef.TryGet(out PlayerController attacker);

            entity_identification = attacker.gameObject.GetComponent<Identification>();
            string AttackerEntityName = entity_identification.FetchEntityName();
            ulong AttackerEntityId = entity_identification.FetchEntityId();

            weaponRef.TryGet(out Weapon weapon);
            ThrowableManager throwable = null;

            if (weapon == null) weaponRef.TryGet(out throwable);

            if (attacker != null && (weapon != null || throwable != null))
            {
                GameModeHandler.Instance.StatEventReceiver(
                    new StatEvent(
                        StatEventType.DEATHS,
                        1.0f,
                        EntityId
                ));

                GameModeHandler.Instance.StatEventReceiver(
                    new StatEvent(
                        StatEventType.KILL,
                        1.0f,
                        AttackerEntityId
                ));

                GameObject weaponObj = weapon != null ? weapon.gameObject : throwable.gameObject;
                LoadoutItemSO itemSO = PlayerLoadout.GetLoadoutItemSOFromPrefab(weaponObj);
                weaponName = itemSO.itemName;

                lethalSource = AttackerEntityName + "'s " + weaponName;
            }
            NotificationManager.Instance.SendKillFeedNotificationRpc(EntityName, lethalSource);
        }

        // TODO Refactor these messages

        // Message to components
        SendMessage("Died");

        // Callback for inherited types
        SendMessage("OnDie");
        // OnDie();

        // Callback for inherited types
        SendMessage("Respawn", 3f);
        // Invoke(nameof(Respawn), 3f);
    }


}
