using UnityEngine;
using UnityEngine.UI;

public class CenterClusterUI : MonoBehaviour
{
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image energyBarImage;

    private Entity entity = null;
    private Health entityHealth = null;
    private Energy entityEnergy = null;
    private bool isInitialized = false;

    public void Initialize(Entity entity)
    {
        if (isInitialized) return;
        isInitialized = true;

        this.entity = entity;
        entityHealth = entity.health;
        entityEnergy = entity.energy;
    }

    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;

        entity = null;
        entityHealth = null;
        entityEnergy = null;
    }

    private void Update()
    {
        if (!isInitialized || entity == null) return;

        healthBarImage.fillAmount = entityHealth.HealthPercentage;
        energyBarImage.fillAmount = entityEnergy.EnergyPercentage;
    }
}
