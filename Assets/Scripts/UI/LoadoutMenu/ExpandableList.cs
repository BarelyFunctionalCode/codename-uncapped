using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExpandableList : MonoBehaviour
{
    [SerializeField] private GameObject expandableListItemPrefabObj;
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private RectTransform listRect;

    [SerializeField] private Transform contentContainer;
    [SerializeField] private RectTransform arrowRect;

    private RectTransform rectTransform;
    private float currentExpansion = 0f;
    private bool doExpand = false;
    private bool isMoving = false;

    public int itemCount = 0;

    float expandSpeed = 5f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {
            currentExpansion = Mathf.MoveTowards(currentExpansion, doExpand ? 1f : 0f, Time.deltaTime * expandSpeed);
            if (Mathf.Abs(currentExpansion - (doExpand ? 1f : 0f)) < 0.01f)
            {
                currentExpansion = doExpand ? 1f : 0f;
                isMoving = false;
            }

            float expandedHeight = buttonRect.sizeDelta.y + listRect.sizeDelta.y;
            rectTransform.sizeDelta = new Vector2(
                rectTransform.sizeDelta.x,
                Mathf.Lerp(buttonRect.sizeDelta.y, expandedHeight, currentExpansion)
            );
            float expandedArrowAngle = 180f;
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, expandedArrowAngle, currentExpansion));
        }
    }

    public UnityEvent<ScriptableObject> AddListItem(string itemName, bool isEnabled = true, ScriptableObject itemSO = null)
    {
        GameObject newItem = Instantiate(expandableListItemPrefabObj, contentContainer);
        itemCount++;
        return newItem.GetComponent<ExpandableListItem>().Initialize(itemName, isEnabled, itemSO);
    }

    public void ClearList()
    {
        foreach (Transform child in contentContainer) Destroy(child.gameObject);
        itemCount = 0;
    }

    public void OnListClicked()
    {
        // Toggle this list and collapse others
        foreach (Transform child in transform.parent)
        {
            if (child == transform) ToggleList();
            else
            {
                ExpandableList otherList = child.GetComponent<ExpandableList>();
                if (otherList != null) otherList.ToggleList(true);
            }
        }
    }

    public void ToggleList(bool forceCollapse = false)
    {
        isMoving = true;
        if (forceCollapse) doExpand = false;
        else doExpand = !doExpand;
    }
}
