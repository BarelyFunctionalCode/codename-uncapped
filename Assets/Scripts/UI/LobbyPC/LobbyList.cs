using System.Collections.Generic;
using Steamworks.Data;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyList : CustomUIElementBase
{
    VisualElement lobbyListContainer;


    public void Initialize()
    {
        lobbyListContainer = this.Q("lobby-list-container");
        if (GameManager.Instance.usingSteam) schedule.Execute(RefreshLobbyListUI).Every(5000);
    }

    public async void RefreshLobbyListUI()
    {
        // Get updated list of lobbies
        await GameManager.Instance.RefreshLobbies();
        List<Lobby> Lobbies = GameManager.Instance.Lobbies;

        // Clear the lobby list
        lobbyListContainer.Clear();

        // Create a new lobby list
        foreach (var lobby in Lobbies)
        {
            LobbyListEntry lobbyListEntry = (LobbyListEntry)UIManager.Spawn("UI/LobbyPC/LobbyListEntry", lobbyListContainer);
            lobbyListEntry.Initialize(lobby);
        }
    }
}