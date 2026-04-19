using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct IdentifierData
{
    public Color color;
    public string topText;
    public string bottomText;
    public bool isActive;
    public Transform targetTransform;
    public bool isAlwaysVisible;
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

    private float sweepDelay = 1f;
    private float sweepDelayTimer = -1f;

    private bool isInitialized = false;

    private void Awake()
    {
        parentCanvas = GetComponent<Canvas>();
        SceneManager.activeSceneChanged += (_, _) => sweepDelayTimer = 0f;
    }

    private void Update()
    {
        cleanupTimer += Time.deltaTime;
        if (cleanupTimer >= cleanupInterval)
        {
            for (int i = activeIdentifiers.Count - 1; i >= 0; i--)
            {
                if (activeIdentifiers[i] == null || activeIdentifiers[i].identifiable == null)
                {
                    if (activeIdentifiers[i] != null) Destroy(activeIdentifiers[i].gameObject);
                    activeIdentifiers.RemoveAt(i);
                }
            }
            cleanupTimer = 0f;
        }

        if (sweepDelayTimer >= 0f)
        {
            sweepDelayTimer += Time.deltaTime;
            if (sweepDelayTimer >= sweepDelay)
            {
                sweepDelayTimer = -1f;
                RegisterSweep();
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= (_, _) => sweepDelayTimer = 0f;
    }

    public void Initialize()
    {
        if (isInitialized) return;
        GameManager.Instance.OnClientConnectedEvent.AddListener((_) => sweepDelayTimer = 0f);
        SpawnManager.Instance.objectSpawnedEvent.AddListener(RegisterIdentifier);
        isInitialized = true;
    }

    public void Deinitialize()
    {
        if (!isInitialized) return;
        GameManager.Instance.OnClientConnectedEvent.RemoveListener((_) => sweepDelayTimer = 0f);
        if (SpawnManager.Instance != null) SpawnManager.Instance.objectSpawnedEvent.RemoveListener(RegisterIdentifier);
        foreach (var identifier in activeIdentifiers)
        {
            if (identifier != null) Destroy(identifier.gameObject);
        }
        activeIdentifiers.Clear();
        isInitialized = false;
    }


    private void RegisterSweep()
    {
        NetworkObject[] existingIdentifiables = FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NetworkObject obj in existingIdentifiables)
        {
            // Check if obj is in active scene and is spawned
            if (!obj.IsSpawned) continue;
            if (obj.gameObject.scene != SceneManager.GetActiveScene()) continue;
            RegisterIdentifier(obj.gameObject);
        }
    }

    public void RegisterIdentifier(GameObject obj)
    {
        if (!Player.Instance.Character) return;
        if (obj == Player.Instance.Character.localCharacterType.gameObject) return;
        if (!obj.TryGetComponent<IIdentifiable>(out var identifiable)) return;
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
