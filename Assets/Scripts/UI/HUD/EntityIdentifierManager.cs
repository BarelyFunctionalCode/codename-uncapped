using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EntityIdentifierManager : MonoBehaviour
{
    public static EntityIdentifierManager Instance { get; private set; } = null;
    [SerializeField] private GameObject entityIdentifierPrefab;

    private Canvas parentCanvas;

    private List<EntityIdentifierUI> activeIdentifiers = new List<EntityIdentifierUI>();

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

    public void RegisterEntity(Entity entity)
    {
        GameObject identifierObj = Instantiate(entityIdentifierPrefab, parentCanvas.transform);
        EntityIdentifierUI identifierUI = identifierObj.GetComponent<EntityIdentifierUI>();
        identifierUI.Initialize(entity);
        identifierObj.transform.SetSiblingIndex(0); // Ensure the identifier is rendered behind other UI elements
        activeIdentifiers.Add(identifierUI);
    }

    public void UnregisterEntity(Entity entity)
    {
        EntityIdentifierUI identifierToRemove = activeIdentifiers.Find(identifier => identifier.entity == entity);
        if (identifierToRemove != null)
        {
            activeIdentifiers.Remove(identifierToRemove);
            Destroy(identifierToRemove.gameObject);
        }
    }
}
