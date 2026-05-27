using Steamworks.Data;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyListEntry : CustomUIElementBase
{
    private Label lobbyNameLabel;
    private Label playerCountLabel;
    private Button joinButton;

    private ulong lobbyId;


    public void Initialize(Lobby lobby)
    {
        lobbyNameLabel = this.Q<Label>("lobby-name");
        playerCountLabel = this.Q<Label>("player-count");
        joinButton = this.Q<Button>("join-button");
        
        lobbyNameLabel.text = lobby.GetData("name");
        playerCountLabel.text = $"{lobby.MemberCount}/{lobby.MaxMembers}";
        lobbyId = lobby.Id;

        joinButton.clicked += JoinLobby;
    }

    private void JoinLobby()
    {
        GameManager.Instance.SetSelectedLobbyId(lobbyId);
        GameManager.Instance.PrepJoiningOtherLobby();
    }
}