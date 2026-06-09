using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyCharacterEntry : CustomUIElementBase
{
    public Character Character { get; private set; }
    private Label characterNameLabel;

    public LobbyCharacterEntry()
    {
        characterNameLabel = new Label()
        {
            name = "character-name",
            text = "Character Name"
        };
        Add(characterNameLabel);
    }

    public void Initialize(Character character)
    {
        Character = character;

        Identification entityIdentification = character.identification;

        string characterName = entityIdentification.FetchEntityName();
        characterNameLabel.text = characterName;

        int teamId = entityIdentification.FetchTeamId();
    }
}