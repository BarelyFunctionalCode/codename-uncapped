using Unity.Netcode.Components;
using UnityEngine;

public class PlayerPuppet : MonoBehaviour
{
    [SerializeField] public Transform freeLookTargetTransform;
    [SerializeField] public AudioSource hoverAudioSource;
    [SerializeField] public AudioSource windAudioSource;
    private PlayerController playerController;
    private Rigidbody authoritativeRb;
    private NetworkTransform networkTransform;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (playerController != null)
        {
            // Sync the puppet's position and rotation with the player's transform
            rb.position = Vector3.Lerp(rb.position, authoritativeRb.position, 0.5f);
            rb.PublishTransform();
        }
    }

    public void Initialize(PlayerController playerController)
    {
        this.playerController = playerController;
        authoritativeRb = playerController.GetComponent<Rigidbody>();
    }


}
