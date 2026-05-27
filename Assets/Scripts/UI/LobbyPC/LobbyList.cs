using System.Collections.Generic;
using Steamworks.Data;
using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyList : CustomUIElementBase
{
    public void Initialize()
    {
        if (GameManager.Instance.usingSteam) schedule.Execute(RefreshLobbyListUI).Every(5000);
    }

    public async void RefreshLobbyListUI()
    {
        // Get updated list of lobbies
        await GameManager.Instance.RefreshLobbies();
        List<Lobby> Lobbies = GameManager.Instance.Lobbies;

        // Clear the lobby list
        foreach (var element in Children())
        {
            element.RemoveFromHierarchy();
        }

        // Create a new lobby list
        foreach (var lobby in Lobbies)
        {
            LobbyListEntry lobbyListEntry = (LobbyListEntry)UIManager.Spawn("UI/LobbyPC/LobbyListEntry", this);
            lobbyListEntry.Initialize(lobby.GetData("name"), lobby.Id, lobby.MemberCount, lobby.MaxMembers);
        }
    }
}