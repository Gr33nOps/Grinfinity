using Godot;

public partial class SceneTransition : CanvasLayer
{
	public static SceneTransition Instance;

	private AnimationPlayer anim;
	private bool isTransitioning = false;

	public override void _Ready()
	{
		Instance = this;
		// Must keep running while the tree is paused, otherwise a transition
		// started from the pause menu would never finish.
		ProcessMode = ProcessModeEnum.Always;
		anim = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public async void ChangeScene(string targetScene)
	{
		// Guards against a double-click queueing two overlapping transitions.
		if (isTransitioning)
			return;

		isTransitioning = true;

		// A scene must never inherit a stuck pause state from the previous one.
		GetTree().Paused = false;

		anim.Play("dissolve");
		await ToSignal(anim, AnimationMixer.SignalName.AnimationFinished);

		Error error = GetTree().ChangeSceneToFile(targetScene);
		if (error != Error.Ok)
		{
			GD.PushError($"SceneTransition: could not load '{targetScene}' ({error}).");
			anim.PlayBackwards("dissolve");
			isTransitioning = false;
			return;
		}

		// ChangeSceneToFile is deferred to the end of the frame, so the fade-in
		// has to wait a frame or it would play over the outgoing scene.
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		anim.PlayBackwards("dissolve");
		await ToSignal(anim, AnimationMixer.SignalName.AnimationFinished);

		isTransitioning = false;
	}
}
