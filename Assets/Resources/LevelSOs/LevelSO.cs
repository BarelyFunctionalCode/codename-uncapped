using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Level/Level")]
public class LevelSO : ScriptableObject
{
    public string displayName;
    public string description;
    public string sceneName;
}
