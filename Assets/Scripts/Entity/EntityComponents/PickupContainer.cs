using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

public class PickupContainer : EntityComponent
{   
    public Transform pickupHoldPoint;
    [SerializeField] private List<string> pickupNameWhitelist = new();
    public Pickup CurrentlyHeldPickup { get; private set; }

    private float maxThrowTime = 2f;
    private float startPutDownTime = -1f;

    private ParentConstraint objectContainerPointConstraint;

    public override void Initialize(Entity entity)
    {
        base.Initialize(entity, OnEntityStateChange);
    }

    public void OnEntityStateChange(EntityStates s)
    {
        switch (s)
        {
            case EntityStates.DEAD:
                if (CurrentlyHeldPickup != null)
                {
                    startPutDownTime = 0f;
                    TryPutDownRpc(Vector3.up);
                }
                break;
            default:
                break;
        }
    }

    public void TryPickUp(Pickup pickup)
    {
        if (!IsServer)
        {
            Debug.LogWarning("TryPickUp() called on client side for " + name + " on " + gameObject.name);
            return;
        }
        if (CurrentlyHeldPickup != null) return;
        if (pickupNameWhitelist.Count > 0 && !pickupNameWhitelist.Contains(pickup.name)) return;

        bool success = pickup.PickUp(this);
        if (!success) return;

        if (objectContainerPointConstraint != null) Destroy(objectContainerPointConstraint);
        objectContainerPointConstraint = pickup.gameObject.AddComponent<ParentConstraint>();
        objectContainerPointConstraint.AddSource(new ConstraintSource { sourceTransform = pickupHoldPoint, weight = 1f });
        objectContainerPointConstraint.constraintActive = true;

        if (pickupHoldPoint != null)
        {
            pickup.transform.position = pickupHoldPoint.position;
            pickup.transform.rotation = pickupHoldPoint.rotation;
        }
        else
        {
            pickup.transform.position = transform.position;
            pickup.transform.rotation = transform.rotation;
        }

        CurrentlyHeldPickup = pickup;
    }

    [Rpc(SendTo.Server)]
    public void StartPutDownRpc() => startPutDownTime = Time.time;

    [Rpc(SendTo.Server)]
    public void TryPutDownRpc(Vector3 throwDirection, bool doMaxThrow = false)
    {
        if (startPutDownTime < 0) startPutDownTime = Time.time;
        if (CurrentlyHeldPickup == null) return;

        objectContainerPointConstraint.constraintActive = false;
        Destroy(objectContainerPointConstraint);

        CurrentlyHeldPickup.transform.transform.position = pickupHoldPoint.position + transform.forward * 3f;
        Physics.SyncTransforms();

        if (!doMaxThrow) throwDirection *= Mathf.Clamp01((Time.time - startPutDownTime) / maxThrowTime);

        CurrentlyHeldPickup.PutDown(throwDirection);
        CurrentlyHeldPickup = null;
        startPutDownTime = -1f;
    }
}
