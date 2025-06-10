using Godot;

public partial class EnemySpawner : Node
{
	private PackedScene enemyScene;
	private Timer spawnTimer;
	private Texture2D[] enemyTextures;
	private ScoreManager scoreManager;
	
	// Global speed that increases over time
	private float globalEnemySpeed = 100.0f;
	private float speedIncreaseRate = 1.67f; // Speed increase per second
	
	private const float SpawnMargin = 100.0f;
	private const float spawnInterval = 0.8f;
	private const int maxEnemyCount = 100;
	
	public override void _Ready()
	{
		LoadResources();
		SetupTimer();
		scoreManager = GetParent().GetNodeOrNull<ScoreManager>("ScoreManager");
	}
	
	public override void _Process(double delta)
	{
		globalEnemySpeed += speedIncreaseRate * (float)delta;

		// Clamp speed effect to max 300 for spawn rate
		float clampedSpeed = Mathf.Min(globalEnemySpeed, 300.0f);

		// Adjust spawn rate (faster spawn as speed increases)
		float dynamicWaitTime = Mathf.Max(0.1f, 1.2f - (clampedSpeed / 300.0f));
		spawnTimer.WaitTime = dynamicWaitTime;;
	}
	
	private void LoadResources()
	{
		enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		enemyTextures = new Texture2D[]
		{
			GD.Load<Texture2D>("res://sprites/enemy 1.png"),
			GD.Load<Texture2D>("res://sprites/enemy 2.png"),
			GD.Load<Texture2D>("res://sprites/enemy 3.png"),
			GD.Load<Texture2D>("res://sprites/enemy 4.png"),
			GD.Load<Texture2D>("res://sprites/enemy 5.png"),
			GD.Load<Texture2D>("res://sprites/enemy 6.png"),
			GD.Load<Texture2D>("res://sprites/enemy 7.png"),
			GD.Load<Texture2D>("res://sprites/enemy 8.png"),
			GD.Load<Texture2D>("res://sprites/enemy 9.png")
		};
	}
	
	private void SetupTimer()
	{
		spawnTimer = new Timer();
		spawnTimer.WaitTime = spawnInterval;
		spawnTimer.Timeout += OnTimerTimeout;
		AddChild(spawnTimer);
		spawnTimer.Start();
	}
	
	private void OnTimerTimeout()
	{
		SpawnEnemy();
	}
	
	private void SpawnEnemy()
	{
		if (enemyScene == null) return;
		
		var enemy = enemyScene.Instantiate() as Enemies;
		if (enemy == null) return;
		
		enemy.GlobalPosition = GetRandomSpawnPositionOutsideViewport();
		
		// CRITICAL: Set the current global speed to the new enemy
		enemy.SetSpeed(globalEnemySpeed);
		
		var sprite = enemy.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (enemyTextures.Length > 0 && sprite != null)
		{
			int randIndex = GD.RandRange(0, enemyTextures.Length - 1);
			if (enemyTextures[randIndex] != null)
			{
				sprite.Texture = enemyTextures[randIndex];
			}
			
			Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
			if (enemy.GlobalPosition.X > viewportSize.X || enemy.GlobalPosition.Y > viewportSize.Y)
			{
				sprite.FlipV = true;
				sprite.FlipH = true;
			}
		}
		
		GetTree().CurrentScene.AddChild(enemy);
	}
	
	private Vector2 GetRandomSpawnPositionOutsideViewport()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		int side = GD.RandRange(0, 3);
		return side switch
		{
			0 => new Vector2(GD.RandRange(0, (int)viewportSize.X), -SpawnMargin),
			1 => new Vector2(viewportSize.X + SpawnMargin, GD.RandRange(0, (int)viewportSize.Y)),
			2 => new Vector2(GD.RandRange(0, (int)viewportSize.X), viewportSize.Y + SpawnMargin),
			3 => new Vector2(-SpawnMargin, GD.RandRange(0, (int)viewportSize.Y)),
			_ => Vector2.Zero
		};
	}
	
	public void SetPaused(bool paused)
	{
		spawnTimer.Paused = paused;
		
		var enemies = GetTree().GetNodesInGroup("enemies");
		foreach (Node enemy in enemies)
		{
			enemy.SetProcess(!paused);
			enemy.SetPhysicsProcess(!paused);
		}
	}
}
