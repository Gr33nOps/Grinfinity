using Godot;

public partial class EnemySpawner : Node
{
	[Export] public float StartSpeed { get; set; } = 100.0f;
	[Export] public float MaxSpeed { get; set; } = 200.0f;
	[Export] public float SpeedIncreasePerSecond { get; set; } = 1.0f;
	[Export] public float StartSpawnInterval { get; set; } = 1.5f;
	[Export] public float MinSpawnInterval { get; set; } = 0.5f;
	[Export] public int MaxEnemyCount { get; set; } = 100;
	[Export] public float SpawnMargin { get; set; } = 100.0f;
	[Export] public PackedScene EnemyScene { get; set; }

	/// <summary>Current ramped chase speed, read by living enemies each frame.</summary>
	public static float CurrentSpeed { get; private set; } = 100.0f;

	private Timer spawnTimer;
	private Texture2D[] enemyTextures;
	private float enemySpeed;

	public override void _Ready()
	{
		enemySpeed = StartSpeed;
		CurrentSpeed = StartSpeed;
		LoadEnemyResources();
		SetupSpawnTimer();
	}

	public override void _Process(double delta)
	{
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
		spawnTimer.Timeout += SpawnEnemy;
		AddChild(spawnTimer);
	}

	private void SpawnEnemy()
	{
		if (EnemyScene == null)
			return;

		if (GetTree().GetNodeCountInGroup("enemies") >= MaxEnemyCount)
			return;

		if (EnemyScene.Instantiate() is not Enemy enemy)
			return;

		enemy.GlobalPosition = GetSpawnPosition();
		enemy.SetSpeed(enemySpeed);
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
