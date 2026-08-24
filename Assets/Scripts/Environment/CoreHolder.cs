using Unity.Netcode;
using UnityEngine;

public class CoreHolder : NetworkBehaviour
{
    public uint teamId;
    [SerializeField] PowerCore assignedPowerCore;

    [SerializeField] ParticleSystem coreHolderParticles;
    [SerializeField] Transform bobPathPointA;
    [SerializeField] Transform bobPathPointB;

    private float bobSpeed = 1.0f;
    private float rotateSpeed = 30.0f;

    private void Update()
    {
        if (assignedPowerCore != null && assignedPowerCore.IsAtBase)
        {
            if (!coreHolderParticles.isPlaying) coreHolderParticles.Play();
            
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * 0.5f + 0.5f;
            transform.position = Vector3.Lerp(bobPathPointA.position, bobPathPointB.position, bobOffset);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime); 
        }
        else
        {
            if (coreHolderParticles.isPlaying) coreHolderParticles.Stop();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsHost) return;

        PowerCore powerCore = null;
        // if (other.CompareTag("PowerCore")) powerCore = other.GetComponent<PowerCore>();
        if (other.CompareTag("Player"))
        {
            Character character = other.GetComponentInParent<Character>();
            if (character == null) return;

            PickupContainer pickupContainer = character.GetComponent<PickupContainer>();
            if (pickupContainer == null) return;

            Pickup pickup = pickupContainer.CurrentlyHeldPickup;
            if (pickup == null) return;

            pickup.TryGetComponent(out powerCore);
        }

        if (powerCore != null && powerCore.TeamId != teamId)
        {
            PowerCorePickup pickup = powerCore.GetComponent<PowerCorePickup>();
            Identification lastHeldByIdentification;
            if (!pickup.isPickedUp) lastHeldByIdentification = pickup.lastHeldByIdentification;
            else
            {
                Character character = pickup.pickerUpper.GetComponentInParent<Character>();
                if (character == null) return;

                lastHeldByIdentification = character.identification;
                character.GetComponent<PickupContainer>().TryPutDownRpc(Vector3.up);
            }
            
            if (lastHeldByIdentification != null)
            {
                GameModeHandler.Instance.StatEventReceiver(new StatEvent(
                    StatEventType.FLAG_CAPTURE,
                    1.0f,
                    lastHeldByIdentification.FetchEntityId()
                ));
                NotificationManager.Instance.SendCaptureNotificationRpc((int)teamId, lastHeldByIdentification.FetchEntityName());
            }

            powerCore.ResetToBase();
        }
    }
}
