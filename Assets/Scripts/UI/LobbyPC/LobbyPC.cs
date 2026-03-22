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

    private float camToCanvasDistance;
    private bool showInteractPrompt = false;
    private bool isActive = false;
    private bool autoInteract = true;

    [SerializeField] private Button activeTabButton;
    [SerializeField] private GameObject matchConfigurationContainerObj;
    [SerializeField] private GameObject lobbiesListContainerObj;

    private PlayerController interactingPlayerController;

    private float activeTabButtonAlpha = 0.4f;
    private float inactiveTabButtonAlpha;


    void Awake()
    {
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

    void Update()
    {
        if (autoInteract && GameManager.Instance.usingSteam && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Interact();
            Intro possibleIntro = FindAnyObjectByType<Intro>();
            if (possibleIntro != null) possibleIntro.IsLoaded();
        }

        if (cursorObj != null && isActive)
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

    private void Interact()
    {
        if (isActive) return;
        NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().TryGetComponent(out PlayerController playerController);
        if (playerController.isInitialized == false) return;
        playerController.SetPlayerControlsRpc(false);
        interactingPlayerController = playerController;
        interactingPlayerController.playerHUD.ToggleHUD();

        // Sets priority to PC Cam and then unlocks the cursor
        Camera.main.cullingMask = noPlayerMask;
        pcCam.Priority.Value = 99;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        interactingPlayerController.SetPlayerControlsRpc(true);
        interactingPlayerController.playerHUD.ToggleHUD();
        interactingPlayerController = null;
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

        other.TryGetComponent(out PlayerController playerController);
        if (playerController != null && playerController.IsLocalPlayer)
        {
            interactPromptObj.SetActive(true);
            showInteractPrompt = true;
        }
    }
}