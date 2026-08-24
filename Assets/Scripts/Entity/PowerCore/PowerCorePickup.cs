using UnityEngine;

public class PowerCorePickup : Pickup
{
    public Identification lastHeldByIdentification;

    protected override void Awake()
    {
        base.Awake();
        PreviousIsKinematic = false;
    }

    protected override bool PickUpState()
    {
        bool baseSuccess = base.PickUpState();
        if (!baseSuccess) return false;

        // Check if power core is at the base or not. If it is at the base, only players from the other team can pick it up. If it is not at the base, any player can pick it up.
        // If the player is on the same team as the power core, picking it up will return it to base.
        Character character = pickerUpper.GetComponentInParent<Character>();
        if (character == null) return false;

        PowerCore powerCore = GetComponent<PowerCore>();
        if (powerCore == null) return false;

        Identification characterIdentifier = character.identification;
        if (powerCore.IsAtBase && characterIdentifier.TeamId == powerCore.TeamId)
        {
            return false;
        }

        if (!powerCore.IsAtBase && characterIdentifier.TeamId == powerCore.TeamId)
        {
            Debug.Log("Returning power core to base.");
            powerCore.ResetToBase();
            return false;
        }

        Debug.Log("Flag picked up by " + pickerUpper.name);
        
        Rb.interpolation = RigidbodyInterpolation.Interpolate;
        return true;
    }

    protected override void PutDownState()
    {
        base.PutDownState();

        Character character = pickerUpper.GetComponentInParent<Character>();
        if (character == null) return;

        lastHeldByIdentification = character.identification;
    }
}
