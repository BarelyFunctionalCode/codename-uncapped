using UnityEngine.UIElements;


[UxmlElement(libraryPath = "")]
public partial class DeathScreen : CustomUIElementBase
{
    private Label deathMessageLabel;

    public void Initialize()
    {
        deathMessageLabel = this.Q<Label>("death-message");

        EnableInClassList("active", false);
        EnableInClassList("active", true);

        deathMessageLabel.RegisterCallback<TransitionEndEvent>( evt =>
        {
            deathMessageLabel.ToggleInClassList("active");
        });
        schedule.Execute(() => deathMessageLabel.ToggleInClassList("active")).StartingIn(50);
    }
}