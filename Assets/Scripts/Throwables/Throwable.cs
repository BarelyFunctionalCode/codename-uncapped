using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Throwable : NetworkBehaviour
{
    [SerializeField] protected SphereCollider throwableCollider;
    [SerializeField] protected CapsuleCollider effectRadiusTrigger;
    [SerializeField] protected AudioClip detonateSound;

    [SerializeField] [Range(0, 1000)] private float soundMinDistance = 100;
    [SerializeField] [Range(0, 1000)] private float soundMaxDistance = 1000;
    [SerializeField] private float throwMaxForce = 4000f;
    [SerializeField] private float throwMinForce = 1000f;
    [SerializeField] protected float effectRadius = 1f;
    [SerializeField] private float selfDestructTimer = 1.5f;

    protected NetworkBehaviourReference ownerRef;
    protected NetworkBehaviourReference throwerRef;
    private Vector3 previousPosition;
    private List<Collider> affectedEntities = new List<Collider>();
    protected AudioSource audioSource;

    public bool isThrown = false;
    protected bool isDetonating = false;
    protected bool isFinished = false;


    protected virtual void Awake()
    {
        previousPosition = transform.position;
        if (effectRadiusTrigger != null) effectRadiusTrigger.radius = effectRadius * 2;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1;
        audioSource.minDistance = soundMinDistance;
        audioSource.maxDistance = soundMaxDistance;
    }

    protected virtual void FixedUpdate()
    {
        if (!IsServer || !isThrown) return;

        selfDestructTimer -= Time.fixedDeltaTime;
        if (selfDestructTimer <= 0)
        {
            Detonate();
        }

        if (isDetonating && isFinished)
        {
            Destroy(gameObject, detonateSound != null ? detonateSound.length : 0);
        }

        transform.LookAt(transform.position + GetComponent<Rigidbody>().linearVelocity.normalized);

        float currentDisplacement = (transform.position - previousPosition).magnitude;

        if (effectRadiusTrigger != null) effectRadiusTrigger.height = effectRadius * 2;
        // if (effectRadiusTrigger != null) effectRadiusTrigger.height = currentDisplacement + effectRadius * 2;
        if (effectRadiusTrigger != null) effectRadiusTrigger.radius = effectRadius;
        // if (effectRadiusTrigger != null) effectRadiusTrigger.center = new Vector3(0, effectRadiusTrigger.height / 2, 0);

        previousPosition = transform.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        if (!ThrowableManager.interactionTags.Contains(collision.gameObject.tag)) return;

        selfDestructTimer /= 2.0f;

        Collider impactCollider = null;
        if (collision.gameObject.CompareTag("Projectile")) Detonate(impactCollider);
        if (collision.gameObject.CompareTag("Player")) impactCollider = collision.collider;
        if (impactCollider != null) Detonate(impactCollider);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !isThrown || !isDetonating) return;
        if (!other.gameObject.CompareTag("Player")) return;
        if (!affectedEntities.Contains(other))
        {
            float distance = Vector3.Distance(transform.position, other.ClosestPoint(transform.position));

            // TODO: This is not at all right
            //float effectFactor = Mathf.Clamp01(1 - distance / effectRadius);

            // Isn't it just this?
            // The "1 +" is just to guarentee a larger area where the max effect happens
            // Ex: radius = 3, distance = 1 -> (3 - 1) / 3 = .66
            //     radius = 3, distance = 2.75 -> (3 - 2.75) / 3 = .25 / 3 = .083
            float effectFactor = Mathf.Clamp01((1 + effectRadius - distance) / effectRadius);

            DoThrowableEffect(ownerRef, other.transform, effectFactor);
            affectedEntities.Add(other);
        }
    }

    protected virtual void DoThrowableEffect(NetworkBehaviourReference ownerRef, Transform receiverTransform, float effectFactor) {}

    public void Throw(NetworkBehaviourReference ownerRef, NetworkBehaviourReference throwerRef, float throwForceFactor)
    {
        if (!IsServer) return;

        this.ownerRef = ownerRef;
        this.throwerRef = throwerRef;

        Vector3 intialVelocity = Vector3.Dot(transform.parent.GetComponentInParent<Rigidbody>().linearVelocity, transform.forward) * transform.forward;

        NetworkObject.TryRemoveParent();
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GetComponent<Rigidbody>().AddForce(intialVelocity, ForceMode.VelocityChange);
        GetComponent<Rigidbody>().AddForce(transform.forward * Mathf.Lerp(throwMinForce, throwMaxForce, throwForceFactor), ForceMode.Impulse);

        throwableCollider.enabled = true;
        isThrown = true;
    }

    private void Detonate(Collider impactCollider = null)
    {
        if (!IsServer) return;
        if (isDetonating) return;
        if (impactCollider != null) 
        {
            DoThrowableEffect(ownerRef, impactCollider.transform, 1);
            affectedEntities.Add(impactCollider);
        }
        DetonateRpc();
        OnDetonate();
    }
    [Rpc(SendTo.Everyone)]
    private void DetonateRpc()
    {
        isDetonating = true;
        if (detonateSound != null) audioSource.PlayOneShot(detonateSound);
    }

    protected virtual void OnDetonate() {}
}
