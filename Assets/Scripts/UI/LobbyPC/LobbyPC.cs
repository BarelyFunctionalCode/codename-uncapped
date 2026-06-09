using UnityEngine.UIElements;


[UxmlElement(libraryPath = "LobbyPC")]
public partial class LobbyPC : CustomUIElementBase
{
    private LobbyPCController lobbyPCController;

    private VisualElement interactPrompt;
    private VisualElement autoStartNotice;

    private VisualElement matchSelectionContainer;
    public MatchSelection MatchSelection { get; private set; }

    private VisualElement lobbyListContainer;


    public void Initialize(LobbyPCController lobbyPCController)
    {
        this.lobbyPCController = lobbyPCController;
        interactPrompt = this.Q("interact-prompt");
        autoStartNotice = this.Q("auto-start-notice");
        matchSelectionContainer = this.Q("match-selection-container");
        lobbyListContainer = this.Q("lobby-list-container");

        MatchSelection = (MatchSelection)UIManager.Spawn("UI/LobbyPC/MatchSelection", matchSelectionContainer);
        MatchSelection.Initialize(lobbyPCController);

        LobbyList lobbyList = (LobbyList)UIManager.Spawn("UI/LobbyPC/LobbyList", lobbyListContainer);
        lobbyList.Initialize();
    
        ToggleInteractPrompt(false);
        ToggleAutoStartNotice(false);
    }

    public void ToggleInteractPrompt(bool show)
    {
        interactPrompt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void ToggleAutoStartNotice(bool show)
    {
        autoStartNotice.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

}