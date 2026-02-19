using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct IdentifierData
{
    public Color color;
    public string topText;
    public string bottomText;
}

public interface IIdentifiable
{
    public IdentifierData GetIdentifierData();
}


public class IdentifierManager : MonoBehaviour
{
    public static Color[] TempTeamColors = new Color[] { Color.blue, Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta };
    public static IdentifierManager Instance { get; private set; } = null;
    [SerializeField] private GameObject identifierPrefab;
    private Canvas parentCanvas;

    private List<IdentifierUI> activeIdentifiers = new();

    private float cleanupInterval = 5f;
    private float cleanupTimer = 0f;

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

    private void Awake()
    {
        parentCanvas = GetComponent<Canvas>();

        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        RegisterSweep();
        SceneManager.activeSceneChanged += (_, _) => RegisterSweep();
        SpawnManager.objectSpawnedEvent.AddListener(obj =>
        {
            if (obj.TryGetComponent<IIdentifiable>(out var identifiable)) RegisterEntity(identifiable);
        });
    }

    private void RegisterSweep()
    {
        GameObject[] existingIdentifiables = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in existingIdentifiables)
        {
            if (obj.TryGetComponent<IIdentifiable>(out var identifiable)) RegisterEntity(identifiable);
        }
    }

    public void RegisterEntity(IIdentifiable identifiable)
    {
        if (activeIdentifiers.Exists(identifier => identifier.identifiable == identifiable)) return;

        GameObject identifierObj = Instantiate(identifierPrefab, parentCanvas.transform);
        IdentifierUI identifierUI = identifierObj.GetComponent<IdentifierUI>();
        identifierUI.Initialize(identifiable);
        identifierObj.transform.SetSiblingIndex(0); // Ensure the identifier is rendered behind other UI elements
        activeIdentifiers.Add(identifierUI);
    }

    public void UnregisterEntity(IIdentifiable identifiable)
    {
        IdentifierUI identifierToRemove = activeIdentifiers.Find(identifier => identifier.identifiable == identifiable);
        if (identifierToRemove != null)
        {
            activeIdentifiers.Remove(identifierToRemove);
            Destroy(identifierToRemove.gameObject);
        }
    }
}
