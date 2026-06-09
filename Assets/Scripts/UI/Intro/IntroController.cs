using UnityEngine;
using UnityEngine.UIElements;


public class IntroController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private AudioSource buttonPressSoundSource;
    [SerializeField] private AudioSource humSoundSource;

    private Intro intro;

    private float humTargetVolume = 1f;
    private float humFadeInDuration = 2f;
    private float humFadeInTimer = 0f;

    private bool isPlayingHum = false;
    private bool introFinished = false;
    private bool loaded = false;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        intro = uiDocument.rootVisualElement.Q<Intro>();
        intro.Initialize(this);
    }

    private void Update()
    {
        if (isPlayingHum && humFadeInTimer < humFadeInDuration) humFadeInTimer += Time.deltaTime;
        if (!isPlayingHum && humFadeInTimer > 0f) humFadeInTimer -= Time.deltaTime;
        humSoundSource.volume = Mathf.Lerp(0f, humTargetVolume, humFadeInTimer / humFadeInDuration);
    }

    public void IntroFinished()
    {
        introFinished = true;
        if (loaded) TriggerFadeOut();
    }

    public void IsLoaded()
    {
        if (loaded) return;
        loaded = true;
        if (introFinished) TriggerFadeOut();
    }

    public void TriggerFadeOut() => intro.Deinitialize();
    public void DestroyIntro() => Destroy(gameObject);

    public void PlayButtonPressSound() => buttonPressSoundSource.Play();
    public void StartHum() => isPlayingHum = true;
    public void StopHum() => isPlayingHum = false;
}
