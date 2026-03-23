using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ToastNotification : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;

    private float hideTime = 5f;
    private float hideTimer = 0f;

    private bool doHide = false;
    private bool isInitialized = false;

    private Vector2 targetPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
    }

    public void Initialize(NotificationData data, float notificationDuration)
    {
        hideTime = notificationDuration;
        if (data.title != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = data.title;
        }
        messageText.text = data.content;
        if (data.color != default)
        {
            if (data.title != null) titleText.color = data.color;
            else messageText.color = data.color;
        }
        transform.SetAsLastSibling();

        Vector2 anchoredPosition = Vector2.zero;
        if (transform.parent.childCount > 1)
        {
            Transform previousSibling = transform.parent.GetChild(transform.GetSiblingIndex() - 1);
            if (previousSibling.TryGetComponent(out ToastNotification previousToast))
            {
                anchoredPosition = previousToast.rectTransform.anchoredPosition - new Vector2(0, rectTransform.rect.height + 10);
            }
        }
        rectTransform.anchoredPosition = anchoredPosition;
        gameObject.SetActive(true);
        isInitialized = true;
    }


    private void Update()
    {
        if (!isInitialized) return;
        if (hideTimer >= hideTime) doHide = true;
        hideTimer += Time.deltaTime;

        targetPosition = Vector2.zero;
        int siblingIndex = transform.GetSiblingIndex();
        if (siblingIndex > 0)
        {
            Transform previousSibling = transform.parent.GetChild(siblingIndex - 1);
            if (previousSibling.TryGetComponent(out ToastNotification previousToast))
            {
                targetPosition = previousToast.targetPosition - new Vector2(0, previousToast.rectTransform.rect.height + 10);
            }
        }
        if (doHide) targetPosition += new Vector2(0, rectTransform.rect.height + 10);

        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * 10f);

        if (doHide && (Vector2.Distance(rectTransform.anchoredPosition, targetPosition) < 0.1f || hideTimer >= hideTime * 2f))
        {
            Destroy(gameObject);
        }
    }
}
