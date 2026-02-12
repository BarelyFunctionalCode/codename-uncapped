using UnityEngine;

public class EntityIdentifierManager : MonoBehaviour
{
    public static EntityIdentifierManager Instance { get; private set; } = null;
    [SerializeField] private GameObject entityIdentifierPrefab;

    private Canvas parentCanvas;

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
    }
}
