using UnityEngine;

[RequireComponent(typeof(DummyState))]
[RequireComponent(typeof(Identification))]
[RequireComponent(typeof(Health))]
public class Dummy : Entity, IIdentifiable
{
    static string[] namePartOneList = new string[] {"Red","Blue","Green","Yellow","Purple","Orange","Pink","Cyan","Magenta","Lime","Teal","Lavender","Brown","Beige","Maroon","Mint","Olive","Coral","Navy","Grey","White","Black","Silver","Gold","Bronze","Copper","Crimson","Indigo","Violet","Turquoise","Tan","Salmon"};
    static string[] namePartTwoList = new string[] {"Zebra","Octopus","Kangaroo","Frog","Penguin","Elephant","Cheetah","Dolphin","Giraffe","Hedgehog","Raccoon","Tiger","Panda","Lion","Koala","Bear","Fox","Whale","Snake","Monkey","Eagle","Shark","Camel","Alligator","Sloth","Platypus","Beetle","Crab","Peacock","Porcupine","Bat","Otter","Meerkat","Armadillo","Jellyfish","Hippopotamus","Wolf","Rhinoceros","Seal","Owl","Butterfly","Gorilla","Swan","Crocodile","Deer","Flamingo","Chameleon","Antelope","Squirrel","Walrus"}; 

    private Material material;


    private void Awake()
    {
        material = GetComponent<MeshRenderer>().materials[0];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;
        identification.SetEntityName($"{namePartOneList[Random.Range(0, namePartOneList.Length)]} {namePartTwoList[Random.Range(0, namePartTwoList.Length)]}");
    }

    protected virtual void Update()
    {
        if (!baseEntityInitialized) return;
        
        float healthPercentage = health.HealthPercentage;
        material.color = Color.Lerp(Color.green, Color.red, 1.0f - healthPercentage);
    }

    public IdentifierData GetIdentifierData()
    {
        if (!baseEntityInitialized) return default;

        ulong TeamId = identification.FetchTeamId();
        string EntityName = identification.FetchEntityName();

        float healthPercentage = health.HealthPercentage;
        float healthValue = health.CurrentHealth;

        return new IdentifierData
        {
            color = IdentifierManager.TempTeamColors[TeamId],
            topText = EntityName,
            bottomText = $"{Mathf.CeilToInt(healthPercentage * 100f)}%",
            isActive = healthValue > 0,
            targetTransform = transform,
            isAlwaysVisible = false
        };
    }
}
