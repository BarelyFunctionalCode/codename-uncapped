using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public struct IdentifierData
{
    public Color color;
    public string topText;
    public string bottomText;
    public bool isActive;
    public Transform targetTransform;
    public bool isAlwaysVisible;
}

public interface IIdentifiable
{
    public IdentifierData GetIdentifierData();
}


[UxmlElement(libraryPath = "FFIndicator")]
public partial class FFIndicatorManager : VisualElement
{
    private bool isInitialized = false;


    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
        SpawnManager.Instance.Subscribe(RegisterIdentifier);
    }

    public void Deinitialize()
    {
        if (!isInitialized) return;
        isInitialized = false;
        if (SpawnManager.Instance != null) SpawnManager.Instance.Unsubscribe(RegisterIdentifier);
    }

    public void RegisterIdentifier(GameObject obj)
    {
        if (obj == null) return;
        if (Player.Instance && Player.Instance.Character &&
            Player.Instance.Character.localCharacterType && 
            obj.transform.IsChildOf(Player.Instance.Character.transform)) return;
        if (!obj.TryGetComponent<IIdentifiable>(out var identifiable)) return;

        FFIndicator newIndicator = (FFIndicator)UIManager.Spawn("UI/HUD/FFIndicator/FFIndicator", this);
        newIndicator.Initialize(identifiable);
    }

    public void Update()
    {
        foreach (var child in Children().ToArray()) if (child is FFIndicator indicator) indicator.Update();
    }
}