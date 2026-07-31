using Godot;

public partial class EnemySpawner : Node
{
	// Tuned for a 2-6 minute orbit: the ramp tops out around 2:00, which is when
	// the spawn interval is also at its floor, so the back half is flat-out.
	[Export] public float StartSpeed { get; set; } = 110.0f;
	[Export] public float MaxSpeed { get; set; } = 215.0f;
	[Export] public float SpeedIncreasePerSecond { get; set; } = 0.9f;
	[Export] public float StartSpawnInterval { get; set; } = 1.35f;
	[Export] public float MinSpawnInterval { get; set; } = 0.38f;
	[Export] public int MaxEnemyCount { get; set; } = 90;
	[Export] public float SpawnMargin { get; set; } = 100.0f;
	[Export] public PackedScene EnemyScene { get; set; }

	[ExportGroup("Enemy variety")]
	/// <summary>Seconds before swarmers start appearing.</summary>
	[Export] public float SwarmerUnlockTime { get; set; } = 18.0f;
	/// <summary>Seconds before tanks start appearing.</summary>
	[Export] public float TankUnlockTime { get; set; } = 40.0f;
	[Export] public int SwarmerPackSize { get; set; } = 4;

	/// <summary>Current ramped chase speed, read by living enemies each frame.</summary>
	public static float CurrentSpeed { get; private set; } = 100.0f;

	private Timer spawnTimer;
	private Texture2D[] enemyTextures;
	private float enemySpeed;
	private float elapsed;

	public override void _Ready()
	{
		enemySpeed = StartSpeed;
		CurrentSpeed = StartSpeed;
		elapsed = 0f;
		LoadEnemyResources();
		SetupSpawnTimer();
	}

	public override void _Process(double delta)
	{
		elapsed += (float)delta;
		enemySpeed = Mathf.Min(enemySpeed + SpeedIncreasePerSecond * (float)delta, MaxSpeed);
		CurrentSpeed = enemySpeed;

		float speedRange = Mathf.Max(MaxSpeed - StartSpeed, 0.001f);
		float progress = (enemySpeed - StartSpeed) / speedRange;
		spawnTimer.WaitTime = Mathf.Lerp(StartSpawnInterval, MinSpawnInterval, progress);
	}

	private void LoadEnemyResources()
	{
		EnemyScene ??= GD.Load<PackedScene>("res://scenes/enemy.tscn");

		enemyTextures = new Texture2D[9];
		for (int i = 0; i < enemyTextures.Length; i++)
		{
			enemyTextures[i] = GD.Load<Texture2D>($"res://sprites/enemy {i + 1}.png");
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
		if (EnemyScene == null)
			return;

		if (GetTree().GetNodeCountInGroup("enemies") >= MaxEnemyCount)
			return;

		EnemyKind kind = PickKind();

		if (kind == EnemyKind.Swarmer)
		{
			// Swarmers are only threatening in numbers, so they arrive together.
			Vector2 origin = GetSpawnPosition();
			for (int i = 0; i < SwarmerPackSize; i++)
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
	/// threat is noticeable when it shows up.
	/// </summary>
	private EnemyKind PickKind()
	{
		if (elapsed < SwarmerUnlockTime)
			return EnemyKind.Chaser;

		float roll = GD.Randf();

		if (elapsed < TankUnlockTime)
			return roll < 0.35f ? EnemyKind.Swarmer : EnemyKind.Chaser;

		if (roll < 0.30f)
			return EnemyKind.Swarmer;
		if (roll < 0.45f)
			return EnemyKind.Tank;
		return EnemyKind.Chaser;
	}

	private void SpawnOne(EnemyKind kind, Vector2 position)
	{
		if (EnemyScene.Instantiate() is not Enemy enemy)
			return;

		enemy.Configure(kind);
		enemy.GlobalPosition = position;
		SetRandomTexture(enemy);
		GameManager.Spawn(this, enemy);
	}

	private void SetRandomTexture(Enemy enemy)
	{
		var sprite = enemy.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite == null || enemyTextures.Length == 0)
			return;

		int randomIndex = GD.RandRange(0, enemyTextures.Length - 1);
		sprite.Texture = enemyTextures[randomIndex];

		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		if (enemy.GlobalPosition.X > viewportSize.X || enemy.GlobalPosition.Y > viewportSize.Y)
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
