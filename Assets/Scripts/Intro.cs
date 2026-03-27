using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class Intro : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private AudioTrack musicAudioTrack;
    private bool introFinished = false;
    private bool loaded = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void IntroFinished()
    {
        introFinished = true;
        playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(0);
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
        playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(1);
    }

    public void DestroyIntro()
    {
        Destroy(gameObject);
    }
}
