using UnityEngine;
using UnityEngine.UIElements;

public struct GlitchEffectData
{
    public float effectRatio;
    public float fadeFactor;
    public float transitionSpeed;
    public int pixelSize;
    public Color effectColor;

    public GlitchEffectData(float effectRatio = 0f, float fadeFactor = 0f, float transitionSpeed = 0f, int pixelSize = 32, Color effectColor = default)
    {
        this.effectRatio = effectRatio;
        this.fadeFactor = fadeFactor;
        this.transitionSpeed = transitionSpeed;
        this.pixelSize = pixelSize;
        this.effectColor = effectColor;
    }
}


[UxmlElement(libraryPath = "HUD")]
public partial class GlitchScreenEffect : CustomUIElementBase
{
    private Material glitchMaterial;

    private float targetEffectRatio = 0f;
    private float effectTransitionSpeed = 2f;
    private float effectDecayFactor = 0.2f;

    public GlitchScreenEffect()
    {
        glitchMaterial = new Material(Shader.Find("Shader Graphs/GlitchScreenEffect"));
        style.unityMaterial = glitchMaterial;

        glitchMaterial.SetFloat("_EffectRatio", 0f);
    }

    public void Initialize()
    {
        SendToBack(); // Ensure the effect is behind other UI elements
    }

    public void SetGlitchEffect(GlitchEffectData data)
    {
        targetEffectRatio += data.effectRatio;
        effectTransitionSpeed = data.transitionSpeed <= 0f ? effectTransitionSpeed : data.transitionSpeed;

        glitchMaterial.SetFloat("_FadeFactor", data.fadeFactor);
        glitchMaterial.SetFloat("_PixelSize", data.pixelSize);
        glitchMaterial.SetColor("_EffectColor", data.effectColor);

        schedule.Execute(Update).Pause();
        Update();
    }

    private void Update()
    {
        float currentEffectRatio = glitchMaterial.GetFloat("_EffectRatio");
        if (Mathf.Approximately(currentEffectRatio, targetEffectRatio))
        {
            if (targetEffectRatio > 0f)
            {
                targetEffectRatio = Mathf.Max(0f, targetEffectRatio - Time.deltaTime * effectTransitionSpeed * effectDecayFactor);
            }
            else
            {
                glitchMaterial.SetFloat("_EffectRatio", 0f);
                return;
            }
        }

        float newEffectRatio = Mathf.MoveTowards(currentEffectRatio, targetEffectRatio, effectTransitionSpeed * Time.deltaTime);
        glitchMaterial.SetFloat("_EffectRatio", newEffectRatio);

        schedule.Execute(Update).ExecuteLater(16); // Approx. 60 FPS
    }
}
