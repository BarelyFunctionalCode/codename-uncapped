using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyListEntry : CustomUIElementBase
{
    private Label lobbyNameLabel;
    private Label playerCountLabel;
    private Button joinButton;

    private ulong lobbyId;


    public void Initialize(string lobbyName, ulong lobbyId, int playerCount, int maxPlayers)
    {
        lobbyNameLabel = this.Q<Label>("lobby-name");
        playerCountLabel = this.Q<Label>("player-count");
        joinButton = this.Q<Button>("join-button");
        
        lobbyNameLabel.text = lobbyName;
        playerCountLabel.text = $"{playerCount}/{maxPlayers}";
        this.lobbyId = lobbyId;

        joinButton.clicked += JoinLobby;
    }

    private void JoinLobby()
    {
        GameManager.Instance.SetSelectedLobbyId(lobbyId);
        GameManager.Instance.PrepJoiningOtherLobby();
    }
}