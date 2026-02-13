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
    private Vector3 entityColliderExtents;
    private RectTransform identifierRect;

    float detectionRadius = 30f;
    bool isHidden = true;
    private float offscreenHideTime = 5f;
    private float offscreenHideTimer = 0f;

    private void Start()
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
        if (entity)
        {
            Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, 0f);

            // Center point of the entity on screen
            Vector3 entityScreenCenterPos = Camera.main.WorldToScreenPoint(entity.transform.position);

            // Render UI only if the entity crosses near the reticle.
            if (Mathf.Abs(screenCenter.x - entityScreenCenterPos.x) < detectionRadius && Mathf.Abs(screenCenter.y - entityScreenCenterPos.y) < detectionRadius)
                isHidden = false;

            if (isHidden) return;

            // Gets the outer corner position of the entity on screen
            Vector3 entityDirectionToCamera = (Camera.main.transform.position - entity.transform.position).normalized;
            Vector3 entityToCameraSideVector = Vector3.Cross(entityDirectionToCamera, Camera.main.transform.up).normalized;
            Vector3 entityScreenCornerPos = Camera.main.WorldToScreenPoint(
                entity.transform.position +
                entityColliderExtents.y * Camera.main.transform.up +
                entityColliderExtents.x * Mathf.Sign(entityScreenCenterPos.x - Screen.width / 2f) * entityToCameraSideVector);

            // Calculate the size of the identifier based on the distance between the center and corner positions
            Vector3 entityScreenExtents = entityScreenCornerPos - entityScreenCenterPos;
            identifierRect.sizeDelta = new Vector2(Mathf.Max(Mathf.Abs(entityScreenExtents.x) * 2f, 25f), Mathf.Max(Mathf.Abs(entityScreenExtents.y) * 2f, 25f));

            // Check if the entity is on screen (considering the size of the identifier)
            bool isOnScreen = !(entityScreenCenterPos.x + identifierRect.sizeDelta.x < 0 || entityScreenCenterPos.x - identifierRect.sizeDelta.x > Screen.width ||
                                entityScreenCenterPos.y + identifierRect.sizeDelta.y < 0 || entityScreenCenterPos.y - identifierRect.sizeDelta.y > Screen.height);

            // Toggle visibility of indicators and text based on whether the entity is on screen
            offscreenIndicator.enabled = !isOnScreen;
            mainIndicator.enabled = isOnScreen;
            identifierText.enabled = isOnScreen;
            healthText.enabled = isOnScreen;

            Vector3 rectPosition = entityScreenCenterPos;
            Color teamColor = tempTeamColors[entity.GetTeamId()];
            if (!isOnScreen)
            {
                // Position the off-screen indicator at the edge of the screen in the direction of the entity
                float indicatorDistanceFromCenter = 210f;
                Vector3 directionToEntity = (entityScreenCenterPos - screenCenter).normalized;
                float angle = Mathf.Atan2(directionToEntity.y, directionToEntity.x) * Mathf.Rad2Deg - 90f;
                offscreenIndicator.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
                Vector3 desiredPosition = screenCenter + directionToEntity * indicatorDistanceFromCenter;
                rectPosition = Vector3.Lerp(identifierRect.anchoredPosition, desiredPosition, 20f * Time.deltaTime);

                // Handle off-screen indicator fading and hiding
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
                    }
                }
            }
            else
            {
                identifierText.text = entity.GetIdentifier();
                Debug.Log(entity.GetHealthPercentage());
                healthText.text = $"{Mathf.CeilToInt(entity.GetHealthPercentage() * 100f)}%";
                offscreenHideTimer = 0f;
                isHidden = false;
                mainIndicator.color = new Color(teamColor.r, teamColor.g, teamColor.b, mainIndicatorOriginalAlpha);
                offscreenIndicator.color = new Color(teamColor.r, teamColor.g, teamColor.b, offscreenIndicatorOriginalAlpha);
            }

            // Set position
            identifierRect.anchoredPosition = rectPosition;
        }
    }

    public void Initialize(Entity entity)
    {
        this.entity = entity;
        entityColliderExtents = entity.GetComponent<Collider>().bounds.extents + new Vector3(1f, 1f, 1f); // Add some padding to ensure the identifier fully encompasses the entity
        Debug.Log($"Initialized EntityIdentifierUI for {entity.name} with collider extents {entityColliderExtents}");
    }
}