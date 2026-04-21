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

    [SerializeField] private Transform identifierContainer;
    [SerializeField] private Transform offscreenIdentifierRadarContainer;

    private List<IdentifierUI> activeIdentifiers = new();

    private float cleanupInterval = 5f;
    private float cleanupTimer = 0f;

    private bool isInitialized = false;

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
    }

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
        foreach (var identifier in activeIdentifiers)
        {
            if (identifier != null) Destroy(identifier.gameObject);
        }
        activeIdentifiers.Clear();
        SpawnManager.Instance.Subscribe(RegisterIdentifier);
    }

    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;
        if (SpawnManager.Instance != null) SpawnManager.Instance.Unsubscribe(RegisterIdentifier);
    }

    public void RegisterIdentifier(GameObject obj)
    {
        if (obj == null) return;
        if (Player.Instance && Player.Instance.Character &&
            Player.Instance.Character.localCharacterType && 
            obj.transform.IsChildOf(Player.Instance.Character.transform)) return;
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
