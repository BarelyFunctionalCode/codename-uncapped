using System.Collections.Generic;
using TMPro;
using UnityEditor;
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

    public TerrainLayer(float amplitude, float frequency)
    {
        this.amplitude = amplitude;
        this.frequency = frequency;
    }
}

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] GameObject terrainChunkPrefab;

    [SerializeField] TMP_InputField mapNameInput;

    [SerializeField] GameObject layerUIPrefab;
    [SerializeField] GameObject layersContainerObj;
    [SerializeField] GameObject layersDetailsContainerObj;

    [SerializeField] TMP_InputField layerAmplitudeMinInput;
    [SerializeField] TMP_InputField layerAmplitudeMaxInput;
    [SerializeField] Slider layerAmplitudeSlider;
    [SerializeField] TMP_InputField layerFrequencyMinInput;
    [SerializeField] TMP_InputField layerFrequencyMaxInput;
    [SerializeField] Slider layerFrequencySlider;

    private float mapWidth = 2048;
    private float mapHeight = 2048;

    private int currentLayerIndex = -1;
    private List<TerrainLayer> terrainLayers = new();

    private GameObject terrainObj;
    void Start()
    {
        layersDetailsContainerObj.SetActive(false);
        terrainObj = Instantiate(terrainChunkPrefab, Vector3.zero, Quaternion.identity);
        InitializeChunk();
    }

    void Update()
    {
        // Slowly rotate the terrain for better visualization
        terrainObj.transform.Rotate(Vector3.up, 10f * Time.deltaTime);
    }

    public void OnMapSaveButtonPressed()
    {
        string mapName = mapNameInput.text;
        if (string.IsNullOrEmpty(mapName))
        {
            Debug.LogWarning("Map name cannot be empty.");
            return;
        }

        int heightmapResolution = terrainObj.GetComponent<Terrain>().terrainData.heightmapResolution;
        TerrainData newTerrainData = new TerrainData
        {
            heightmapResolution = heightmapResolution,
            size = terrainObj.GetComponent<Terrain>().terrainData.size
        };
        newTerrainData.SetHeights(0, 0, terrainObj.GetComponent<Terrain>().terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution));
        
        // Only works in Editor
        #if UNITY_EDITOR
            Debug.Log($"Saving map '{mapName}' to Assets/Terrains/{mapName}.asset");
            AssetDatabase.CreateAsset(newTerrainData, $"Assets/Terrains/{mapName}.asset");
        #endif
        Debug.Log($"Map '{mapName}' saved successfully.");
    }

    public void OnMapClearButtonPressed()
    {
        layersDetailsContainerObj.SetActive(false);
        terrainLayers = new();
        if (terrainObj != null)
        {
            Destroy(terrainObj);
        }
        terrainObj = Instantiate(terrainChunkPrefab, Vector3.zero, Quaternion.identity);
        InitializeChunk();
    }

    public void OnAddLayerButtonPressed()
    {
        TerrainLayer newLayer = new(0, 0);
        terrainLayers.Add(newLayer);

        GameObject newLayerUI = Instantiate(layerUIPrefab, layersContainerObj.transform);
        // Make the new object the 3rd to last child (before the Add Layer button)
        currentLayerIndex++;
        newLayerUI.transform.SetSiblingIndex(layersContainerObj.transform.childCount - 3);
        newLayerUI.name = $"Layer_{currentLayerIndex}";
        newLayerUI.GetComponent<TerrainLayerUI>().Initialize(currentLayerIndex, newLayer, this);
        layersDetailsContainerObj.SetActive(true);
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
    }

    public void RemoveLayer(int index)
    {
        if (index < 0 || index >= terrainLayers.Count) return;
        terrainLayers.RemoveAt(index);
        if (terrainLayers.Count == 0) layersDetailsContainerObj.SetActive(false);
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

    void InitializeChunk()
    {
        terrainObj.transform.position = new Vector3(-mapWidth / 2, 0, -mapHeight / 2);
        Terrain terrain = terrainObj.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning("Terrain component not found on chunk prefab.");
            return;
        }
        TerrainData terrainData = terrain.terrainData;
        terrainData.size = new Vector3(mapWidth, 600, mapHeight);
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