using Godot;

public partial class Credits : Control
{
	private Button backButton;

	public override void _Ready()
	{
        Callable.From(()=>ArcadeSkin.SupportingScreen(this)).CallDeferred();
		GetNode<Label>("Layout/Body").Text="Design, code and art • Gr33nOps\n\nBuilt with Godot Engine 4.7.1\nMIT License • godotengine.org\n\nLilita One by Juan Montoreano\nSIL Open Font License 1.1\n\nThanks for playing!";
        backButton = GetNode<Button>("Layout/BackButton");
		backButton.Pressed += OnBackPressed;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("pause"))
		{
			OnBackPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
