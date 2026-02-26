using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField] private CenterClusterUI centerClusterUI;

    private Entity entity = null;
    private bool isInitialized = false;

    public void Initialize(Entity entity)
    {
        if (isInitialized) return;
        isInitialized = true;

        this.entity = entity;
        centerClusterUI.Initialize(entity);
    }
}
