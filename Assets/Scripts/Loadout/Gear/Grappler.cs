using UnityEngine;

public class Grappler : Gear
{
    [SerializeField] private Transform endAttachmentPoint;
    [SerializeField] private Transform startVisualAttachmentPoint;

    private Rigidbody endAttachmentRigidbody;
    private ConfigurableJoint endToCharacterJoint;
    private SoftJointLimit endToCharacterJointLimit;
    private FixedJoint endToTargetJoint;
    private LineRenderer grappleLineRenderer;

    private float maxGrappleDistance = 100f;
    private bool heldMode = false;

    private float fastGrappleSpeed = 20f;
    private float slowGrappleSpeed = 10f;

    private void Awake()
    {
        endAttachmentRigidbody = endAttachmentPoint.GetComponent<Rigidbody>();
        endToCharacterJoint = endAttachmentPoint.GetComponent<ConfigurableJoint>();
        endToTargetJoint = endAttachmentPoint.GetComponent<FixedJoint>();
        endAttachmentPoint.GetChild(0).TryGetComponent(out grappleLineRenderer);
        endAttachmentPoint.gameObject.SetActive(false);
    }

    public sealed override void OnNetworkSpawn()
    {
        Cooldown = 0f;
        rechargeRate = 15f;
        MaxAmmo = 3;
        base.OnNetworkSpawn();
    }

    protected sealed override void Update()
    {
        base.Update();

        if (!IsServer || !IsActive || character == null) return;
        grappleLineRenderer.SetPosition(0, endAttachmentPoint.InverseTransformPoint(startVisualAttachmentPoint.position));


        if (!heldMode)
        {
            endToCharacterJointLimit.limit -= Time.deltaTime * fastGrappleSpeed;
            endToCharacterJoint.linearLimit = endToCharacterJointLimit;
        }

        endToCharacterJointLimit.limit = Mathf.Min(endToCharacterJointLimit.limit, Vector3.Distance(character.transform.position, endAttachmentPoint.position));
        endToCharacterJoint.linearLimit = endToCharacterJointLimit;

        if (endToCharacterJointLimit.limit < 0.5f) StopUse();
    }

    protected override void OnDeinitialize()
    {
        endToCharacterJoint.connectedBody = null;
        endToTargetJoint.connectedBody = null;
    }

    protected override bool OnUse()
    {
        if (!Physics.Raycast(
                character.localCharacterType.characterCollider.bounds.center,
                character.characterAimPosition - character.localCharacterType.characterCollider.bounds.center,
                out RaycastHit hitInfo,
                maxGrappleDistance
            ))
            return false;

        endAttachmentPoint.parent = null;
        endAttachmentRigidbody.isKinematic = true;
        endAttachmentPoint.gameObject.SetActive(true);
        endAttachmentPoint.position = hitInfo.point;
        if (hitInfo.collider.transform.TryGetComponent(out Rigidbody hitRb))
        {
            endToTargetJoint.connectedBody = hitRb;
            endAttachmentRigidbody.isKinematic = false;
        }
        else
        {
            // If the grapple doesn't hit a rigidbody, we can still attach it to the world by making the joint kinematic.
            endToTargetJoint.connectedBody = null;
            endAttachmentRigidbody.isKinematic = true;
        }
        endToCharacterJoint.connectedBody = character.localRb;
        endToCharacterJointLimit.limit = Vector3.Distance(character.transform.position, endAttachmentPoint.position);
        endToCharacterJoint.linearLimit = endToCharacterJointLimit;
        return false;
    }

    protected sealed override void OnHeldUse(float deltaTime, float heldDuration)
    {
        heldMode = true;
        endToCharacterJointLimit.limit -= deltaTime * slowGrappleSpeed;
        endToCharacterJoint.linearLimit = endToCharacterJointLimit;
    }

    protected override void OnStopUse()
    {
        heldMode = false;
        endToTargetJoint.connectedBody = null;
        endAttachmentRigidbody.isKinematic = true;
        endToCharacterJoint.connectedBody = null;
        endAttachmentPoint.parent = transform;
        endAttachmentPoint.position = startVisualAttachmentPoint.position;
        endAttachmentPoint.gameObject.SetActive(false);
    }
}
