using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;


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

        EnableInClassList("active", false);
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

    public void Update()
    {
        IdentifierData data = identifiable?.GetIdentifierData() ?? default;
        if (data.Equals(default(IdentifierData)) || data.targetTransform == null)
        {
            Debug.LogWarning($"Invalid identifier data or target transform is null. Removing indicator. Data: {data}");
            RemoveFromHierarchy();
            return;
        }
        if (objectCollider == null)
        {
            GameObject identifiableObject = data.targetTransform.GetComponentInParent<NetworkObject>().gameObject;
            Collider collider = identifiableObject.GetComponentInChildren<Collider>();
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

            Vector2 objectScreenSize = (objectScreenPosition - objectScreenCornerPosition) * 4f;
            Vector2 scale = new(
                Mathf.Clamp(objectScreenSize.x / layout.width, 0.2f, 5f),
                Mathf.Clamp(objectScreenSize.y / layout.height, 0.2f, 5f)
            );
            style.scale = scale;
            Vector2 textScale = new(Mathf.Max(1, 1 / scale.x), Mathf.Max(1, 1 / scale.y));
            topLabel.style.scale = textScale;
            bottomLabel.style.scale = textScale;
            style.translate = objectScreenPosition - new Vector2(layout.width, layout.height) * 0.5f;

            topLabel.text = data.topText;
            bottomLabel.text = data.bottomText;
        }
    }

    private bool VisibilityCheck(IdentifierData data, Vector2 objectScreenPosition)
    {
        if (!data.isActive) return false;
        if (!data.isAlwaysVisible)
        {
            // Check if the object is behind camera
            Vector3 directionToObject = (data.targetTransform.position - Camera.main.transform.position).normalized;
            float dot = Vector3.Dot(Camera.main.transform.forward, directionToObject);
            if (dot < 0) return false;

            // Check if the object is off-screen
            bool isOffScreen = objectScreenPosition.x < 0 || objectScreenPosition.x > Screen.width ||
                            objectScreenPosition.y < 0 || objectScreenPosition.y > Screen.height;
            if (isOffScreen) return false;

            // Perform a raycast to check if there are any obstacles between the camera and the object
            bool isVisible = false;
            Ray ray = new(Camera.main.transform.position, directionToObject);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                isVisible = hit.collider.transform == data.targetTransform || data.targetTransform.IsChildOf(hit.collider.gameObject.transform);
            }
            if (!isVisible) return false;
        }
        return true;
    }
}