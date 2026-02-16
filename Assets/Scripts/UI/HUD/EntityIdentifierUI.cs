using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntityIdentifierUI : MonoBehaviour
{

    private static Color[] tempTeamColors = new Color[] { Color.blue, Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta };

    [SerializeField] private TMP_Text identifierText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image mainIndicator;
    [SerializeField] private Image offscreenIndicator;
    private float offscreenIndicatorOriginalAlpha;
    private float mainIndicatorOriginalAlpha;

    public Entity entity;
    private Collider entityCollider;
    private Vector3 entityColliderExtents;
    private RectTransform identifierRect;

    float detectionMaxDistanceRange = 1000f;
    float detectionMaxDistanceScreenRadius = 10f;
    float detectionMinDistanceRange = 0f;
    float detectionMinDistanceScreenRadius = 200f;

    private float offscreenHideTime = 5f;
    private float offscreenHideTimer = 0f;

    private float mainInfoShowTime = 2f;
    private float mainInfoShowTimer = 0f;

    private bool isHidden = true;

    private void Awake()
    {
        identifierRect = GetComponent<RectTransform>();
        offscreenIndicatorOriginalAlpha = offscreenIndicator.color.a;
        mainIndicatorOriginalAlpha = mainIndicator.color.a;

        mainIndicator.enabled = false;
        offscreenIndicator.enabled = false;
        identifierText.enabled = false;
        healthText.enabled = false;
    }

    private void Update()
    {
        if (entity == null) return;

        Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 entityScreenCenterPos = Camera.main.WorldToScreenPoint(entity.transform.position);
        Vector3 entityDirectionToCamera = (Camera.main.transform.position - entity.transform.position).normalized;

        float distanceToEntity = Vector3.Distance(Camera.main.transform.position, entity.transform.position);
        if (isHidden)
        {
            // Check if the entity is in front of the camera
            if (Vector3.Dot(entityDirectionToCamera, Camera.main.transform.forward) > 0)
                return;

            // Render UI only if the entity crosses near the reticle.
            float detectionRadius = CalculateDetectionRadius(distanceToEntity);
            if (Mathf.Abs(screenCenter.x - entityScreenCenterPos.x) > detectionRadius || Mathf.Abs(screenCenter.y - entityScreenCenterPos.y) > detectionRadius)
                return;

            // Check if the entity is visible to the camera
            Ray ray = new(Camera.main.transform.position, entity.transform.position - Camera.main.transform.position);
            if (!entityCollider.Raycast(ray, out RaycastHit _, Mathf.Infinity))
                return;

            isHidden = false;
        }

        UpdateMainIndicatorSize(entityDirectionToCamera, entityScreenCenterPos);

        // Check if the entity is on screen (considering the size of the identifier)
        bool isOnScreen = !(entityScreenCenterPos.x + identifierRect.sizeDelta.x < 0 || entityScreenCenterPos.x - identifierRect.sizeDelta.x > Screen.width ||
                            entityScreenCenterPos.y + identifierRect.sizeDelta.y < 0 || entityScreenCenterPos.y - identifierRect.sizeDelta.y > Screen.height);


        mainIndicator.enabled = isOnScreen;
        identifierText.enabled = isOnScreen;
        healthText.enabled = isOnScreen;
        offscreenIndicator.enabled = !isOnScreen;

        Vector3 rectPosition = entityScreenCenterPos;
        Color teamColor = tempTeamColors[entity.GetTeamId()];
        if (!isOnScreen)
        {
            // Off Screen
            // Position the off-screen indicator at the edge of the screen in the direction of the entity
            float indicatorDistanceFromCenter = 210f;
            Vector3 directionToEntity = (entityScreenCenterPos - screenCenter).normalized;
            float angle = Mathf.Atan2(directionToEntity.y, directionToEntity.x) * Mathf.Rad2Deg - 90f;
            offscreenIndicator.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 desiredPosition = screenCenter + directionToEntity * indicatorDistanceFromCenter;
            rectPosition = Vector3.Lerp(identifierRect.anchoredPosition, desiredPosition, 20f * Time.deltaTime);

            // Handle off-screen indicator fading and hiding
            FadeOutOffscreenIndicator(teamColor);
        }
        else
        {
            // On Screen
            // Fade in main info after a delay
            FadeInMainInfo(teamColor);

            // Update main info
            identifierText.text = entity.GetIdentifier();
            healthText.text = $"{Mathf.CeilToInt(entity.GetHealthPercentage() * 100f)}%";
                
            isHidden = false;
            offscreenHideTimer = 0f;

            if (offscreenIndicator.enabled) ResetOffscreenIndicator(teamColor);
        }

        // Set position
        identifierRect.anchoredPosition = rectPosition;
    }

    private float CalculateDetectionRadius(float distanceToEntity)
    {
        float t = Mathf.InverseLerp(detectionMinDistanceRange, detectionMaxDistanceRange, distanceToEntity);
        float screenRadius = Mathf.Lerp(detectionMinDistanceScreenRadius, detectionMaxDistanceScreenRadius, t);
        return screenRadius;
    }

    private void UpdateMainIndicatorSize(Vector3 entityDirectionToCamera, Vector3 entityScreenCenterPos)
    {
        // Gets the outer corner position of the entity on screen
        Vector3 entityToCameraSideVector = Vector3.Cross(entityDirectionToCamera, Camera.main.transform.up).normalized;
        Vector3 entityScreenCornerPos = Camera.main.WorldToScreenPoint(
            entity.transform.position +
            entityColliderExtents.y * Camera.main.transform.up +
            entityColliderExtents.x * Mathf.Sign(entityScreenCenterPos.x - Screen.width / 2f) * entityToCameraSideVector);

        // Calculate the size of the identifier based on the distance between the center and corner positions
        Vector3 entityScreenExtents = entityScreenCornerPos - entityScreenCenterPos;
        Vector2 desiredSize = new(Mathf.Max(Mathf.Abs(entityScreenExtents.x) * 2f, 25f), Mathf.Max(Mathf.Abs(entityScreenExtents.y) * 2f, 25f));
        identifierRect.sizeDelta = Vector2.Lerp(identifierRect.sizeDelta, desiredSize, 5f * Time.deltaTime);
    }

    private void FadeInMainInfo(Color teamColor)
    {
        if (mainInfoShowTimer < 0f) return;
        mainInfoShowTimer += Time.deltaTime;
        if (mainInfoShowTimer >= mainInfoShowTime)
        {
            identifierText.color = new Color(teamColor.r, teamColor.g, teamColor.b, Mathf.Lerp(identifierText.color.a, 1f, 5f * Time.deltaTime));
            healthText.color = new Color(teamColor.r, teamColor.g, teamColor.b, Mathf.Lerp(healthText.color.a, 1f, 5f * Time.deltaTime));
            if (identifierText.color.a >= 0.99f && healthText.color.a >= 0.99f)
            {
                identifierText.color = new Color(teamColor.r, teamColor.g, teamColor.b, 1f);
                healthText.color = new Color(teamColor.r, teamColor.g, teamColor.b, 1f);
                mainInfoShowTimer = -1f;
            }
        }
    }

    private void FadeOutOffscreenIndicator(Color teamColor)
    {
        offscreenHideTimer += Time.deltaTime;
        if (offscreenHideTimer >= offscreenHideTime)
        {
            offscreenIndicator.color = new Color(teamColor.r, teamColor.g, teamColor.b, Mathf.Lerp(offscreenIndicator.color.a, 0f, 5f * Time.deltaTime));
            if (offscreenIndicator.color.a <= 0.01f)
            {
                offscreenIndicator.enabled = false;
                offscreenIndicator.color = new Color(teamColor.r, teamColor.g, teamColor.b, offscreenIndicatorOriginalAlpha);
                isHidden = true;
                offscreenHideTimer = 0f;
                ResetMainIndicator(teamColor);
            }
        }
    }

    private void ResetMainIndicator(Color teamColor)
    {
        mainIndicator.enabled = false;
        identifierText.enabled = false;
        healthText.enabled = false;
        mainIndicator.color = new Color(teamColor.r, teamColor.g, teamColor.b, mainIndicatorOriginalAlpha);
        identifierText.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0f);
        healthText.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0f);
        identifierRect.sizeDelta = new Vector2(Screen.width * 2f, Screen.height * 2f);
        mainInfoShowTimer = 0f;
    }

    private void ResetOffscreenIndicator(Color teamColor)
    {
        offscreenIndicator.enabled = false;
        offscreenIndicator.color = new Color(teamColor.r, teamColor.g, teamColor.b, offscreenIndicatorOriginalAlpha);
    }

    public void Initialize(Entity entity)
    {
        this.entity = entity;
        entityCollider = entity.GetComponent<Collider>();
        entityColliderExtents = entityCollider.bounds.extents + new Vector3(1f, 1f, 1f); // Add some padding to ensure the identifier fully encompasses the entity
        Color teamColor = tempTeamColors[entity.GetTeamId()];
        ResetMainIndicator(teamColor);
        ResetOffscreenIndicator(teamColor);

        Debug.Log($"Initialized EntityIdentifierUI for {entity.name} with collider extents {entityColliderExtents}");
    }
}