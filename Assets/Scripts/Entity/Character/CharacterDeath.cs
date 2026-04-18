using Unity.Netcode;
using UnityEngine;

public class CharacterDeath : MonoBehaviour
{
    [SerializeField] private GameObject deathCam;
    [SerializeField] private GameObject deathUI;

    public void Initialize(bool isLocalPlayerCharacter, Vector3 inheritedVelocity)
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.AddForce(inheritedVelocity, ForceMode.VelocityChange);
            rb.AddExplosionForce(3000f, transform.position, 10f);
        }
        Destroy(gameObject, 5f);

        if (!isLocalPlayerCharacter) return;

        deathCam.SetActive(true);
        deathUI.SetActive(true);
    }
}
