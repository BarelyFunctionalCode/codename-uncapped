using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThrowableUI : MonoBehaviour
{
    [SerializeField] private Image throwableIconImage;
    [SerializeField] private TMP_Text ammoCountText;

    private bool isInitialized = false;
    private ThrowableManager throwableManager;
    private float activeAlpha;
    private float inactiveAlpha;

    private void Update()
    {
        if (!isInitialized) return;

        if (throwableManager == null)
        {
            ammoCountText.text = "";
            throwableIconImage.sprite = null;
            return;
        }

        throwableIconImage.enabled = throwableIconImage.sprite != null;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (throwableManager == null) return;
        throwableManager.ammoCount.OnValueChanged -= UpdateAmmo;
    }

    public void Initialize(ThrowableManager throwableManager)
    {
        this.throwableManager = throwableManager;
        activeAlpha = transform.parent.GetComponent<Image>().color.a;
        inactiveAlpha = throwableIconImage.color.a;
        ammoCountText.text = throwableManager.maxAmmo.ToString();
        throwableIconImage.sprite = throwableManager.iconSprite;
        
        Color newColor = throwableIconImage.color;
        newColor.a = activeAlpha;
        throwableIconImage.color = newColor;
        newColor.a *= 2f;
        ammoCountText.color = newColor;

        throwableManager.ammoCount.OnValueChanged += UpdateAmmo;
        isInitialized = true;
    }

    private void UpdateAmmo(float _, float ammoCount)
    {
        ammoCountText.text = ammoCount.ToString();

        Color newColor = throwableIconImage.color;
        newColor.a = ammoCount > 0 ? activeAlpha : inactiveAlpha;
        throwableIconImage.color = newColor;
        newColor.a *= 2f;
        ammoCountText.color = newColor;
    }
}
