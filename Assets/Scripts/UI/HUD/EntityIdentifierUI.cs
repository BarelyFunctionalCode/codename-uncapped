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
    private Renderer[] entityRenderers;
    private Collider entityCollider;
    private Vector3 entityColliderExtents;
    private RectTransform identifierRect;

    float detectionMaxDistanceRange = 1000f;
    float detectionMaxDistanceScreenRadius = 10f;
    float detectionMinDistanceRange = 0f;
    float detectionMinDistanceScreenRadius = 200f;

    private float offscreenHideTime = 5f;
    private float offscreenHideTimer = 0f;

    private float mainInfoShowTime = 0.3f;
    private float mainInfoShowTimer = 0f;

    private float entityVisibilityCheckInterval = 0.5f;
    private float entityVisibilityCheckTimer = 0f;
    private bool isVisible = false;

    private float fadeSpeed = 0.2f;
    private float mainIndicatorSizeLerpSpeed = 0.2f;

    private bool isEnabled = false;


    #region Lifecycle
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

        // Basic screen and entity information
        Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 entityScreenCenterPos = Camera.main.WorldToScreenPoint(entity.transform.position);
        Vector3 entityDirectionToCamera = (Camera.main.transform.position - entity.transform.position).normalized;
        float distanceToEntity = Vector3.Distance(Camera.main.transform.position, entity.transform.position);

        if (!isEnabled)
        {
            // Check if the entity is in front of the camera
            if (Vector3.Dot(entityDirectionToCamera, Camera.main.transform.forward) > 0)
                return;

            // Render UI only if the entity crosses near the reticle.
            float detectionRadius = CalculateDetectionRadius(distanceToEntity);
            if (Mathf.Abs(screenCenter.x - entityScreenCenterPos.x) > detectionRadius || Mathf.Abs(screenCenter.y - entityScreenCenterPos.y) > detectionRadius)
                return;

            // Check if the entity is visible to the camera
            if (!EntityVisibilityCheck())
                return;

            isEnabled = true;
        }

        // Check if the entity is on screen
        bool isOnScreen = !(entityScreenCenterPos.x < 0 || entityScreenCenterPos.x > Screen.width ||
                            entityScreenCenterPos.y < 0 || entityScreenCenterPos.y > Screen.height) &&
                            Vector3.Dot(entityDirectionToCamera, Camera.main.transform.forward) < 0;

        // Toggle various elements based on on-screen status
        mainIndicator.enabled = isOnScreen;
        identifierText.enabled = isOnScreen;
        healthText.enabled = isOnScreen;
        offscreenIndicator.enabled = !isOnScreen;

        Vector3 rectPosition = entityScreenCenterPos;
        Color teamColor = tempTeamColors[entity.GetTeamId()];
        if (!isOnScreen)
        {
            // Off Screen
            rectPosition = CalculateOffscreenIndicatorPosition(entityScreenCenterPos, screenCenter);
            
            // Handle off-screen indicator fading and hiding
            FadeOutOffscreenIndicator(teamColor);
        }
        else
        {
            // On Screen
            // Check if the entity is visible at a given interval
            entityVisibilityCheckTimer += Time.deltaTime;
            if (entityVisibilityCheckTimer >= entityVisibilityCheckInterval)
            {
                isVisible = EntityVisibilityCheck();
                entityVisibilityCheckTimer = 0f;
            }
            if (isVisible)
            {
                identifierRect.sizeDelta = CalculateMainIndicatorSize(entityDirectionToCamera, entityScreenCenterPos);
                FadeInMainIndicator(teamColor);
            }
            else FadeOutMainIndicator(teamColor);

            // Update main indicator info
            identifierText.text = entity.GetIdentifier();
            healthText.text = $"{Mathf.CeilToInt(entity.GetHealthPercentage() * 100f)}%";
            
            // Reset off-screen indicator if it was previously shown
            if (offscreenIndicator.enabled) ResetOffscreenIndicator(teamColor);
        }

        // Set position
        identifierRect.anchoredPosition = rectPosition;
    }
    #endregion


    #region Initialization
    public void Initialize(Entity entity)
    {
        this.entity = entity;
        entityRenderers = entity.GetComponentsInChildren<Renderer>();
        entityCollider = entity.GetComponent<Collider>();
        if (entityCollider == null) entityCollider = entity.GetComponentInChildren<Collider>();
        entityColliderExtents = entityCollider.bounds.extents + new Vector3(1f, 1f, 1f); // Add some padding to ensure the identifier fully encompasses the entity
        Color teamColor = tempTeamColors[entity.GetTeamId()];
        FullReset(teamColor);

        Debug.Log($"Initialized EntityIdentifierUI for {entity.name} with collider extents {entityColliderExtents}");
    }
    #endregion
    

    #region Main Indicator
    private Vector2 CalculateMainIndicatorSize(Vector3 entityDirectionToCamera, Vector3 entityScreenCenterPos)
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
        return Vector2.Lerp(identifierRect.sizeDelta, desiredSize, mainIndicatorSizeLerpSpeed);
    }

    private void FadeInMainIndicator(Color teamColor)
    {
        if (mainInfoShowTimer >= mainInfoShowTime)
        {
            FadeElementAlpha(identifierText, teamColor, 1f);
            FadeElementAlpha(healthText, teamColor, 1f);
        }
        else mainInfoShowTimer += Time.deltaTime;
        
        FadeElementAlpha(mainIndicator, teamColor, mainIndicatorOriginalAlpha);
    }

    private void FadeOutMainIndicator(Color teamColor)
    {
        bool done = FadeElementAlpha(mainIndicator, teamColor, 0f);
        done = FadeElementAlpha(identifierText, teamColor, 0f) && done;
        done = FadeElementAlpha(healthText, teamColor, 0f) && done;
        if (done) FullReset(teamColor);
    }

    private void ResetMainIndicator(Color teamColor)
    {
        mainIndicator.enabled = false;
        identifierText.enabled = false;
        healthText.enabled = false;
        SetElementAlpha(mainIndicator, teamColor, 0f);
        SetElementAlpha(identifierText, teamColor, 0f);
        SetElementAlpha(healthText, teamColor, 0f);
        identifierRect.sizeDelta = new Vector2(Screen.width * 2f, Screen.height * 2f);
        mainInfoShowTimer = 0f;
    }
    #endregion


    #region Offscreen Indicator
    private Vector3 CalculateOffscreenIndicatorPosition(Vector3 entityScreenCenterPos, Vector3 screenCenter)
    {
        // Position the off-screen indicator at the edge of the screen in the direction of the entity
        float indicatorDistanceFromCenter = 210f;
        Vector3 directionToEntity = Camera.main.transform.InverseTransformDirection(
            Vector3.ProjectOnPlane(
                entity.transform.position - Camera.main.transform.position,
                Camera.main.transform.forward
            ).normalized
        );
        float angle = Mathf.Atan2(directionToEntity.y, directionToEntity.x) * Mathf.Rad2Deg - 90f;
        offscreenIndicator.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        Vector3 desiredPosition = screenCenter + directionToEntity * indicatorDistanceFromCenter;
        return Vector3.Lerp(identifierRect.anchoredPosition, desiredPosition, 0.3f);
    }

    private void FadeOutOffscreenIndicator(Color teamColor)
    {
        offscreenHideTimer += Time.deltaTime;
        if (offscreenHideTimer >= offscreenHideTime)
        {
            bool done = FadeElementAlpha(offscreenIndicator, teamColor, 0f);
            if (done) FullReset(teamColor);
        }
    }

    private void ResetOffscreenIndicator(Color teamColor)
    {
        offscreenIndicator.enabled = false;
        SetElementAlpha(offscreenIndicator, teamColor, offscreenIndicatorOriginalAlpha);
        offscreenHideTimer = 0f;
    }
    #endregion


    #region Utility
    private bool EntityVisibilityCheck()
    {
        bool isVisible = false;

        // Renderer.isVisible can be used to check if the entity is completely off screen, but not reliable to confirm visibility,
        // since it will also be true if only a small part or even the shadow is visible.
        for (int i = 0; i < entityRenderers.Length; i++)
        {
            if (entityRenderers[i].isVisible)
            {
                isVisible = true;
                break;
            }
        }
        if (!isVisible) return false;

        Ray ray = new(Camera.main.transform.position, (entity.transform.position - Camera.main.transform.position).normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            isVisible = hit.collider == entityCollider;
        }
        return isVisible;
    }
    
    private float CalculateDetectionRadius(float distanceToEntity)
    {
        float t = Mathf.InverseLerp(detectionMinDistanceRange, detectionMaxDistanceRange, distanceToEntity);
        float screenRadius = Mathf.Lerp(detectionMinDistanceScreenRadius, detectionMaxDistanceScreenRadius, t);
        return screenRadius;
    }

    private bool FadeElementAlpha(Graphic graphic, Color teamColor, float targetAlpha)
    {
        if (graphic.color.a == targetAlpha) return true;

        graphic.color = new Color(teamColor.r, teamColor.g, teamColor.b, Mathf.Lerp(graphic.color.a, targetAlpha, fadeSpeed));
        if (Mathf.Abs(graphic.color.a - targetAlpha) <= 0.01f)
        {
            SetElementAlpha(graphic, teamColor, targetAlpha);
        }
        return false;
    }

    private void SetElementAlpha(Graphic graphic, Color teamColor, float targetAlpha)
    {
        graphic.color = new Color(teamColor.r, teamColor.g, teamColor.b, targetAlpha);
    }

    private void FullReset(Color teamColor)
    {
        ResetMainIndicator(teamColor);
        ResetOffscreenIndicator(teamColor);
        isEnabled = false;
    }
    #endregion
}