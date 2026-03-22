using Unity.Netcode;
using UnityEngine;

public class Dummy : Entity, IIdentifiable
{
    static string[] namePartOneList = new string[] {"Red","Blue","Green","Yellow","Purple","Orange","Pink","Cyan","Magenta","Lime","Teal","Lavender","Brown","Beige","Maroon","Mint","Olive","Coral","Navy","Grey","White","Black","Silver","Gold","Bronze","Copper","Crimson","Indigo","Violet","Turquoise","Tan","Salmon"};
    static string[] namePartTwoList = new string[] {"Zebra","Octopus","Kangaroo","Frog","Penguin","Elephant","Cheetah","Dolphin","Giraffe","Hedgehog","Raccoon","Tiger","Panda","Lion","Koala","Bear","Fox","Whale","Snake","Monkey","Eagle","Shark","Camel","Alligator","Sloth","Platypus","Beetle","Crab","Peacock","Porcupine","Bat","Otter","Meerkat","Armadillo","Jellyfish","Hippopotamus","Wolf","Rhinoceros","Seal","Owl","Butterfly","Gorilla","Swan","Crocodile","Deer","Flamingo","Chameleon","Antelope","Squirrel","Walrus"}; 

    private Material material;

    [SerializeField] private GameObject explodeParticleObj;

    private void Awake()
    {
        material = GetComponent<MeshRenderer>().materials[0];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        _entityName = $"{namePartOneList[Random.Range(0, namePartOneList.Length)]} {namePartTwoList[Random.Range(0, namePartTwoList.Length)]}";
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        material.color = Color.Lerp(Color.green, Color.red, 1.0f - HealthPercentage);
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
            color = IdentifierManager.TempTeamColors[TeamId],
            topText = EntityName,
            bottomText = $"{Mathf.CeilToInt(HealthPercentage * 100f)}%",
            isActive = Health > 0
        };
    }
}
