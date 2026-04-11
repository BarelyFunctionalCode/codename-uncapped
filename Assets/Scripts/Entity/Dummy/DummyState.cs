using Unity.Netcode;
using UnityEngine;

public class DummyState : State
{
    [SerializeField] private GameObject explodeParticleObj;
    private MeshRenderer meshRenderer;

    public override void Initialize(ulong ParentNetworkObjectId)
    {
        base.Initialize(ParentNetworkObjectId);
        meshRenderer = GetComponent<MeshRenderer>();
    }

    protected override void OnDie()
    {
        OnDieRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void OnDieRPC()
    {
        meshRenderer.enabled = false;
        explodeParticleObj.SetActive(true);
    }

    protected override void OnRespawn()
    {
        OnRespawnRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void OnRespawnRPC()
    {
        meshRenderer.enabled = true;
        explodeParticleObj.SetActive(false);
    }
}
