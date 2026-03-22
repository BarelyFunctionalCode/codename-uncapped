using UnityEngine;
using UnityEngine.UI;

public class CenterClusterUI : MonoBehaviour
{
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image energyBarImage;

    private Entity entity = null;
    private bool isInitialized = false;

    public void Initialize(Entity entity)
    {
        if (isInitialized) return;
        isInitialized = true;

        this.entity = entity;
    }

    private void Update()
    {
        if (!isInitialized) return;

        healthBarImage.fillAmount = entity.HealthPercentage;
        energyBarImage.fillAmount = entity.EnergyPercentage;
    }
}