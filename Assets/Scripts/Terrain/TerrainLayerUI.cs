using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerrainLayerUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField layerNameInput;
    [SerializeField] private Button toggleActiveButton;
    private bool isActive = true;
    private int layerIndex;
    private TerrainLayer terrainLayer;
    private TerrainGenerator terrainGenerator;

    public void Initialize(int index, TerrainLayer layer, TerrainGenerator generator)
    {
        layerIndex = index;
        terrainLayer = layer;
        terrainGenerator = generator;
    }

    public void OnLayerNameChanged()
    {
        terrainGenerator.SetLayerName(layerIndex, layerNameInput.text);
    }

    public void OnSelectLayerButtonPressed()
    {
        terrainGenerator.SelectLayer(layerIndex);
    }

    public void OnDeleteButtonPressed()
    {
        terrainGenerator.RemoveLayer(layerIndex);
        Destroy(gameObject);
    }

    public void OnToggleActiveButtonPressed()
    {
        isActive = !isActive;
        terrainGenerator.SetLayerActive(layerIndex, isActive);

        // Update the UI to reflect the active state (e.g., change button color)
        ColorBlock colors = toggleActiveButton.colors;
        if (isActive)
        {
            colors.normalColor = Color.green;
        }
        else
        {
            colors.normalColor = Color.red;
        }
        toggleActiveButton.colors = colors;
    }
}
