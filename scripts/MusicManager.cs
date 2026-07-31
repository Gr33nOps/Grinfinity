using Godot;

/// <summary>
/// Autoloaded music. Keeps the base track running across scene changes and
/// drives a danger-reactive intensity mix on top of it.
///
/// Danger is read from the live game — how long the orbit has run and how
/// crowded the arena is — so the music rises with the run rather than on a
/// fixed timeline. Outside gameplay it settles back to calm.
/// </summary>
public partial class MusicManager : Node
{
	/// <summary>Second stem. Optional: the mix falls back to volume swell without it.</summary>
	private const string IntenseStreamPath = "res://sounds/music_intense.ogg";

	[Export] public float DangerRampSeconds { get; set; } = 100.0f;
	[Export] public int DangerCrowd { get; set; } = 26;
	/// <summary>How fast the mix chases the current danger, in mix units per second.</summary>
	[Export] public float MixSpeed { get; set; } = 0.35f;

	[ExportGroup("Levels")]
	[Export] public float BaseCalmDb { get; set; } = -13.0f;
	[Export] public float BaseIntenseDb { get; set; } = -8.0f;
	[Export] public float LayerSilentDb { get; set; } = -45.0f;
	[Export] public float LayerIntenseDb { get; set; } = -9.0f;

	private AudioStreamPlayer baseTrack;
	private AudioStreamPlayer intenseTrack;
	private float mix;

	public override void _Ready()
	{
		baseTrack = GetNodeOrNull<AudioStreamPlayer>("BackgroundMusic");
		SetupIntenseLayer();
		ApplyMix();
	}

	// Both stems start together and stay together; only their levels move. That
	// is the only way a crossfade stays phase-aligned over a long session.
	private void SetupIntenseLayer()
	{
		if (!ResourceLoader.Exists(IntenseStreamPath))
			return;

		var stream = GD.Load<AudioStream>(IntenseStreamPath);
		if (stream == null)
			return;

		intenseTrack = new AudioStreamPlayer
		{
			Stream = stream,
			Bus = "Music",
			VolumeDb = LayerSilentDb,
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(intenseTrack);
		intenseTrack.Play();
	}

	public override void _Process(double delta)
	{
		float target = ReadDanger();
		mix = Mathf.MoveToward(mix, target, MixSpeed * (float)delta);
		ApplyMix();
	}

	/// <returns>0 when calm, 1 at full intensity.</returns>
	private float ReadDanger()
	{
		var manager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		if (manager == null)
			return 0f;

		float byTime = manager.RunTime / Mathf.Max(DangerRampSeconds, 1f);
		float byCrowd = GetTree().GetNodeCountInGroup("enemies") / (float)Mathf.Max(DangerCrowd, 1);

		return Mathf.Clamp(Mathf.Max(byTime, byCrowd), 0f, 1f);
	}

	private void ApplyMix()
	{
		if (baseTrack != null)
			baseTrack.VolumeDb = Mathf.Lerp(BaseCalmDb, BaseIntenseDb, mix);

		if (intenseTrack != null)
			intenseTrack.VolumeDb = Mathf.Lerp(LayerSilentDb, LayerIntenseDb, mix);
	}
}
