using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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



[UxmlElement(libraryPath = "FFIndicator")]
public partial class FFIndicatorManager : VisualElement
{
    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
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

        FFIndicator newIndicator = (FFIndicator)UIManager.Spawn("UI/HUD/FFIndicator/FFIndicator", this);
        newIndicator.Initialize(identifiable);
    }
}

[UxmlElement(libraryPath = "FFIndicator")]
public partial class FFIndicator : VectorFillShape
{
    private Label topLabel;
    private Label bottomLabel;

    private Color fillColor = new(1, 0, 0, 0.7f);
    private Color textColor = new(1, 0, 0, 0.95f);

    private IIdentifiable identifiable;
    private Collider objectCollider = null;

    public FFIndicator()
    {
        topLabel = new Label
        {
            name = "top-label",
            text = "TOP TEXT"
        };
        topLabel.style.color = textColor;
        Add(topLabel);

        bottomLabel = new Label
        {
            name = "bottom-label",
            text = "BOTTOM TEXT"
        };
        bottomLabel.style.color = textColor;
        Add(bottomLabel);
    }

    public void Initialize(IIdentifiable identifiable)
    {
        this.identifiable = identifiable;
        IdentifierData data = identifiable.GetIdentifierData();

        topLabel.text = data.topText;
        bottomLabel.text = data.bottomText;
        fillColor = data.color;
        textColor = data.color;
        textColor.a = 0.95f;

        topLabel.style.color = textColor;
        bottomLabel.style.color = textColor;

        style.top = -layout.height * 0.5f;
        style.left = -layout.width * 0.5f;

        schedule.Execute(Update).Every(20);
    }

    protected override void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        List<Vector2> points = new()
        {
            new Vector2(0, layout.height * 0.8f),
            new Vector2(-3, layout.height * 0.6f),
            new Vector2(-3, -1),
            new Vector2(-1, -3),
            new Vector2(layout.width * 0.6f, -3),
            new Vector2(layout.width * 0.8f, 0),
            new Vector2(5, 0),
            new Vector2(0, 5),
        };
        BuildFillShape(mgc, points, fillColor);

        points = new()
        {
            new Vector2(layout.width, layout.height * 0.2f),
            new Vector2(layout.width + 3, layout.height * 0.4f),
            new Vector2(layout.width + 3, layout.height + 1),
            new Vector2(layout.width + 1, layout.height + 3),
            new Vector2(layout.width * 0.4f, layout.height + 3),
            new Vector2(layout.width * 0.2f, layout.height),
            new Vector2(layout.width - 5, layout.height),
            new Vector2(layout.width, layout.height - 5),
        };
        BuildFillShape(mgc, points, fillColor);
    }

    private void Update()
    {
        IdentifierData data = identifiable?.GetIdentifierData() ?? default;
        if (data.Equals(default(IdentifierData)) || data.targetTransform == null)
        {
            Debug.LogWarning($"Invalid identifier data or target transform is null. Removing indicator. Data: {data}");
            schedule.Execute(Update).Pause();
            RemoveFromHierarchy();
            return;
        }
        if (objectCollider == null)
        {
            GameObject identifiableObject = data.targetTransform.GetComponentInParent<NetworkObject>().gameObject;
            Collider collider = identifiableObject.GetComponentInParent<Collider>();
            if (collider != null) objectCollider = collider;
            else return;
        }   

        Vector3 objectBounds = objectCollider.bounds.extents;
        Vector2 objectScreenPosition = RuntimePanelUtils.CameraTransformWorldToPanel(panel, objectCollider.bounds.center, Camera.main);
        bool isVisible = VisibilityCheck(data, objectScreenPosition);
        
        EnableInClassList("active", isVisible);
        if (isVisible) 
        {
            Vector2 objectScreenCornerPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                panel,
                objectCollider.bounds.center + Camera.main.transform.up * objectBounds.y - Camera.main.transform.right * objectBounds.x,
                Camera.main
            );

            Vector2 objectScreenSize = (objectScreenPosition - objectScreenCornerPosition) * 2.5f;
            Vector2 scale = new(
                Mathf.Clamp(objectScreenSize.x / layout.width, 0.2f, 5f),
                Mathf.Clamp(objectScreenSize.y / layout.height, 0.2f, 5f)
            );
            // Debug.DrawLine(objectCollider.bounds.center, objectCollider.bounds.center + Camera.main.transform.up * objectBounds.y - Camera.main.transform.right * objectBounds.x, Color.red);
            Debug.Log($"Object Screen Position: {objectScreenPosition}, Object Screen Size: {objectScreenSize}, Scale: {scale}");
            style.scale = scale;
            style.translate = objectScreenPosition - new Vector2(layout.width, layout.height) * 0.5f;
        }
    }

    private bool VisibilityCheck(IdentifierData data, Vector2 objectScreenPosition)
    {
        if (data.isActive && !data.isAlwaysVisible)
        {
            // Check if the object is off-screen
            bool isOffScreen = objectScreenPosition.x < 0 || objectScreenPosition.x > Screen.width ||
                            objectScreenPosition.y < 0 || objectScreenPosition.y > Screen.height;
            if (isOffScreen) return false;

            // Check if the object is behind an obstacle
            Vector3 directionToObject = data.targetTransform.position - Camera.main.transform.position;
            float dot = Vector3.Dot(Camera.main.transform.forward, directionToObject.normalized);
            if (dot < 0) return false; // Object is behind the camera
        }
        return true;
    }
}