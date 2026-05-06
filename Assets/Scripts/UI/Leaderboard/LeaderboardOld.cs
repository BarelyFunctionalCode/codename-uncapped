using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardOld : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardEntryPrefabObj;

    [SerializeField] private TMP_Text leaderboardTitleText;

    [SerializeField] private GameObject listColumn0Obj;
    [SerializeField] private TMP_Text listColumn0TitleText;
    [SerializeField] private GameObject listColumn1Obj;
    [SerializeField] private TMP_Text listColumn1TitleText;
    [SerializeField] private GameObject teamSeparatorObj;

    [SerializeField] private GameObject capturesStat0Obj;
    [SerializeField] private GameObject capturesStat1Obj;

    private List<LeaderboardEntryOld> entries = new();
    private bool isTeamBased = false;
    private bool enableCapturesStat;

    private bool isActive = false;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Initialize()
    {
        GameModeHandler.Instance.OnGameModeChanged.AddListener(SetGameModeData);
        GameModeHandler.Instance.TriggerGameModeUpdateRpc(NetworkManager.Singleton.LocalClientId);

        GameModeHandler.Instance.OnStatUpdated.AddListener(OnStatEventReceived);
        GameModeHandler.Instance.TriggerCharactersStatsDumpRpc(NetworkManager.Singleton.LocalClientId);

        CharacterManager.Instance.OnCharacterChangedTeam.AddListener(AddEntry);
        foreach (Character character in CharacterManager.Instance.characters)
        {
            AddEntry(new NetworkBehaviourReference(character));
        }
    }

    public void Deinitialize()
    {
        if (GameModeHandler.Instance)
        {
            GameModeHandler.Instance.OnGameModeChanged.RemoveListener(SetGameModeData);
            GameModeHandler.Instance.OnStatUpdated.RemoveListener(OnStatEventReceived);
        }
        if (CharacterManager.Instance)
        {
            CharacterManager.Instance.OnCharacterChangedTeam.RemoveListener(AddEntry);
        }
        ClearEntries();
    }

    private void SetGameModeData(GameModes g)
    {
        TeamBasedType teamBasedType = GameModeHandler.Instance.FetchTeamBasedType(g);

        leaderboardTitleText.text = g.ToString();
        isTeamBased = teamBasedType == TeamBasedType.TEAM;

        listColumn0TitleText.text = isTeamBased ? GameModeHandler.Instance.currentGameMode.TeamStructure.GetTeamByIndex(0) : "Player";
        listColumn1TitleText.text = isTeamBased ? GameModeHandler.Instance.currentGameMode.TeamStructure.GetTeamByIndex(1) : "";
        listColumn1Obj.SetActive(isTeamBased);
        teamSeparatorObj.SetActive(isTeamBased);

        enableCapturesStat = false; // TODO: Change this to be based on the selected GameModeSO
        capturesStat0Obj.SetActive(enableCapturesStat);
        capturesStat1Obj.SetActive(enableCapturesStat);

        isActive = true;
    }

    public void ToggleMenu(bool enabled)
    {
        if (!isActive) return;
        gameObject.SetActive(enabled);
    }

    private void AddEntry(NetworkBehaviourReference characterRef)
    {
        characterRef.TryGet(out Character character);
        if (character == null) return;
        
        ulong characterId = character.identification.FetchEntityId();
        int teamIndex = character.identification.FetchTeamId();
        if (teamIndex == -1) return;

        LeaderboardEntryOld entryToRemove = entries.Find(entry => entry.characterId == characterId);
        if (entryToRemove != null) RemoveEntry(characterId);
        
        string name = character.identification.FetchEntityName();
        GameObject entryObj = Instantiate(leaderboardEntryPrefabObj, (!isTeamBased || teamIndex == 0) ? listColumn0Obj.transform : listColumn1Obj.transform);
        LeaderboardEntryOld entry = entryObj.GetComponent<LeaderboardEntryOld>();
        entry.Initialize(characterId, name, enableCapturesStat);
        entries.Add(entry);
    }

    private void RemoveEntry(ulong characterId)
    {
        LeaderboardEntryOld entryToRemove = entries.Find(entry => entry.characterId == characterId);
        if (entryToRemove != null)
        {
            Destroy(entryToRemove.gameObject);
            entries.Remove(entryToRemove);
        }
    }

    private void OnStatEventReceived(StatEvent statEvent)
    {
        LeaderboardEntryOld entryToUpdate = entries.Find(entry => entry.characterId == statEvent.Source);
        if (entryToUpdate != null) entryToUpdate.UpdateStats(statEvent);
    }

    public void ClearEntries()
    {
        foreach (LeaderboardEntryOld entry in entries)
        {
            Destroy(entry.gameObject);
        }
        entries.Clear();
    }
}
