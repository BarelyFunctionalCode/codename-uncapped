using Unity.Netcode;
using UnityEngine;

public class Dummy : Entity, IIdentifiable
{
    private Material material;

    [SerializeField] private GameObject explodeParticleObj;

    private void Awake()
    {
        material = GetComponent<MeshRenderer>().materials[0];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        material.color = Color.Lerp(Color.green, Color.red, 1.0f - GetHealthPercentage());
    }

    protected override void OnDie()
    {
        OnDieRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void OnDieRPC()
    {
        GetComponent<MeshRenderer>().enabled = false;
        explodeParticleObj.SetActive(true);
    }

    protected override void OnRespawn()
    {
        OnRespawnRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void OnRespawnRPC()
    {
        GetComponent<MeshRenderer>().enabled = true;
        explodeParticleObj.SetActive(false);
    }

    public IdentifierData GetIdentifierData()
    {
        return new IdentifierData
        {
            color = IdentifierManager.TempTeamColors[GetTeamId()],
            topText = GetEntityName(),
            bottomText = $"{Mathf.CeilToInt(GetHealthPercentage() * 100f)}%",
            isActive = GetHealth() > 0
        };
    }
}
