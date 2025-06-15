using Godot;
public partial class EnemySpawner : Node
{
	private PackedScene enemyScene;
	private Timer spawnTimer;
	private Texture2D[] enemyTextures;
	private float enemySpeed = 100.0f; 
	private float speedIncrease = 1.0f; 
	private float spawnInterval = 1.0f; 
	private float spawnMargin = 100.0f;
	private bool isPaused = false;
	
	public override void _Ready()
	{
		LoadEnemyResources();
		SetupSpawnTimer();
	}
	
	public override void _Process(double delta)
	{
		if (!isPaused)
		{
			enemySpeed += speedIncrease * (float)delta;
			if (enemySpeed > 200.0f)
				enemySpeed = 200.0f;
			
			float newInterval = 1.5f - ((enemySpeed - 100.0f) / 100.0f); 
			if (newInterval < 0.5f)
				newInterval = 0.5f;
			
			spawnTimer.WaitTime = newInterval;
		}
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
		if (side == 0) {
			return new Vector2(GD.RandRange(0, (int)viewportSize.X), -spawnMargin);
		}
		else if (side == 1)
		{
			return new Vector2(viewportSize.X + spawnMargin, GD.RandRange(0, (int)viewportSize.Y));
		}
		else if (side == 2)
		{
			return new Vector2(GD.RandRange(0, (int)viewportSize.X), viewportSize.Y + spawnMargin);
		}
		else
		{
			return new Vector2(-spawnMargin, GD.RandRange(0, (int)viewportSize.Y));
		}
	}
	
	public void SetPaused(bool paused)
	{
		isPaused = paused; 
		spawnTimer.Paused = paused;
		var enemies = GetTree().GetNodesInGroup("enemies");
		foreach (Node enemy in enemies)
		{
			enemy.SetProcess(!paused);
			enemy.SetPhysicsProcess(!paused);
		}
	}
}
