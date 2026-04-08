using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class FragGrenade : Throwable
{
    [SerializeField] private Transform blastWaveObj;
    [SerializeField] private Transform coreObj;
    [SerializeField, Range(1, 100)] private float maxDamage = 50;
    [SerializeField, Range(1, 100)] private float minDamage = 10;
    private Color startColor;
    private Color endColor;
    private float blastWaveRadius = 10f;
    private float blastWaveRadiusIncreaseRate = 5f;
    private float blastWaveFactor = 1f;

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

        if (blastWaveFactor > 0)
        {
            blastWaveFactor -= Time.fixedDeltaTime * blastWaveRadiusIncreaseRate;

            effectRadius = 1 + blastWaveRadius * (1 - blastWaveFactor);
            VisualRpc(blastWaveFactor, blastWaveRadius);
        }
        else
        {
            isFinished = true;
        }
    }
    [Rpc(SendTo.Everyone)]
    private void VisualRpc(float blastWaveFactor, float blastWaveRadius)
    {
        blastWaveObj.localScale = Vector3.Lerp(Vector3.one * blastWaveRadius, Vector3.zero, blastWaveFactor);
        blastWaveObj.GetComponent<MeshRenderer>().material.color = Color.Lerp(endColor, startColor, blastWaveFactor);
    }

    protected override void DoThrowableEffect(NetworkBehaviourReference _ownerRef, Transform receiverTransform, float effectFactor)
    {
        float damage = Mathf.Lerp(minDamage, maxDamage, blastWaveFactor) * effectFactor;


        if (receiverTransform.GetComponentInParent<IDamageable>() != null)
            receiverTransform.GetComponentInParent<IDamageable>().TakeDamage(damage, ownerRef, throwerRef);
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
