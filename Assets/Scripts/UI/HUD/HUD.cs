using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField] private CenterClusterUI centerClusterUI;
    [SerializeField] private Transform weaponsContainer;
    [SerializeField] private GameObject weaponUIPrefabObj;
    [SerializeField] private ThrowableUI throwableUI;
    [SerializeField] private Transform gearContainer;

    private bool isInitialized = false;

    public void Initialize(Entity entity)
    {
        if (isInitialized) return;
        isInitialized = true;

        centerClusterUI.Initialize(entity);
    }

    public void AddWeaponUI(Weapon weapon)
    {
        GameObject weaponUIObj = Instantiate(weaponUIPrefabObj, weaponsContainer);
        WeaponUI weaponUI = weaponUIObj.GetComponent<WeaponUI>();
        weaponUI.Initialize(weapon);
    }

    public void SetThrowableUI(ThrowableManager throwableManager)
    {
        throwableUI.Initialize(throwableManager);
    }
}
