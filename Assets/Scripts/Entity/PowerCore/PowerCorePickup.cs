using UnityEngine;

public class PowerCorePickup : Pickup
{
    protected override void PickUpState()
    {
        base.PickUpState();

        Debug.Log("Flag picked up by " + pickerUpper.name);
    }
}
