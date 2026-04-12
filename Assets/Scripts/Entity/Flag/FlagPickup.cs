using UnityEngine;

public class FlagPickup : Pickup
{
    public override void Initialize(ulong ParentNetworkObjectId, bool isServer)
    {
        base.Initialize(ParentNetworkObjectId, isServer);

        if (!isServer) return;

        CanBePickedUp.Value = true;
        isConsumable = false;
    }

    protected override void PickUpState()
    {
        base.PickUpState();

        Debug.Log("Flag picked up by " + pickerUpper.name);
    }
}
