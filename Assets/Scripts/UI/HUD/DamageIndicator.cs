using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour
{
    [SerializeField] private Image damageImage;
    [SerializeField] private Color badColor;
    [SerializeField] private Color goodColor;

    private float fullValue = 0.02f;
    private float fullAlpha = 0.8f;
    private float emptyValue = 0.13f;
    private float emptyAlpha = 0.3f;

    private float currentValueRatio = 0;
    private float currentValue = 0;
    private float currentAlpha = 0;

    void Awake()
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().onHealthChanged.AddListener(OnHealthChanged);
    }

    // Update is called once per frame
    void Update()
    {
        bool isBad = currentValueRatio < 0;
        float alpha = Mathf.Lerp(emptyAlpha, fullAlpha, Mathf.Abs(currentValueRatio));
        float value = Mathf.Lerp(emptyValue, fullValue, Mathf.Abs(currentValueRatio));

        currentAlpha = Mathf.Lerp(currentAlpha, alpha, Time.deltaTime * 10);
        currentValue = Mathf.Lerp(currentValue, value, Time.deltaTime * 10);

        damageImage.color = Color.Lerp(goodColor, badColor, isBad ? 1 : 0);
        damageImage.color = new Color(damageImage.color.r, damageImage.color.g, damageImage.color.b, currentAlpha);
        damageImage.pixelsPerUnitMultiplier = currentValue;

        currentValueRatio = Mathf.Lerp(currentValueRatio, 0, Time.deltaTime);
        Debug.Log($"Damage Indicator - Ratio: {currentValueRatio}, Alpha: {currentAlpha}, Value: {currentValue}");
    }

    private void OnHealthChanged(float healthDeltaRatio) => currentValueRatio += healthDeltaRatio;
}
