using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    [SerializeField] private GameObject titleBackgroundObj;
    [SerializeField] private GameObject titleTextObj;

    private bool loaded = false;

    private float screenWipeSpeed = 1.0f;
    private float fadeDelay = 4.0f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!loaded) return;

        titleBackgroundObj.GetComponent<Image>().fillAmount -= Time.deltaTime * screenWipeSpeed;

        if (titleBackgroundObj.GetComponent<Image>().fillAmount <= 0.0f)
        {
            fadeDelay -= Time.deltaTime;
            if (fadeDelay <= 0.0f)
            {
                titleTextObj.GetComponent<TMP_Text>().color = new Color(
                    titleTextObj.GetComponent<TMP_Text>().color.r,
                    titleTextObj.GetComponent<TMP_Text>().color.g,
                    titleTextObj.GetComponent<TMP_Text>().color.b,
                    Mathf.Max(0.0f, titleTextObj.GetComponent<TMP_Text>().color.a - Time.deltaTime)
                );
                if (titleTextObj.GetComponent<TMP_Text>().color.a <= 0.0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    public void IsLoaded()
    {
        if (loaded) return;
        loaded = true;
    }
}
