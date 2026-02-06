using System.Collections.Generic;
using Steamworks.Data;
using UnityEngine;

public class LobbyMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject lobbyListContainerObj;
    [SerializeField] private GameObject lobbyListEntryPrefab;

    private bool usingSteam = false;
    private float refreshLobbyListInterval = 5f;

    void Start()
    {
        if (GameManager.Instance.usingSteam)
        {
            usingSteam = true;
            RefreshLobbyListUI();
        }
    }

    private void Update()
    {
        if (!usingSteam) return;

        refreshLobbyListInterval -= Time.deltaTime;
        if (refreshLobbyListInterval <= 0)
        {
            RefreshLobbyListUI();
            refreshLobbyListInterval = 5f;
        }
    }

    public void JoinSteamLobby(ulong id) {
        GameManager.Instance.SetSelectedLobbyId(id);
        GameManager.Instance.PrepJoiningOtherLobby();
    }

    public async void RefreshLobbyListUI()
    {
        // Get updated list of lobbies
        await GameManager.Instance.RefreshLobbies();
        List<Lobby> Lobbies = GameManager.Instance.Lobbies;

        // Clear the lobby list
        foreach (Transform child in lobbyListContainerObj.transform)
        {
            if (!child.transform.GetComponent<LobbyListEntryUI>()) continue;
            Destroy(child.gameObject);
        }

        // Create a new lobby list
        foreach (var lobby in Lobbies)
        {
            GameObject lobbyListItem = Instantiate(lobbyListEntryPrefab, lobbyListContainerObj.transform);
            lobbyListItem.transform.SetAsFirstSibling();
            lobbyListItem.GetComponent<LobbyListEntryUI>().Initialize(lobby.GetData("name"), lobby.Id);
        }
    }
}