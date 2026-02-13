using UnityEngine;

public class Dummy : Entity
{
    private Material material;

    [SerializeField] private GameObject explodeParticleObj;

    private bool setupDone = false;

    private void Awake()
    {
        material = GetComponent<MeshRenderer>().materials[0];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Setup()
    {
        if (setupDone) return;

        if (EntityIdentifierManager.Instance == null) return;
        EntityIdentifierManager.Instance.RegisterEntity(this);

        setupDone = true;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        Setup();

        material.color = Color.Lerp(Color.green, Color.red, 1.0f - GetHealthPercentage());
    }

    protected override void OnDie()
    {
        explodeParticleObj.transform.parent = null;
        explodeParticleObj.SetActive(true);
    }
}
