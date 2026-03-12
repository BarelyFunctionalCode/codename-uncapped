using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class GravityEffector : MonoBehaviour
{
    SphereCollider sphereCollider;

    [SerializeField] private float gravityModifier = 0.2f;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }
    
    private void OnTriggerStay(Collider other)
    {
        IGravityModifiable affectedObject = other.GetComponentInParent<IGravityModifiable>();
        if (affectedObject != null)
        {
            float effectRatio = 1.0f - Vector3.Distance(transform.position, other.transform.position) / sphereCollider.radius;

            float effect = Mathf.Lerp(1f, gravityModifier, effectRatio);
            affectedObject.SetGravityModifier(effect);
        }
    }
}
