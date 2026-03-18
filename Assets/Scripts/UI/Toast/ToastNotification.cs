using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ToastNotification : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;

    private float hideTime = 5f;
    private float hideTimer = 0f;

    private bool doHide = false;

    private Vector2 targetPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRectTransform = transform.parent.GetComponent<RectTransform>();
    }

    public void Initialize(string message)
    {
        messageText.text = message;
        transform.SetAsLastSibling();
        if (transform.GetSiblingIndex() == 0) return;

        Transform previousSibling = transform.parent.GetChild(transform.GetSiblingIndex() - 1);
        if (previousSibling.TryGetComponent(out ToastNotification previousToast))
        {
            rectTransform.anchoredPosition = previousToast.rectTransform.anchoredPosition - new Vector2(0, rectTransform.rect.height + 10);
        }
        gameObject.SetActive(true);
    }


    private void Update()
    {
        if (hideTimer >= hideTime) doHide = true;
        if (!doHide) hideTimer += Time.deltaTime;

        targetPosition = Vector2.zero;
        int siblingIndex = transform.GetSiblingIndex();
        if (siblingIndex > 0)
        {
            Transform previousSibling = transform.parent.GetChild(siblingIndex - 1);
            if (previousSibling.TryGetComponent(out ToastNotification previousToast))
            {
                targetPosition = previousToast.targetPosition - new Vector2(0, rectTransform.rect.height + 10);
            }
        }
        if (doHide) targetPosition += new Vector2(0, rectTransform.rect.height + 10);

        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * 10f);
    }
}
