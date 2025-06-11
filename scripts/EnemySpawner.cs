using Godot;

public partial class EnemySpawner : Node
{
	private PackedScene enemyScene;
	private Timer spawnTimer;
	private Texture2D[] enemyTextures;
	private float enemySpeed = 100.0f;
	private float speedIncrease = 1.67f;
	private float spawnInterval = 0.8f;
	private float spawnMargin = 100.0f;
	
	public override void _Ready()
	{
		LoadEnemyResources();
		SetupSpawnTimer();
	}
	
	public override void _Process(double delta)
	{
		enemySpeed += speedIncrease * (float)delta;
		float maxSpeed = Mathf.Min(enemySpeed, 300.0f);
		float newInterval = Mathf.Max(0.1f, 1.2f - (maxSpeed / 300.0f));
		spawnTimer.WaitTime = newInterval;
	}
	
	private void LoadEnemyResources()
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
	
	private void SetupSpawnTimer()
	{
		spawnTimer = new Timer();
		spawnTimer.WaitTime = spawnInterval;
		spawnTimer.Timeout += SpawnEnemy;
		AddChild(spawnTimer);
		spawnTimer.Start();
	}
	
	private void SpawnEnemy()
	{
		if (enemyScene == null) return;
		
		var enemy = enemyScene.Instantiate() as Enemy;
		if (enemy == null) return;
		
		enemy.GlobalPosition = GetSpawnPosition();
		enemy.SetSpeed(enemySpeed);
		
		SetRandomTexture(enemy);
		GetTree().CurrentScene.AddChild(enemy);
	}
	
	private void SetRandomTexture(Enemy enemy)
	{
		var sprite = enemy.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (enemyTextures.Length > 0 && sprite != null)
		{
			int randomIndex = GD.RandRange(0, enemyTextures.Length - 1);
			sprite.Texture = enemyTextures[randomIndex];
			
			Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
			if (enemy.GlobalPosition.X > viewportSize.X || enemy.GlobalPosition.Y > viewportSize.Y)
			{
				sprite.FlipV = true;
				sprite.FlipH = true;
			}
		}
	}
	
	private Vector2 GetSpawnPosition()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		int side = GD.RandRange(0, 3);
		
		return side switch
		{
			0 => new Vector2(GD.RandRange(0, (int)viewportSize.X), -spawnMargin),
			1 => new Vector2(viewportSize.X + spawnMargin, GD.RandRange(0, (int)viewportSize.Y)),
			2 => new Vector2(GD.RandRange(0, (int)viewportSize.X), viewportSize.Y + spawnMargin),
			3 => new Vector2(-spawnMargin, GD.RandRange(0, (int)viewportSize.Y)),
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
