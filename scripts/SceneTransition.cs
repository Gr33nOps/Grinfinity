using Godot;
using System.Threading.Tasks;

public partial class SceneTransition : CanvasLayer
{
	public static SceneTransition Instance; 

	private AnimationPlayer anim;

	public override void _Ready()
	{
		Instance = this; 
		anim = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public async void ChangeScene(string targetScene)
	{
		anim.Play("dissolve");
		await ToSignal(anim, "animation_finished");

		GetTree().ChangeSceneToFile(targetScene);

		anim.PlayBackwards("dissolve");
	}
}
