using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IdentifierUI : MonoBehaviour
{
    [SerializeField] private TMP_Text identifierTopText;
    [SerializeField] private TMP_Text identifierBottomText;
    [SerializeField] private Image mainIndicator;
    [SerializeField] private Image offscreenIndicator;
    private RectTransform offscreenIndicatorContainer;
    private float offscreenIndicatorOriginalAlpha;
    private float mainIndicatorOriginalAlpha;

    public IIdentifiable identifiable;
    public GameObject identifiableObject;
    private Renderer[] objectRenderers;
    private Collider objectCollider;
    private Vector3 objectColliderExtents;
    private RectTransform identifierRect;
    private RectTransform offscreenIndicatorRect;

    float detectionMaxDistanceRange = 1000f;
    float detectionMaxDistanceScreenRadius = 10f;
    float detectionMinDistanceRange = 0f;
    float detectionMinDistanceScreenRadius = 200f;

    private float offscreenHideTime = 5f;
    private float offscreenHideTimer = 0f;

    private float mainInfoShowTime = 0.3f;
    private float mainInfoShowTimer = 0f;

    private float ObjectVisibilityCheckInterval = 0.5f;
    private float ObjectVisibilityCheckTimer = 0f;
    private bool isVisible = false;

    private float fadeSpeed = 0.2f;
    private float mainIndicatorSizeLerpSpeed = 0.2f;

    private bool isEnabled = false;

    private bool isInitialized = false;

    Canvas parentCanvas;


    #region Lifecycle
    private void Awake()
    {
        identifierRect = GetComponent<RectTransform>();
        offscreenIndicatorRect = offscreenIndicator.GetComponent<RectTransform>();
        offscreenIndicatorOriginalAlpha = offscreenIndicator.color.a;
        mainIndicatorOriginalAlpha = mainIndicator.color.a;

        mainIndicator.enabled = false;
        offscreenIndicator.enabled = false;
        identifierTopText.enabled = false;
        identifierBottomText.enabled = false;

        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (identifiableObject == null) {
            Destroy(gameObject);
            return;
        }
        
        Transform objectTransform = identifiableObject.transform;
        IdentifierData identifierData = identifiable.GetIdentifierData();
        if (!identifierData.isActive)
        {
            if (isEnabled) FullReset(identifierData.color);
            return;
        }

        // Basic screen and object information
        Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 objectScreenCenterPos = Camera.main.WorldToScreenPoint(objectTransform.position);
        Vector3 objectDirectionToCamera = (Camera.main.transform.position - objectTransform.position).normalized;
        float distanceToObject = Vector3.Distance(Camera.main.transform.position, objectTransform.position);

        if (!isEnabled && identifierData.isActive)
        {
            // Check if the object is in front of the camera
            if (Vector3.Dot(objectDirectionToCamera, Camera.main.transform.forward) > 0)
                return;

            // Render UI only if the object crosses near the reticle.
            float detectionRadius = CalculateDetectionRadius(distanceToObject);
            if (Mathf.Abs(screenCenter.x - objectScreenCenterPos.x) > detectionRadius || Mathf.Abs(screenCenter.y - objectScreenCenterPos.y) > detectionRadius)
                return;

            // Check if the object is visible to the camera
            if (!ObjectVisibilityCheck(objectTransform))
                return;

            isEnabled = true;
        }

        // Check if the object is on screen
        bool isOnScreen = !(objectScreenCenterPos.x < 0 || objectScreenCenterPos.x > Screen.width ||
                            objectScreenCenterPos.y < 0 || objectScreenCenterPos.y > Screen.height) &&
                            Vector3.Dot(objectDirectionToCamera, Camera.main.transform.forward) < 0;

        // Toggle various elements based on on-screen status
        mainIndicator.enabled = isOnScreen;
        identifierTopText.enabled = isOnScreen;
        identifierBottomText.enabled = isOnScreen;
        offscreenIndicator.enabled = !isOnScreen;

        if (!isOnScreen)
        {
            // Off Screen
            Vector3 offscreenIndicatorPosition = CalculateOffscreenIndicatorPosition(objectTransform);
            
            // Handle off-screen indicator fading and hiding
            FadeOutOffscreenIndicator(identifierData.color);

            // Set position
            offscreenIndicatorRect.anchoredPosition = offscreenIndicatorPosition;
        }
        else
        {
            // On Screen
            // Check if the object is visible at a given interval
            // TODO: Possible performance issues
            ObjectVisibilityCheckTimer += Time.deltaTime;
            if (ObjectVisibilityCheckTimer >= ObjectVisibilityCheckInterval)
            {
                isVisible = ObjectVisibilityCheck(objectTransform);
                ObjectVisibilityCheckTimer = 0f;
            }
            if (isVisible)
            {
                identifierRect.sizeDelta = CalculateMainIndicatorSize(objectTransform, objectDirectionToCamera, objectScreenCenterPos);
                FadeInMainIndicator(identifierData.color);
            }
            else FadeOutMainIndicator(identifierData.color);

            // Update main indicator info
            identifierTopText.text = identifierData.topText;
            identifierBottomText.text = identifierData.bottomText;
            
            // Reset off-screen indicator if it was previously shown
            if (offscreenIndicator.enabled) ResetOffscreenIndicator(identifierData.color);

            // Set position
            identifierRect.anchoredPosition = objectScreenCenterPos / parentCanvas.scaleFactor;
        }
    }
    #endregion


    #region Initialization
    public void Initialize(IIdentifiable identifiable, Transform offscreenIndicatorContainer)
    {
        this.identifiable = identifiable;
        identifiableObject = ((Component)identifiable).gameObject;
        objectRenderers = identifiableObject.GetComponentsInChildren<Renderer>();
        objectCollider = identifiableObject.GetComponent<Collider>();
        if (objectCollider == null) objectCollider = identifiableObject.GetComponentInChildren<Collider>();
        objectColliderExtents = objectCollider.bounds.extents + new Vector3(1f, 1f, 1f); // Add some padding to ensure the identifier fully encompasses the object
        offscreenIndicator.transform.SetParent(offscreenIndicatorContainer, false);
        this.offscreenIndicatorContainer = offscreenIndicatorContainer.GetComponent<RectTransform>();
        offscreenIndicatorRect.anchoredPosition = this.offscreenIndicatorContainer.rect.center;
        
        IdentifierData identifierData = identifiable.GetIdentifierData();
        FullReset(identifierData.color);

        isInitialized = true;
    }
    #endregion
    

    #region Main Indicator
    private Vector2 CalculateMainIndicatorSize(Transform objectTransform, Vector3 objectDirectionToCamera, Vector3 objectScreenCenterPos)
    {
        // Gets the outer corner position of the object on screen
        Vector3 objectToCameraSideVector = Vector3.Cross(objectDirectionToCamera, Camera.main.transform.up).normalized;
        Vector3 objectScreenCornerPos = Camera.main.WorldToScreenPoint(
            objectTransform.position +
            objectColliderExtents.y * Camera.main.transform.up +
            objectColliderExtents.x * Mathf.Sign(objectScreenCenterPos.x - Screen.width / 2f) * objectToCameraSideVector);

        // Calculate the size of the identifier based on the distance between the center and corner positions
        Vector3 objectScreenExtents = objectScreenCornerPos - objectScreenCenterPos;
        Vector2 desiredSize = new(Mathf.Max(Mathf.Abs(objectScreenExtents.x) * 2f, 25f), Mathf.Max(Mathf.Abs(objectScreenExtents.y) * 2f, 25f));
        return Vector2.Lerp(identifierRect.sizeDelta, desiredSize, mainIndicatorSizeLerpSpeed);
    }

    private void FadeInMainIndicator(Color teamColor)
    {
        if (mainInfoShowTimer >= mainInfoShowTime)
        {
            FadeElementAlpha(identifierTopText, teamColor, 1f);
            FadeElementAlpha(identifierBottomText, teamColor, 1f);
        }
        else mainInfoShowTimer += Time.deltaTime;
        
        FadeElementAlpha(mainIndicator, teamColor, mainIndicatorOriginalAlpha);
    }

    private void FadeOutMainIndicator(Color teamColor)
    {
        bool done = FadeElementAlpha(mainIndicator, teamColor, 0f);
        done = FadeElementAlpha(identifierTopText, teamColor, 0f) && done;
        done = FadeElementAlpha(identifierBottomText, teamColor, 0f) && done;
        if (done) FullReset(teamColor);
    }

    private void ResetMainIndicator(Color teamColor)
    {
        mainIndicator.enabled = false;
        identifierTopText.enabled = false;
        identifierBottomText.enabled = false;
        SetElementAlpha(mainIndicator, teamColor, 0f);
        SetElementAlpha(identifierTopText, teamColor, 0f);
        SetElementAlpha(identifierBottomText, teamColor, 0f);
        identifierRect.sizeDelta = new Vector2(Screen.width * 2f, Screen.height * 2f);
        mainInfoShowTimer = 0f;
    }
    #endregion


    #region Offscreen Indicator
    private Vector3 CalculateOffscreenIndicatorPosition(Transform objectTransform)
    {
        // Position the off-screen indicator at the edge of the screen in the direction of the object
        float maxObjectDistanceForIndicator = 500f;
        Vector3 directionToObject = Camera.main.transform.InverseTransformDirection(
            Vector3.ProjectOnPlane(
                objectTransform.position - Camera.main.transform.position,
                Camera.main.transform.forward
            ).normalized
        );
        float distanceToObject = Vector3.Distance(Camera.main.transform.position, objectTransform.position);
        float angle = Mathf.Atan2(directionToObject.y, directionToObject.x) * Mathf.Rad2Deg - 90f;
        offscreenIndicator.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        Vector2 containerRatio = offscreenIndicatorContainer.rect.size.normalized;
        // Map indicator to oval-sized container to ensure it stays within bounds while still pointing in the correct direction
        float indicatorDistanceFromCenter = Mathf.Lerp(
            30f,
            offscreenIndicatorContainer.rect.size.magnitude / 2f - 30f,
            Mathf.Clamp01(distanceToObject / maxObjectDistanceForIndicator)
        );
        Vector3 desiredPosition = new Vector3(directionToObject.x * containerRatio.x, directionToObject.y * containerRatio.y, 0f) * indicatorDistanceFromCenter;
        return Vector3.Lerp(offscreenIndicatorRect.anchoredPosition, desiredPosition, 0.3f);
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
    private bool ObjectVisibilityCheck(Transform objectTransform)
    {
        bool isVisible = false;

        // Renderer.isVisible can be used to check if the object is completely off screen, but not reliable to confirm visibility,
        // since it will also be true if only a small part or even the shadow is visible.
        for (int i = 0; i < objectRenderers.Length; i++)
        {
            if (objectRenderers[i].isVisible)
            {
                isVisible = true;
                break;
            }
        }
        if (!isVisible) return false;


        // Perform a raycast to check if there are any obstacles between the camera and the object
        Ray ray = new(Camera.main.transform.position, (objectTransform.position - Camera.main.transform.position).normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            isVisible = hit.collider.gameObject == identifiableObject || hit.collider.gameObject.transform.IsChildOf(identifiableObject.transform);
        }
        return isVisible;
    }
    
    private float CalculateDetectionRadius(float distanceToObject)
    {
        // The further an object is, the smaller the area around the reticle that it can be detected in.
        float t = Mathf.InverseLerp(detectionMinDistanceRange, detectionMaxDistanceRange, distanceToObject);
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