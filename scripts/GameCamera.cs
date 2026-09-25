using Godot;

/// <summary>
/// Follows the planet around the arena, and shakes.
///
/// The follow has a small dead zone, so tiny corrections do not drag the whole
/// screen, and then catches up quickly rather than floating. It leans a little
/// toward where the player is aiming, and it never looks past the edge of the
/// world. Every frame it publishes what is on screen to <see cref="Arena.View"/>.
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
	private Player player;
	private Vector2 focus;

	public override void _Ready()
	{
		// Shake must freeze with the game, not keep rattling behind the pause menu.
		ProcessMode = ProcessModeEnum.Pausable;
		MakeCurrent();
		player = GameManager.Of(this)?.GetNodeOrNull<Player>("player");
		focus = player?.GlobalPosition ?? Arena.Centre;
		Follow(0f);

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

	/// <summary>Moves the focus toward the planet and clamps it to the world.</summary>
	private void Follow(float delta)
	{
		Vector2 half = GetViewportRect().Size * 0.5f / Zoom;

		if (player != null && IsInstanceValid(player))
		{
			Vector2 aim = player.AimPosition - player.GlobalPosition;
			Vector2 target = player.GlobalPosition + aim.LimitLength(400f) / 400f * Balance.CameraAimLead;

			// In a boss fight, lean toward the nearest boss so both stay in frame.
			Node2D boss = null;
			foreach (Node node in GetTree().GetNodesInGroup("bosses"))
			{
				if (node is Node2D candidate && (boss == null
					|| candidate.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) < boss.GlobalPosition.DistanceSquaredTo(player.GlobalPosition)))
					boss = candidate;
			}
			if (boss != null)
			{
				// Half as much vertically: the screen is shorter that way, and the
				// planet must never slide up under the boss bar.
				Vector2 lean = ((boss.GlobalPosition - player.GlobalPosition) * Balance.CameraBossLean).LimitLength(Balance.CameraBossLeanMax);
				target += new Vector2(lean.X, lean.Y * 0.5f);
			}
			Vector2 away = target - focus;
			if (away.Length() > Balance.CameraDeadZone)
			{
				Vector2 desired = target - away.Normalized() * Balance.CameraDeadZone;
				focus = delta <= 0f ? desired : focus.Lerp(desired, 1f - Mathf.Exp(-Balance.CameraFollowRate * delta));
			}
		}

		Rect2 world = Arena.World;
		focus = new Vector2(
			world.Size.X <= half.X * 2f ? world.GetCenter().X : Mathf.Clamp(focus.X, half.X, world.End.X - half.X),
			world.Size.Y <= half.Y * 2f ? world.GetCenter().Y : Mathf.Clamp(focus.Y, half.Y, world.End.Y - half.Y));

		Position = focus;
		Arena.View = new Rect2(focus - half, half * 2f);
	}

	public override void _Process(double delta)
	{
		Follow((float)delta);

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
