using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Pickup : EntityComponent
{
    [SerializeField] private SphereCollider pickupTrigger;
    [SerializeField] private float throwMinForce = 20f;
    [SerializeField] private float throwMaxForce = 40f;
    [SerializeField] private bool canBePickedUpOnSpawn = false;
    [HideInInspector] public NetworkVariable<bool> CanBePickedUp = new(false);

    public bool isConsumable = true;
    protected bool isOneHanded = true;
    protected Vector3 targetHoldRotation = Vector3.zero;
    public bool isPickedUp = false;
    protected PickupContainer pickerUpper;

    public Rigidbody Rb { get; private set; }
    public CollisionDetectionMode PreviousCollisionMode { get; private set; }
    public bool PreviousIsKinematic { get; private set; }
    public bool PreviousUseGravity { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        if (Rb != null)
        {
            PreviousCollisionMode = Rb.collisionDetectionMode;
            PreviousIsKinematic = Rb.isKinematic;
            PreviousUseGravity = Rb.useGravity;
        }
    }

    public override void Initialize(Entity entity)
    {
        base.Initialize(entity);

        if (!IsServer) pickupTrigger.enabled = false;
        else CanBePickedUp.Value = canBePickedUpOnSpawn;
    }

    public bool PickUp(PickupContainer pickerUpper)
    {
        if (!IsServer)
        {
            if (!IsSpawned)
            {
                Debug.LogWarning("PickUp() called for " + name + " on " + gameObject.name + " but the entity is not spawned.");
                return false;
            }
            Debug.LogWarning("PickUp() called on client side for " + name + " on " + gameObject.name);
            return false;
        }

        // Set the picker upper so that we can reference it in derived classes and run derived class specific code in PickUpState()
        this.pickerUpper = pickerUpper;

        // Run derived class specific code
        PickUpState();

        // Stop here if the pickup is consumable
        if (isConsumable)
        {
            Destroy(gameObject, 0.02f);
            return false;
        }

        // Check if the picker upper is already holding something
        if (pickerUpper.CurrentlyHeldPickup != null) return false;

        // Move pickup object to the picker upper
        NetworkObject.TrySetParent(pickerUpper.NetworkObject, true);
        Rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        Rb.isKinematic = true;
        Rb.useGravity = false;
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        pickupTrigger.enabled = false;
        isPickedUp = true;
        return true;
    }

    protected virtual void PickUpState() { }

    public void PutDown(Vector3 throwVector = default)
    {
        if (!IsServer)
        {
            Debug.LogWarning("PutDown() called on client side for " + name + " on " + gameObject.name);
            return;
        }

        // Reversing the pickup process basically
        isPickedUp = false;
        pickupTrigger.enabled = true;
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
        Rb.collisionDetectionMode = PreviousCollisionMode;
        Rb.isKinematic = PreviousIsKinematic;
        Rb.useGravity = PreviousUseGravity;
        NetworkObject.TryRemoveParent();

        // Run derived class specific code
        PutDownState();

        pickerUpper = null;

        // If specified, add force to the pickup to throw it
        Debug.Log("Throw vector: " + throwVector);
        if (throwVector != default)
        {
            float throwForce = Mathf.Lerp(throwMinForce, throwMaxForce, throwVector.magnitude);
            throwVector = throwVector.normalized * throwForce;
            Debug.Log("Applied throw vector: " + throwVector);
            Rb.AddForce(throwVector);
        }
    }

    protected virtual void PutDownState() { }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;
        if (!CanBePickedUp.Value || isPickedUp) return;

        PickupContainer pickupContainer = other.GetComponentInParent<PickupContainer>();
        if (pickupContainer == null) return;
        
        pickupContainer.TryPickUp(this);
    }
}
