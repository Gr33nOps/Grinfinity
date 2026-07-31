using Godot;

public partial class BodySpawner : Node
{
	// Tuned for a 2-6 minute orbit: the ramp tops out around 2:00, which is when
	// the spawn interval is also at its floor, so the back half is flat-out.
	[Export] public float StartSpeed { get; set; } = 110.0f;
	[Export] public float MaxSpeed { get; set; } = 215.0f;
	[Export] public float SpeedIncreasePerSecond { get; set; } = 0.9f;
	[Export] public float StartSpawnInterval { get; set; } = 1.35f;
	[Export] public float MinSpawnInterval { get; set; } = 0.38f;
	[Export] public int MaxBodyCount { get; set; } = 90;
	[Export] public float SpawnMargin { get; set; } = 100.0f;
	[Export] public PackedScene BodyScene { get; set; }

	[ExportGroup("Bestiary")]
	// One new kind roughly every 20 seconds. Each arrival is meant to be
	// noticeable, and each one teaches something the previous ones did not.
	[Export] public float ShardUnlockTime { get; set; } = 18.0f;
	[Export] public float PlanetoidUnlockTime { get; set; } = 40.0f;
	[Export] public float FractureUnlockTime { get; set; } = 62.0f;
	[Export] public float BulwarkUnlockTime { get; set; } = 85.0f;
	[Export] public float SatelliteUnlockTime { get; set; } = 108.0f;
	[Export] public float FlareUnlockTime { get; set; } = 130.0f;
	[Export] public int ShardPackSize { get; set; } = 4;

	[ExportGroup("Mass response")]
	/// <summary>Spawn interval multiplier at full world mass. Under 1 means faster.</summary>
	[Export] public float HeavySpawnRate { get; set; } = 0.78f;

	/// <summary>Current ramped base speed, read by living bodies each frame.</summary>
	public static float CurrentSpeed { get; private set; } = 100.0f;

	/// <summary>The ramp as a multiplier on its starting value, for scaling pull.</summary>
	public static float SpeedScale { get; private set; } = 1.0f;

	private Timer spawnTimer;
	private Texture2D[] bodyTextures;
	private RunState run;
	private float enemySpeed;
	private float elapsed;

	public override void _Ready()
	{
		enemySpeed = StartSpeed;
		CurrentSpeed = StartSpeed;
		SpeedScale = 1.0f;
		elapsed = 0f;
		run = GameManager.Of(this)?.Run;
		LoadBodyResources();
		SetupSpawnTimer();
	}

	public override void _Process(double delta)
	{
		elapsed += (float)delta;
		enemySpeed = Mathf.Min(enemySpeed + SpeedIncreasePerSecond * (float)delta, MaxSpeed);
		CurrentSpeed = enemySpeed;
		SpeedScale = enemySpeed / Mathf.Max(StartSpeed, 1f);

		float speedRange = Mathf.Max(MaxSpeed - StartSpeed, 0.001f);
		float progress = (enemySpeed - StartSpeed) / speedRange;

		// A heavy world pulls harder, so it also draws more attention: mass
		// tightens the spawn interval on top of the time ramp.
		float massRate = Mathf.Lerp(1.0f, HeavySpawnRate, run?.MassNormalised ?? 0f);
		spawnTimer.WaitTime = Mathf.Lerp(StartSpawnInterval, MinSpawnInterval, progress) * massRate;
	}

	private void LoadBodyResources()
	{
		BodyScene ??= GD.Load<PackedScene>("res://scenes/body.tscn");

		bodyTextures = new Texture2D[9];
		for (int i = 0; i < bodyTextures.Length; i++)
		{
			bodyTextures[i] = GD.Load<Texture2D>($"res://sprites/enemy {i + 1}.png");
		}
	}

	private void SetupSpawnTimer()
	{
		spawnTimer = new Timer
		{
			WaitTime = StartSpawnInterval,
			Autostart = true
		};
		spawnTimer.Timeout += OnSpawnTimeout;
		AddChild(spawnTimer);
	}

	private void OnSpawnTimeout()
	{
		if (BodyScene == null)
			return;

		// A boss fight is about the boss. Trash on top of it would only make the
		// safe gaps unreadable, which is the one thing The Coil teaches.
		if (GameManager.Of(this)?.BossActive == true)
			return;

		if (GetTree().GetNodeCountInGroup("bodies") >= MaxBodyCount)
			return;

		BodyKind kind = PickKind();

		if (kind == BodyKind.Shard)
		{
			// Shards are only threatening in numbers, so they arrive together.
			Vector2 origin = GetSpawnPosition();
			for (int i = 0; i < ShardPackSize; i++)
			{
				Vector2 jitter = new Vector2(GD.RandRange(-90, 90), GD.RandRange(-90, 90));
				SpawnOne(kind, origin + jitter);
			}
			return;
		}

		SpawnOne(kind, GetSpawnPosition());
	}

	/// <summary>
	/// Introduces kinds over time so the opening stays readable and each new
	/// threat is noticeable when it shows up. Weights are cumulative bands over
	/// a single roll; anything not claimed by a band falls through to a Drifter.
	/// </summary>
	private BodyKind PickKind()
	{
		if (elapsed < ShardUnlockTime)
			return BodyKind.Drifter;

		float roll = GD.Randf();
		float band = 0f;

		if (Unlocked(FlareUnlockTime) && roll < (band += 0.10f))
			return BodyKind.Flare;

		if (Unlocked(SatelliteUnlockTime) && roll < (band += 0.10f))
			return BodyKind.Satellite;

		if (Unlocked(BulwarkUnlockTime) && roll < (band += 0.12f))
			return BodyKind.Bulwark;

		if (Unlocked(FractureUnlockTime) && roll < (band += 0.13f))
			return BodyKind.Fracture;

		if (Unlocked(PlanetoidUnlockTime) && roll < (band += 0.13f))
			return BodyKind.Planetoid;

		if (roll < band + 0.26f)
			return BodyKind.Shard;

		return BodyKind.Drifter;
	}

	private bool Unlocked(float at) => elapsed >= at;

	private void SpawnOne(BodyKind kind, Vector2 position)
	{
		if (BodyScene.Instantiate() is not Body body)
			return;

		body.Configure(kind);
		body.GlobalPosition = position;
		SetRandomTexture(body);
		GameManager.Spawn(this, body);
	}

	private void SetRandomTexture(Body body)
	{
		var sprite = body.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite == null || bodyTextures.Length == 0)
			return;

		// Most kinds pin a face so they stay recognisable between orbits; only
		// the baseline Drifter is left to vary.
		int index = body.TextureIndex >= 0
			? Mathf.Min(body.TextureIndex, bodyTextures.Length - 1)
			: GD.RandRange(0, Mathf.Min(2, bodyTextures.Length - 1));
		sprite.Texture = bodyTextures[index];

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		if (body.GlobalPosition.X > viewportSize.X || body.GlobalPosition.Y > viewportSize.Y)
		{
			sprite.FlipV = true;
			sprite.FlipH = true;
		}
	}

	private Vector2 GetSpawnPosition()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		int side = GD.RandRange(0, 3);
		return side switch
		{
			0 => new Vector2(GD.RandRange(0, (int)viewportSize.X), -SpawnMargin),
			1 => new Vector2(viewportSize.X + SpawnMargin, GD.RandRange(0, (int)viewportSize.Y)),
			2 => new Vector2(GD.RandRange(0, (int)viewportSize.X), viewportSize.Y + SpawnMargin),
			_ => new Vector2(-SpawnMargin, GD.RandRange(0, (int)viewportSize.Y))
		};
	}
}
