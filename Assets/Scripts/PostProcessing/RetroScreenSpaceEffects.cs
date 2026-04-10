using UnityEngine;
using UnityEngine.Rendering;

public class RetroScreenSpaceEffects : VolumeComponent, IPostProcessComponent, IUpdatableVolumeComponent
{
    public FloatParameter ditherThreshold = new(30);
    public FloatParameter ditherScale = new(10);
    public FloatParameter pixelationValue = new(128);
    public IntParameter colorPrecision = new(32);
    // public FloatParameter fogSkyBlend = new(1.0f);
    // public FloatParameter fogDensity = new(1.0f);
    // [Range(0,100)]
    // public FloatParameter fogDistance = new(30.0f);
    // public ColorParameter fogColor = new(Color.white);
    // [Range(0,1000)]
    // public FloatParameter fogNoiseScale = new(3.0f);
    // [Range(0,1)]
    // public FloatParameter fogNoiseStrength = new(0.05f);
    
    public bool IsActive() => true;
    public bool IsTileCompatible() => false;

    public void Update()
    {
        Shader.SetGlobalFloat("_DitherThreshold", ditherThreshold.value);
        Shader.SetGlobalFloat("_DitherScale", ditherScale.value);
        Shader.SetGlobalFloat("_PixelationValue", pixelationValue.value);
        Shader.SetGlobalFloat("_ColorPrecision", colorPrecision.value);
        // Shader.SetGlobalFloat("_FogSkyBlend", fogSkyBlend.value);
        // Shader.SetGlobalFloat("_FogDensity", fogDensity.value);
        // Shader.SetGlobalFloat("_FogDistance", fogDistance.value);
        // Shader.SetGlobalColor("_FogColor", fogColor.value);
        // Shader.SetGlobalFloat("_FogNoiseScale", fogNoiseScale.value);
        // Shader.SetGlobalFloat("_FogNoiseStrength", fogNoiseStrength.value);
    }
}