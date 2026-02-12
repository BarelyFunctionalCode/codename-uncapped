using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dummy : Entity
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

        // if (IsLocalPlayer && EntityIdentifierManager.Instance != null) EntityIdentifierManager.Instance.RegisterEntity(this);
    }

    private void Start()
    {
        EntityIdentifierManager.Instance.RegisterEntity(this);    
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        material.color = Color.Lerp(Color.green, Color.red, 1.0f - GetHealthPercentage());
    }

    protected override void OnDie()
    {
        explodeParticleObj.transform.parent = null;
        explodeParticleObj.SetActive(true);
    }
}
