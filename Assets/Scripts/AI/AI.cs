using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    private static Dictionary<ulong, AI> _list = new();
    public static IReadOnlyDictionary<ulong, AI> List => _list;

    public PlayerSettings settings;

    public Character Character { get; protected set; }
    protected bool controlsEnabled = false;

    public virtual void Add(Character character, ulong characterId)
    {
        Character = character;
        _list.Add(characterId, this);
    }

    public void Initialize()
    {
        settings = new PlayerSettings();
        OnInitialized();
        CharacterManager.Instance.CompletedAIInitialization(Character.identification.FetchEntityId());
    }

    protected virtual void OnInitialized() {}

    public void EnableControls() => controlsEnabled = true;
    public void DisableControls() => controlsEnabled = false;
    public void SetControls(bool enabled)
    {
        if (enabled) EnableControls();
        else DisableControls();
    }
}
