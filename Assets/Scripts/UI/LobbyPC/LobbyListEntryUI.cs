using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListEntryUI : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text lobbyNameTextObj;
    [SerializeField] private TMP_Text playerCountTextObj;

    private LobbyMenuUI lobbyMenuUI;
    private ulong lobbyId;

    private void Awake() => selectButton.onClick.AddListener(Select);

    private void OnDestroy() => selectButton.onClick.RemoveListener(Select);

    private void Start() => lobbyMenuUI = FindAnyObjectByType<LobbyMenuUI>();

    public void Initialize(string lobbyName, ulong lobbyId, int playerCount, int maxPlayers)
    {
        // Set button info
        lobbyNameTextObj.text = lobbyName;
        playerCountTextObj.text = $"{playerCount}/{maxPlayers}";
        this.lobbyId = lobbyId;
    }

    public void Select() => lobbyMenuUI.JoinSteamLobby(lobbyId);
}
