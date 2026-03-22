using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class Intro : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private TimelineAsset fadeTimeline;
    private bool introFinished = false;
    private bool loaded = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
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

    public void TriggerFadeOut()
    {
        playableDirector.Play(fadeTimeline);
    }

    public void DestroyIntro()
    {
        Destroy(gameObject);
    }
}
