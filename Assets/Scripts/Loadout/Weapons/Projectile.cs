using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Projectile : NetworkBehaviour, IGravityModifiable
{
    [SerializeField] private SphereCollider projectileCollider;
    [SerializeField] protected CapsuleCollider damageRadiusTrigger;
    [SerializeField] protected AudioClip impactSound;
    [SerializeField] [Range(0, 1000)] private float soundMinDistance = 100;
    [SerializeField] [Range(0, 1000)] private float soundMaxDistance = 1000;
    [SerializeField] protected GameObject firedParticleObj;
    [SerializeField] private GameObject impactParticleObj;
    [SerializeField] private Material firedMaterial;
    [SerializeField] protected float launchForce = 400;
    [SerializeField] private ForceMode launchForceMode = ForceMode.VelocityChange;
    [SerializeField] protected float maxImpactForce = 2000;
    [SerializeField] private float damageRadius = 1f;
    [SerializeField] private float selfDestructTimer = 10;
    private float armingTimer = 0.5f;
    [SerializeField] public bool hasHoldModifier = false;

    private NetworkVariable<float> gravityModifier = new();

    protected NetworkBehaviourReference ownerRef;
    protected NetworkBehaviourReference weaponRef;
    protected Rigidbody rb;
    protected AudioSource audioSource;
    private Vector3 previousPosition;
    private List<Collider> damagedReceivers = new();
    protected float maxDamage;

    public bool isFired = false;
    private bool hasImpacted = false;
    private bool isArmed = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        previousPosition = rb.position;
        if (damageRadiusTrigger != null) damageRadiusTrigger.radius = damageRadius * 2;

        ownerRef.TryGet(out Character owner);
        if (owner != null)
        {
            Collider ownerCollider = owner.GetComponentInChildren<CharacterType>().characterCollider;
            Physics.IgnoreCollision(projectileCollider, ownerCollider);
        }
    }

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) gravityModifier.Value = 1f;

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1;
        audioSource.dopplerLevel = 0;
        audioSource.minDistance = soundMinDistance;
        audioSource.maxDistance = soundMaxDistance;

        rb.useGravity = false;
    }

    protected virtual void FixedUpdate()
    {
        if (!IsServer || !isFired) return;

        if (IsServer)
        {
            if (gravityModifier.Value != 1f)
            {
                gravityModifier.Value = Mathf.Lerp(gravityModifier.Value, 1f, Time.fixedDeltaTime * 5f);
                if (Mathf.Abs(gravityModifier.Value - 1f) < 0.01f) gravityModifier.Value = 1f;
            }
        }

        float gravityValue = gravityModifier.Value;
        if (launchForceMode == ForceMode.VelocityChange) gravityValue -= 1.0f;
        rb.AddForce(Physics.gravity * gravityValue, ForceMode.Force);

        selfDestructTimer -= Time.fixedDeltaTime;
        armingTimer -= Time.fixedDeltaTime;
        if (!isArmed && armingTimer <= 0)
        {
            isArmed = true;
            ownerRef.TryGet(out Character owner);
            if (owner != null)
            {
                Collider ownerCollider = owner.GetComponentInChildren<CharacterType>().characterCollider;
                Physics.IgnoreCollision(projectileCollider, ownerCollider, false);
            }
        }
        if (selfDestructTimer <= 0)
        {
            Impact();
        }

        transform.LookAt(rb.position + rb.linearVelocity.normalized);

        float currentDisplacement = (rb.position - previousPosition).magnitude;

        if (damageRadiusTrigger != null) damageRadiusTrigger.height = currentDisplacement * 2 + damageRadius * 2;
        if (damageRadiusTrigger != null) damageRadiusTrigger.center = new Vector3(0, damageRadiusTrigger.height / 2, 0);

        previousPosition = rb.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (Weapon.interactionIgnoreTags.Contains(collision.gameObject.tag)) return;

        if (!DoImpactCheck(collision)) return;

        selfDestructTimer /= 2.0f;

        Impact(collision.collider);
    }

    protected virtual bool DoImpactCheck(Collision collision) { return true; }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !isFired) return;
        AddDamageReceiver(other);
    }

    public void SetGravityModifier(float modifier)
    {
        if (!IsSpawned || !IsServer) return;
        gravityModifier.Value = modifier;
    }

    private void AddDamageReceiver(Collider receiverCollider)
    {
        if (!IsServer) return;
        IDamageable damageable = receiverCollider.gameObject.GetComponentInParent<IDamageable>();
        Rigidbody rb = receiverCollider.gameObject.GetComponentInParent<Rigidbody>();
        if (damageable == null && rb == null) return;

        foreach (Collider c in damagedReceivers)
        {
            IDamageable cDamageable = c.gameObject.GetComponentInParent<IDamageable>();
            Rigidbody cRb = c.gameObject.GetComponentInParent<Rigidbody>();

            if (c == receiverCollider) return;
            if (cDamageable != null && cDamageable == damageable) return;
            if (cRb != null && cRb == rb) return;
        }

        damagedReceivers.Add(receiverCollider);
    }

    public void Fire(NetworkBehaviourReference ownerRef, NetworkBehaviourReference weaponRef, float maxDamage)
    {
        if (!IsServer || isFired) return;

        this.ownerRef = ownerRef;
        this.weaponRef = weaponRef;
        this.maxDamage = maxDamage;

        Vector3 intialVelocity = Vector3.Project(transform.parent.GetComponentInParent<Rigidbody>().linearVelocity, transform.forward);

        NetworkObject.TryRemoveParent();
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.AddForce(intialVelocity, ForceMode.VelocityChange);
        rb.AddForce(transform.forward * launchForce, launchForceMode);
        projectileCollider.enabled = true;

        FireRpc();
        isFired = true;
    }
    [Rpc(SendTo.Everyone)]
    private void FireRpc()
    {
        if (firedMaterial != null)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshRenderer>() != null) child.GetComponent<MeshRenderer>().material = firedMaterial;
            }
        }

        if (firedParticleObj != null) firedParticleObj.SetActive(true);
        isFired = true;
    }

    private void Impact(Collider directImpactCollider = null) {
        if (!IsServer || hasImpacted) return;
        hasImpacted = true;
        
        GetComponentsInChildren<Collider>(true).ToList().ForEach(c => c.enabled = false);

        ImpactRpc();

        OnImpact();

        if (directImpactCollider != null)
        {
            IDamageable damageable = directImpactCollider.gameObject.GetComponentInParent<IDamageable>();
            Rigidbody rb = directImpactCollider.gameObject.GetComponentInParent<Rigidbody>();
            ApplyDamage(directImpactCollider.gameObject, maxDamage);
            if (rb != null) rb.AddForce(
                transform.forward * maxImpactForce,
                ForceMode.Impulse
            );
            foreach (Collider c in damagedReceivers.ToList())
            {
                if (c == null) continue;
                IDamageable cDamageable = c.gameObject.GetComponentInParent<IDamageable>();
                Rigidbody cRb = c.gameObject.GetComponentInParent<Rigidbody>();

                bool doRemove = false;
                if (c == directImpactCollider) doRemove = true;
                else if (cDamageable != null && cDamageable == damageable) doRemove = true;
                else if (cRb != null && cRb == rb) doRemove = true;

                if (doRemove) damagedReceivers.Remove(c);
            }
        }

        foreach (Collider receiver in damagedReceivers)
        {
            if (receiver == null) continue;
            
            float distance = Vector3.Distance(damageRadiusTrigger.transform.position, receiver.ClosestPoint(damageRadiusTrigger.transform.position));
            ApplyDamage(receiver.gameObject, maxDamage * Mathf.Max(1 - distance / damageRadius, 0));
            if (receiver.GetComponentInParent<Rigidbody>() != null) receiver.GetComponentInParent<Rigidbody>().AddExplosionForce(
                maxImpactForce,
                rb.position,
                damageRadius,
                1,
                ForceMode.Impulse
            );
        }

        Destroy(gameObject, impactSound != null ? impactSound.length : 0);
    }
    [Rpc(SendTo.Everyone)]
    private void ImpactRpc()
    {
        GetComponentsInChildren<MeshRenderer>(true).ToList().ForEach(r => r.enabled = false);

        if (impactParticleObj != null)
        {
            impactParticleObj.transform.parent = null;
            impactParticleObj.SetActive(true);
        }
        if (impactSound != null) audioSource.PlayOneShot(impactSound);

        if (firedParticleObj != null)
        {
            Vector3 firedParticlesScale = firedParticleObj.transform.localScale;
            firedParticleObj.transform.parent = null;
            firedParticleObj.transform.localScale = firedParticlesScale;
            firedParticleObj.GetComponent<ParticleSystem>().Stop();
        }
    }

    protected virtual void OnImpact() {}

    protected void ApplyDamage(GameObject target, float damage)
    {
        if (!IsServer) return;
        // print("Applying " + damage + " damage to " + target.name);
        //if (target.GetComponent<Entity>() != null) target.GetComponent<Entity>().TakeDamage(damage);
        if (target.GetComponentInParent<IDamageable>() != null) target.GetComponentInParent<IDamageable>().TakeDamage(damage, ownerRef, weaponRef);
    }
}
