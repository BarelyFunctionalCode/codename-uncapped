using Unity.Netcode;
using UnityEngine;

public class RefillStation : NetworkBehaviour
{
    private float cooldown = 1f;
    private float cooldownTimer = 0f;

    // Update is called once per frame
    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime; // Decrease the cooldown timer
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsHost) return;
        if (other.CompareTag("Player") && cooldownTimer <= 0f)
        {
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                playerController.playerLoadout.Restock();
                cooldownTimer = cooldown; // Reset the cooldown timer
            }
        }
    }
}
