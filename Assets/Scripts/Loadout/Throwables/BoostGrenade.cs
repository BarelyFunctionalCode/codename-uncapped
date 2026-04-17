using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BoostGrenade : Throwable
{
    [SerializeField] private Transform blastWaveObj;
    [SerializeField] private Transform coreObj;
    [SerializeField, Range(1000, 10000)] private float maxBoostForce = 3000f;
    [SerializeField, Range(1000, 10000)] private float minBoostForce = 1000f;
    private Color startColor;
    private Color endColor;
    private float blastWaveRadius = 10f;
    private float blastWaveRadiusIncreaseRate = 5f;
    private float boostForceFactor = 1f;

    private float customGravity = 5f;

    public sealed override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        startColor = blastWaveObj.GetComponent<MeshRenderer>().material.color;
        endColor = startColor;
        endColor.a = 0;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Apply custom gravity
        GetComponent<Rigidbody>().AddForce(Vector3.down * customGravity, ForceMode.Acceleration);

        if (!isDetonating) return;

        if (boostForceFactor > 0)
        {
            boostForceFactor -= Time.fixedDeltaTime * blastWaveRadiusIncreaseRate;

            effectRadius = 1 + blastWaveRadius * (1 - boostForceFactor);
            VisualRpc(boostForceFactor, blastWaveRadius);
        }
        else
        {
            isFinished = true;
        }
    }
    [Rpc(SendTo.Everyone)]
    private void VisualRpc(float boostForceFactor, float blastWaveRadius)
    {
        blastWaveObj.localScale = Vector3.Lerp(Vector3.one * blastWaveRadius, Vector3.zero, boostForceFactor);
        blastWaveObj.GetComponent<MeshRenderer>().material.color = Color.Lerp(endColor, startColor, boostForceFactor);
    }

    protected override void DoThrowableEffect(NetworkBehaviourReference _ownerRef, Transform receiverTransform, float effectFactor)
    {
        Vector3 boostDirection = (receiverTransform.position - transform.position).normalized;
        float boostForce = Mathf.Lerp(minBoostForce, maxBoostForce, boostForceFactor) * effectFactor;

        receiverTransform.gameObject.GetComponentInParent<Rigidbody>().AddForce(boostDirection * boostForce, ForceMode.Impulse);
    }

    protected override void OnDetonate()
    {
        OnDetonateRpc();

        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        GetComponent<Rigidbody>().isKinematic = true;
        throwableCollider.enabled = false;
    }
    [Rpc(SendTo.Everyone)]
    private void OnDetonateRpc()
    {
        coreObj.gameObject.SetActive(false);
    }
}
