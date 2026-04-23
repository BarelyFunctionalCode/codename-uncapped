using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GearUI : MonoBehaviour
{
    [SerializeField] private Image gearIconImage;
    [SerializeField] private TMP_Text ammoCountText;
    [SerializeField] private TMP_Text cooldownText;

    private bool isInitialized = false;
    private bool isCooldownActive = false;
    private Gear gear;
    private float activeAlpha;
    private float inactiveAlpha;

    private void Update()
    {
        if (!isInitialized) return;

        if (gear == null)
        {
            ammoCountText.text = "";
            cooldownText.text = "";
            gearIconImage.sprite = null;
            return;
        }

        gearIconImage.enabled = gearIconImage.sprite != null;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (gear == null) return;
        gear.ammo.OnValueChanged -= UpdateAmmo;
    }

    public void Initialize(Gear gear)
    {
        this.gear = gear;
        activeAlpha = transform.parent.GetComponent<Image>().color.a;
        inactiveAlpha = gearIconImage.color.a;
        ammoCountText.text = gear.MaxAmmo.ToString();
        gearIconImage.sprite = gear.iconSprite;
        
        Color newColor = gearIconImage.color;
        newColor.a = activeAlpha;
        gearIconImage.color = newColor;
        newColor.a *= 2f;
        ammoCountText.color = newColor;

        gear.ammo.OnValueChanged += UpdateAmmo;
        gear.cooldownTimer.OnValueChanged += UpdateCooldown;
        isInitialized = true;
    }

    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;

        if (gear != null)
        {
            gear.ammo.OnValueChanged -= UpdateAmmo;
            gear.cooldownTimer.OnValueChanged -= UpdateCooldown;
        }

        gear = null;
        ammoCountText.text = "";
        gearIconImage.sprite = null;
        cooldownText.gameObject.SetActive(false);
    }

    private void UpdateAmmo(int _, int ammoCount)
    {
        ammoCountText.text = ammoCount.ToString();

        Color newColor = gearIconImage.color;
        newColor.a = ammoCount > 0 ? activeAlpha : inactiveAlpha;
        gearIconImage.color = newColor;
        newColor.a *= 2f;
        ammoCountText.color = newColor;
    }

    private void UpdateCooldown(float _, float cooldown)
    {
        if (cooldown <= 0)
        {
            cooldownText.gameObject.SetActive(false);
            Color newColor = gearIconImage.color;
            newColor.a = gear.ammo.Value > 0 ? activeAlpha : inactiveAlpha;
            gearIconImage.color = newColor;
            newColor.a *= 2f;
            ammoCountText.color = newColor;
            isCooldownActive = false;
            return;
        }
        if (!isCooldownActive)
        {
            isCooldownActive = true;
            Color newColor = gearIconImage.color;
            newColor.a = inactiveAlpha;
            gearIconImage.color = newColor;
            newColor.a *= 2f;
            ammoCountText.color = newColor;
            cooldownText.gameObject.SetActive(true);
        }
        cooldownText.text = cooldown.ToString("F0");
    }
}
