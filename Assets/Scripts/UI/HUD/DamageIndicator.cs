using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicator : MonoBehaviour
{
    [SerializeField] private Image damageImage;
    [SerializeField] private Color badColor;
    [SerializeField] private Color goodColor;

    private float fullAlpha = 1f;
    private float emptyAlpha = 0;

    private float currentValueRatio = 0;
    private float currentAlpha = 0;

    void Awake()
    {
        NetworkManager
        .Singleton
        .LocalClient
        .PlayerObject
        .GetComponent<PlayerController>()
        .health
        .onHealthChanged
        .AddListener(OnHealthChanged);
    }

    // Update is called once per frame
    void Update()
    {
        bool isBad = currentValueRatio < 0;
        float alpha = Mathf.Lerp(emptyAlpha, fullAlpha, Mathf.Abs(currentValueRatio));
        currentAlpha = Mathf.Lerp(currentAlpha, alpha, Time.deltaTime * 10);

        damageImage.color = Color.Lerp(goodColor, badColor, isBad ? 1 : 0);
        damageImage.color = new Color(damageImage.color.r, damageImage.color.g, damageImage.color.b, currentAlpha);

        currentValueRatio = Mathf.Lerp(currentValueRatio, 0, Time.deltaTime);
    }

    private void OnHealthChanged(float healthDeltaRatio) => currentValueRatio += healthDeltaRatio;
}
