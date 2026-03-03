using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExpandableListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private Button itemButton;
    [SerializeField] private GameObject disabledOverlayObj;

    private UnityEvent<ScriptableObject> onListItemSelected = new();
    private ScriptableObject associatedItemSO;

    private void OnDestroy()
    {
        onListItemSelected.RemoveAllListeners();
    }

    public UnityEvent<ScriptableObject> Initialize(string itemName, bool isEnabled = true, ScriptableObject itemSO = null)
    {
        itemNameText.text = itemName;
        associatedItemSO = itemSO;
        itemButton.interactable = isEnabled;
        disabledOverlayObj.SetActive(!isEnabled);
        return onListItemSelected;
    }

    public void OnButtonClicked()
    {
        onListItemSelected?.Invoke(associatedItemSO);
    }
}
