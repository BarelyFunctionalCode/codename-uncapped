using UnityEngine;

public class EntityIdentifierUI : MonoBehaviour
{

    private Entity entity;
    private Vector3 entityColliderExtents;
    private RectTransform identifierRect;

    private float hideTime = 1f;
    private float hideTimer = 0f;

    private void Start()
    {
        identifierRect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (entity)
        {
            // Convert speaker pos from world position to screen point
            Vector3 entityScreenCenterPos = Camera.main.WorldToScreenPoint(entity.transform.position);

            // Set the size of the identifier rect to match the size of the entity on screen
            Vector3 entityScreenCornerPos = Camera.main.WorldToScreenPoint(entity.transform.position + Camera.main.transform.right * entityColliderExtents.x + Camera.main.transform.up * entityColliderExtents.y);
            Vector3 entityScreenExtents = entityScreenCornerPos - entityScreenCenterPos;
            identifierRect.sizeDelta = new Vector2(Mathf.Abs(entityScreenExtents.x) * 2f / transform.parent.localScale.x, Mathf.Abs(entityScreenExtents.y) * 2f / transform.parent.localScale.y);
            Debug.Log($"Entity {entity.name} screen center pos: {entityScreenCenterPos}, corner pos: {entityScreenCornerPos}, extents: {entityScreenExtents}, rect size: {identifierRect.sizeDelta}");

            // // Define half width and half height for math later
            // float halfRectWidth = identifierRect.rect.width / 2f;
            // float halfRectHeight = identifierRect.rect.height / 2f;
            // float xpos = entityScreenCenterPos.x;
            // float ypos = entityScreenCenterPos.y;

            // // Clamp position in repsect to rect size so it stays on screen
            // xpos = Mathf.Clamp(xpos, 0 + halfRectWidth, Screen.width - halfRectWidth);
            // ypos = Mathf.Clamp(ypos, 0 + halfRectHeight, Screen.height - halfRectHeight);

            // entityScreenCenterPos = new Vector3(xpos, ypos, entityScreenCenterPos.z);

            // Set position
            identifierRect.position = entityScreenCenterPos;
        }
    }

    public void Initialize(Entity entity)
    {
        this.entity = entity;
        entityColliderExtents = entity.GetComponent<Collider>().bounds.extents + new Vector3(1f, 1f, 1f); // Add some padding to ensure the identifier fully encompasses the entity
        Debug.Log($"Initialized EntityIdentifierUI for {entity.name} with collider extents {entityColliderExtents}");
    }
}