using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LobbyPC : MonoBehaviour
{
    private Canvas canvas;
    [SerializeField] private CinemachineCamera pcCam;
    [SerializeField] private LayerMask noPlayerMask;

    [SerializeField] private GameObject interactPromptObj;

    [SerializeField] private GameObject cursorObj;
    [SerializeField] public AudioSource musicSource;

    private float camToCanvasDistance;
    private bool showInteractPrompt = false;
    private bool isActive = false;
    private bool autoInteract = true;

    [SerializeField] private Button activeTabButton;
    [SerializeField] private GameObject matchConfigurationContainerObj;
    [SerializeField] private GameObject lobbiesListContainerObj;

    private float activeTabButtonAlpha = 0.4f;
    private float inactiveTabButtonAlpha;

    private bool isInitialized = false;


    void Awake()
    {
        GameManager.Instance.OnLevelLoadedEvent.AddListener(OnLevelLoadedEvent);

        canvas = GetComponentInChildren<Canvas>();
        camToCanvasDistance = pcCam.GetComponent<CinemachinePositionComposer>().CameraDistance;

        interactPromptObj.SetActive(true);
        cursorObj.SetActive(false);
        matchConfigurationContainerObj.SetActive(true);
        lobbiesListContainerObj.SetActive(false);

        Color tabColor = activeTabButton.image.color;
        inactiveTabButtonAlpha = tabColor.a;
        tabColor.a = activeTabButtonAlpha;
        activeTabButton.image.color = tabColor;
    }

    void OnDestroy()
    {
        Reset();
        if (GameManager.Instance) GameManager.Instance.OnLevelLoadedEvent.RemoveListener(OnLevelLoadedEvent);
    }

    void Update()
    {
        if (!isInitialized && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (autoInteract)
            {
                autoInteract = false;
                Interact();
                Intro possibleIntro = FindAnyObjectByType<Intro>();
                if (possibleIntro != null) possibleIntro.IsLoaded();
            }
            isInitialized = true;
        }

        musicSource.spatialBlend = Mathf.Lerp(musicSource.spatialBlend, isActive ? 0f : 1f, Time.deltaTime);

        if (Camera.main != null && cursorObj != null && isActive)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, camToCanvasDistance));
            cursorObj.transform.position = cursorPosition;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame && showInteractPrompt && !isActive)
        {
            Interact();
        }
    }

    private void FixedUpdate()
    {
        interactPromptObj.SetActive(showInteractPrompt && !isActive);
        showInteractPrompt = false;
    }

    private void OnLevelLoadedEvent(string levelName)
    {
        if (levelName == gameObject.scene.name)
        {
            foreach (Character character in CharacterManager.Instance.characters)
            {
                if (character == null) continue;
                
                character.characterInputs.SetHUDActiveRpc(false);
                character.Teleport(Vector3.zero, Quaternion.identity);
            }
        }
    }

    private void Interact()
    {
        if (isActive) return;
        Player.Instance.DisableControls();

        // Sets priority to PC Cam and then unlocks the cursor
        Camera.main.cullingMask = noPlayerMask;
        pcCam.Priority.Value = 99;
        Player.Instance.playerHUD.SetCursorState(true, true);
        cursorObj.SetActive(true);
        isActive = true;
        autoInteract = false;
    }

    public void Reset()
    {
        // Resets priority to PC Cam and then locks the cursor
        isActive = false;
        cursorObj.SetActive(false);
        Camera.main.cullingMask = -1;
        pcCam.Priority.Value = 0;
        Player.Instance.playerHUD.SetCursorState(false);
        Player.Instance.EnableControls();
    }

    public void OnTabButtonSelect(Button button)
    {
        if (button.name == "MatchConfiguration")
        {
            matchConfigurationContainerObj.SetActive(true);
            lobbiesListContainerObj.SetActive(false);
        }
        else if (button.name == "LobbiesList")
        {
            matchConfigurationContainerObj.SetActive(false);
            lobbiesListContainerObj.SetActive(true);
        }
        Color tabColor = activeTabButton.image.color;
        tabColor.a = inactiveTabButtonAlpha;
        activeTabButton.image.color = tabColor;

        activeTabButton = button;
        tabColor = activeTabButton.image.color;
        tabColor.a = activeTabButtonAlpha;
        activeTabButton.image.color = tabColor;
    }

    public void OnExitButtonSelect()
    {
        Reset();
    }

    void OnTriggerStay(Collider other)
    {
        if (isActive) return;

        Character localPlayerCharacter = Player.Instance.Character;
        if (localPlayerCharacter == null) return;
        Character character = other.GetComponentInParent<Character>();
        CharacterPuppet characterPuppet = other.GetComponentInParent<CharacterPuppet>();
        if ((character != null && character == localPlayerCharacter) || characterPuppet != null)
        {
            interactPromptObj.SetActive(true);
            showInteractPrompt = true;
        }
    }
}