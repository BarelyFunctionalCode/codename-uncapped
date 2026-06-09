using UnityEngine;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "")]
public partial class Intro : CustomUIElementBase
{
    private VisualElement staticBackground;
    private Label subtitleLabel;

    private IntroController introController;

    private bool isSpawned = false;
    private bool isInitialized = false;
    private bool isFinished = false;


    protected override void OnSpawned()
    {
        base.OnSpawned();
        isSpawned = true;
        if (isInitialized) Start();
    }

    public void Initialize(IntroController introController)
    {
        this.introController = introController;
        isInitialized = true;
        if (isSpawned) Start();
    }

    public void Start()
    {
        staticBackground = this.Q<VisualElement>("static");
        subtitleLabel = this.Q<Label>("subtitle");

        staticBackground.RegisterCallback<TransitionStartEvent>(evt =>
        {
            if (evt.target != staticBackground) return;
            introController.PlayButtonPressSound();
            if (!isFinished) introController.StartHum();
            else introController.StopHum();
        });
        subtitleLabel.RegisterCallback<TransitionEndEvent>(evt =>
        {
            if (evt.target != subtitleLabel) return;
            if (isFinished) return;
            isFinished = true;
            introController.IntroFinished();
        });
        RegisterCallback<TransitionEndEvent>(evt =>
        {
            if (evt.target != this) return;
            if (!isFinished) return;
            introController.DestroyIntro();
        });

        schedule.Execute(() => EnableInClassList("active", true)).ExecuteLater(500);
    }

    public void Deinitialize()
    {
        EnableInClassList("active", false);
        EnableInClassList("fade", true);
    }
}