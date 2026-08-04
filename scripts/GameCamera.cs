using Godot;

/// <summary>
/// Trauma-based screen shake.
///
/// The camera parks itself on the viewport centre every frame so world space
/// still equals screen space — spawn positions, the player's on-screen clamp and
/// the crosshair all assume that, and the stretch aspect is "expand", so the
/// real viewport is not always the 1920x1080 base.
///
/// Shake is driven by <c>trauma</c> rather than a duration: callers add trauma,
/// it decays linearly, and the offset uses trauma squared so a stray chip hit
/// barely registers while a death lands hard. Overlapping kills accumulate
/// instead of restarting a tween.
/// </summary>
public partial class GameCamera : Camera2D
{
	[Export] public float MaxOffset { get; set; } = 26.0f;
	[Export] public float MaxRoll { get; set; } = 0.022f;
	/// <summary>Trauma lost per second. One full-trauma shake lasts ~0.55 s.</summary>
	[Export] public float TraumaDecay { get; set; } = 1.8f;
	/// <summary>How fast the shake noise is traversed. Higher is buzzier.</summary>
	[Export] public float Frequency { get; set; } = 26.0f;

	private FastNoiseLite noise;
	private float trauma;
	private float noiseTime;

	public override void _Ready()
	{
		// Shake must freeze with the game, not keep rattling behind the pause menu.
		ProcessMode = ProcessModeEnum.Pausable;
		MakeCurrent();

		// Deliberately not RunState.Rng: this only varies the shake texture's
		// look, never an orbit outcome, so it stays off the seeded stream Daily
		// Alignment fixes.
		noise = new FastNoiseLite
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Seed = (int)GD.Randi(),
			Frequency = 1.0f
		};

		AddToGroup("game_camera");
	}

	/// <summary>Adds shake. 0.2 is a light kill, 0.5 a heavy one, 1.0 a death.</summary>
	public void AddTrauma(float amount)
	{
		trauma = Mathf.Min(trauma + Mathf.Max(amount, 0f), 1.0f);
	}

	public override void _Process(double delta)
	{
		Position = GetViewportRect().Size * 0.5f;

		if (trauma <= 0f)
		{
			// Only write when there is something to clear, so a zero-shake
			// setting leaves the transform completely untouched.
			if (Offset != Vector2.Zero || Rotation != 0f)
			{
				Offset = Vector2.Zero;
				Rotation = 0f;
			}
			return;
		}

		trauma = Mathf.Max(trauma - TraumaDecay * (float)delta, 0f);
		noiseTime += (float)delta * Frequency;

		float intensity = GameSettings.Instance?.ShakeIntensity ?? 1.0f;
		float shake = trauma * trauma * intensity;

		// Three separate noise rows so the axes and the roll never move together.
		Offset = new Vector2(
			noise.GetNoise2D(0.0f, noiseTime),
			noise.GetNoise2D(137.0f, noiseTime)
		) * MaxOffset * shake;

		Rotation = noise.GetNoise2D(311.0f, noiseTime) * MaxRoll * shake;
	}
}
