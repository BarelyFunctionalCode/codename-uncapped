using Unity.Netcode;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private GameObject deathCam;
    [SerializeField] private GameObject deathUI;

    public void Initialize(bool isLocalPlayer)
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.AddExplosionForce(3000f, transform.position, 10f);
        }
        Destroy(gameObject, 5f);

        if (!isLocalPlayer) return;

        deathCam.SetActive(true);
        deathUI.SetActive(true);
    }
}
