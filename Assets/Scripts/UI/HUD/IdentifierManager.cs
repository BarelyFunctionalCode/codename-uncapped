using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct IdentifierData
{
    public Color color;
    public string topText;
    public string bottomText;
    public bool isActive;
}

public interface IIdentifiable
{
    public IdentifierData GetIdentifierData();
}


public class IdentifierManager : MonoBehaviour
{
    public static Color[] TempTeamColors = new Color[] { Color.blue, Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta };
    [SerializeField] private GameObject identifierPrefab;
    private Canvas parentCanvas;

    [SerializeField] private Transform identifierContainer;
    [SerializeField] private Transform offscreenIdentifierRadarContainer;

    private List<IdentifierUI> activeIdentifiers = new();

    private float cleanupInterval = 5f;
    private float cleanupTimer = 0f;

    private void Awake()
    {
        parentCanvas = GetComponent<Canvas>();

        SceneManager.activeSceneChanged += (_, _) => RegisterSweep();
        SpawnManager.objectSpawnedEvent.AddListener(obj =>
        {
            if (obj.TryGetComponent<IIdentifiable>(out var identifiable)) RegisterIdentifier(identifiable);
        });
    }

    private void Update()
    {
        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= cleanupInterval)
        {
            for (int i = activeIdentifiers.Count - 1; i >= 0; i--)
            {
                if (activeIdentifiers[i] == null || activeIdentifiers[i].identifiableObject == null)
                {
                    if (activeIdentifiers[i] != null) Destroy(activeIdentifiers[i].gameObject);
                    activeIdentifiers.RemoveAt(i);
                }
            }
            cleanupTimer = 0f;
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= (_, _) => RegisterSweep(); // TODO: This is fucked
        SpawnManager.objectSpawnedEvent.RemoveListener(obj =>
        {
            if (obj.TryGetComponent<IIdentifiable>(out var identifiable)) RegisterIdentifier(identifiable);
        });
    }


    private void RegisterSweep()
    {
        GameObject[] existingIdentifiables = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in existingIdentifiables)
        {
            // Check if obj is in active scene)
            if (obj.scene != SceneManager.GetActiveScene()) continue;
            if (obj.TryGetComponent<IIdentifiable>(out var identifiable)) RegisterIdentifier(identifiable);
        }
    }

    public void RegisterIdentifier(IIdentifiable identifiable)
    {
        if (activeIdentifiers.Exists(identifier => identifier.identifiable == identifiable)) return;

        GameObject identifierObj = Instantiate(identifierPrefab, identifierContainer);
        IdentifierUI identifierUI = identifierObj.GetComponent<IdentifierUI>();
        identifierUI.Initialize(identifiable, offscreenIdentifierRadarContainer);
        activeIdentifiers.Add(identifierUI);
    }

    public void UnregisterIdentifier(IIdentifiable identifiable)
    {
        IdentifierUI identifierToRemove = activeIdentifiers.Find(identifier => identifier.identifiable == identifiable);
        if (identifierToRemove != null)
        {
            activeIdentifiers.Remove(identifierToRemove);
            Destroy(identifierToRemove.gameObject);
        }
    }
}
