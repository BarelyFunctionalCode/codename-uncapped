using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Pickup : EntityComponent
{
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
        Rb = GetComponentInChildren<Rigidbody>();
        if (Rb != null)
        {
            PreviousCollisionMode = Rb.collisionDetectionMode;
            PreviousIsKinematic = Rb.isKinematic;
            PreviousUseGravity = Rb.useGravity;
        }
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
        if (Rb != null)
        {
            Rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Rb.isKinematic = true;
            Rb.useGravity = false;
        }
        isPickedUp = true;
        return true;
    }

    protected virtual void PickUpState() { }
}
