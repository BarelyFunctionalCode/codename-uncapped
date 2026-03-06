using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TerrainLayer
{
    public string layerName;
    public bool isActive = true;
    public float minAmplitude = 0;
    public float maxAmplitude = 1;
    public float amplitude;
    public float minFrequency = 0;
    public float maxFrequency = 1;
    public float frequency;
    public float minPersistence = 0;
    public float maxPersistence = 1;
    public float persistence;
    public float minLacunarity = 0;
    public float maxLacunarity = 1;
    public float lacunarity;

    public TerrainLayer(float amplitude, float frequency, float persistence, float lacunarity)
    {
        this.amplitude = amplitude;
        this.frequency = frequency;
        this.persistence = persistence;
        this.lacunarity = lacunarity;
    }
}

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] GameObject terrainChunkPrefab;

    [SerializeField] GameObject layerUIPrefab;
    [SerializeField] GameObject layersContainerObj;

    [SerializeField] TMP_InputField layerAmplitudeMinInput;
    [SerializeField] TMP_InputField layerAmplitudeMaxInput;
    [SerializeField] Slider layerAmplitudeSlider;
    [SerializeField] TMP_InputField layerFrequencyMinInput;
    [SerializeField] TMP_InputField layerFrequencyMaxInput;
    [SerializeField] Slider layerFrequencySlider;
    [SerializeField] TMP_InputField layerPersistenceMinInput;
    [SerializeField] TMP_InputField layerPersistenceMaxInput;
    [SerializeField] Slider layerPersistenceSlider;
    [SerializeField] TMP_InputField layerLacunarityMinInput;
    [SerializeField] TMP_InputField layerLacunarityMaxInput;
    [SerializeField] Slider layerLacunaritySlider;

    private int currentLayerIndex = -1;
    private List<TerrainLayer> terrainLayers = new();

    private GameObject terrainObj;
    void Start()
    {
        terrainObj = Instantiate(terrainChunkPrefab, Vector3.zero, Quaternion.identity);
        InitializeChunk();
    }

    public void OnAddLayerButtonPressed()
    {
        TerrainLayer newLayer = new(0, 0, 0, 0);
        terrainLayers.Add(newLayer);

        GameObject newLayerUI = Instantiate(layerUIPrefab, layersContainerObj.transform);
        // Make the new object the 3rd to last child (before the Add Layer button)
        currentLayerIndex++;
        newLayerUI.transform.SetSiblingIndex(layersContainerObj.transform.childCount - 3);
        newLayerUI.name = $"Layer_{currentLayerIndex}";
        newLayerUI.GetComponent<TerrainLayerUI>().Initialize(currentLayerIndex, newLayer, this);
    }

    public void SelectLayer(int index)
    {
        if (index < 0 || index >= terrainLayers.Count) return;
        currentLayerIndex = index;

        // Update UI sliders and inputs to reflect the selected layer's parameters
        TerrainLayer selectedLayer = terrainLayers[currentLayerIndex];
        layerAmplitudeMinInput.text = selectedLayer.minAmplitude.ToString();
        layerAmplitudeMaxInput.text = selectedLayer.maxAmplitude.ToString();
        layerAmplitudeSlider.value = (selectedLayer.amplitude - selectedLayer.minAmplitude) / (selectedLayer.maxAmplitude - selectedLayer.minAmplitude);
        layerFrequencyMinInput.text = selectedLayer.minFrequency.ToString();
        layerFrequencyMaxInput.text = selectedLayer.maxFrequency.ToString();
        layerFrequencySlider.value = (selectedLayer.frequency - selectedLayer.minFrequency) / (selectedLayer.maxFrequency - selectedLayer.minFrequency);
        layerPersistenceMinInput.text = selectedLayer.minPersistence.ToString();
        layerPersistenceMaxInput.text = selectedLayer.maxPersistence.ToString();
        layerPersistenceSlider.value = (selectedLayer.persistence - selectedLayer.minPersistence) / (selectedLayer.maxPersistence - selectedLayer.minPersistence);
        layerLacunarityMinInput.text = selectedLayer.minLacunarity.ToString();
        layerLacunarityMaxInput.text = selectedLayer.maxLacunarity.ToString();
        layerLacunaritySlider.value = (selectedLayer.lacunarity - selectedLayer.minLacunarity) / (selectedLayer.maxLacunarity - selectedLayer.minLacunarity);
    }

    public void RemoveLayer(int index)
    {
        if (index < 0 || index >= terrainLayers.Count) return;
        terrainLayers.RemoveAt(index);
    }

    public void SetLayerName(int index, string name)
    {
        if (index < 0 || index >= terrainLayers.Count) return;
        terrainLayers[index].layerName = name;
    }

    public void SetLayerActive(int index, bool isActive)
    {
        if (index < 0 || index >= terrainLayers.Count) return;
        terrainLayers[index].isActive = isActive;
    }

    public void OnAmplitudeSliderChanged()
    {
        float min = float.Parse(layerAmplitudeMinInput.text);
        float max = float.Parse(layerAmplitudeMaxInput.text);
        float value = layerAmplitudeSlider.value;
        float amplitude = Mathf.Lerp(min, max, value);

        terrainLayers[currentLayerIndex].amplitude = amplitude;
        terrainLayers[currentLayerIndex].minAmplitude = min;
        terrainLayers[currentLayerIndex].maxAmplitude = max;
        InitializeChunk();
    }

    public void OnFrequencySliderChanged()
    {
        float min = float.Parse(layerFrequencyMinInput.text);
        float max = float.Parse(layerFrequencyMaxInput.text);
        float value = layerFrequencySlider.value;
        float frequency = Mathf.Lerp(min, max, value);

        terrainLayers[currentLayerIndex].frequency = frequency;
        terrainLayers[currentLayerIndex].minFrequency = min;
        terrainLayers[currentLayerIndex].maxFrequency = max;
        InitializeChunk();
    }

    public void OnPersistenceSliderChanged()
    {
        float min = float.Parse(layerPersistenceMinInput.text);
        float max = float.Parse(layerPersistenceMaxInput.text);
        float value = layerPersistenceSlider.value;
        float persistence = Mathf.Lerp(min, max, value);

        terrainLayers[currentLayerIndex].persistence = persistence;
        terrainLayers[currentLayerIndex].minPersistence = min;
        terrainLayers[currentLayerIndex].maxPersistence = max;
        InitializeChunk();
    }

    public void OnLacunaritySliderChanged()
    {
        float min = float.Parse(layerLacunarityMinInput.text);
        float max = float.Parse(layerLacunarityMaxInput.text);
        float value = layerLacunaritySlider.value;
        float lacunarity = Mathf.Lerp(min, max, value);

        terrainLayers[currentLayerIndex].lacunarity = lacunarity;
        terrainLayers[currentLayerIndex].minLacunarity = min;
        terrainLayers[currentLayerIndex].maxLacunarity = max;
        InitializeChunk();
    }

    void InitializeChunk()
    {
        Terrain terrain = terrainObj.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("Terrain component not found on chunk prefab.");
            return;
        }
        TerrainData terrainData = terrain.terrainData;
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];
        // Example: Simple height generation using layers
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float heightValue = 0f;
                foreach (var layer in terrainLayers)
                {
                    if (layer.isActive)
                    {
                        float amplitude = layer.amplitude;
                        float frequency = layer.frequency;
                        float persistence = layer.persistence;
                        float lacunarity = layer.lacunarity;
                        frequency *= lacunarity;
                        amplitude *= persistence;
                        float noiseValue = Mathf.PerlinNoise(x * frequency / width, y * frequency / height);
                        heightValue += noiseValue * amplitude;
                    }
                }
                heights[x, y] = heightValue;
            }
        }
        terrainData.SetHeights(0, 0, heights);
    }
}