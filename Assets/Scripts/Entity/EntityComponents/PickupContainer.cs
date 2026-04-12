using System.Collections.Generic;
using UnityEngine;

public class PickupContainer : EntityComponent
{   
    [SerializeField] private List<string> pickupNameWhitelist = new();
    [SerializeField] private float pickupRange = 2f;
    public Pickup CurrentlyHeldPickup { get; private set; }

    public override void Initialize(ulong ParentNetworkObjectId, bool isServer)
    {
        base.Initialize(ParentNetworkObjectId, isServer);

        if (!isServer) return;

        SphereCollider col = GetComponent<SphereCollider>();
        col.enabled = true;
        col.radius = pickupRange;
    }

    public void TryPickUp(Pickup pickup)
    {
        if (!IsServer)
        {
            Debug.LogWarning("TryPickUp() called on client side for " + name + " on " + gameObject.name);
            return;
        }

        bool success = pickup.PickUp(this);
        if (success) CurrentlyHeldPickup = pickup;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        Pickup pickup = other.GetComponentInParent<Pickup>();
        if (pickup == null) return;
        if (pickupNameWhitelist.Count > 0 && !pickupNameWhitelist.Contains(pickup.name)) return;
        if (!pickup.CanBePickedUp.Value || pickup.isPickedUp) return;
        
        TryPickUp(pickup);
    }
}
