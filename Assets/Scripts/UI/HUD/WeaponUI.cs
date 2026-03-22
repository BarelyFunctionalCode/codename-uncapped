using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private Image sideBorderImage;

    private bool isInitialized = false;
    private Weapon weapon;
    private float activeAlpha;
    private float inactiveAlpha;

    private void Update()
    {
        if (!isInitialized) return;

        if (weapon == null)
        {
            Destroy(gameObject);
            return;
        }

        // Turn off border for last weapon UI element
        sideBorderImage.gameObject.SetActive(transform.GetSiblingIndex() != transform.parent.childCount - 1);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (weapon == null) return;
        weapon.ammoCount.OnValueChanged -= UpdateAmmo;
        weapon.isEquiped.OnValueChanged -= UpdateActiveState;
    }

    public void Initialize(Weapon weapon)
    {
        this.weapon = weapon;
        inactiveAlpha = borderImage.color.a;
        activeAlpha = inactiveAlpha * 2f;
        weaponIconImage.sprite = weapon.iconSprite;
        ammoCountText.text = weapon.maxAmmo.ToString();

        weapon.ammoCount.OnValueChanged += UpdateAmmo;
        weapon.isEquiped.OnValueChanged += UpdateActiveState;
        isInitialized = true;
    }

    private void UpdateAmmo(float _, float ammoCount)
    {
        ammoCountText.text = ammoCount.ToString();
    }

    private void UpdateActiveState(bool _, bool isActive)
    {
        Color newColor = borderImage.color;
        newColor.a = isActive ? activeAlpha : inactiveAlpha;

        weaponIconImage.color = newColor;
        borderImage.color = newColor;
        newColor.a *= 2f;
        ammoCountText.color = newColor;
    }
}
