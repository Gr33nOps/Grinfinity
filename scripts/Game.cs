// Game.cs
using Godot;
using System;

public partial class Game : Node2D
{
	private PackedScene enemyScene;
	private Node2D player;
	private Sprite2D playerSprite;
	private PauseMenu pauseMenu;
	private GameOverMenu gameOverMenu;
	private const float SpawnMargin = 100.0f;
	private bool isPaused = false;
	private bool isGameOver = false;
	private float survivalTime = 0.0f;
	private Label scoreLabel;
	private Texture2D[] enemyTextures;
	private Texture2D[] playerTextures;
	private float spriteSwapTimer = 0f;
	private Sprite2D crosshair;
	private const float playerSpriteSwapInterval = 5f;
	private const float spawnIntervalStart = 1.0f;
	private const float spawnIntervalMin = 0.4f;
	private const float spawnIntervalRampUpTime = 120f;
	private const int maxEnemyCount = 100;
	
	public override void _Ready()
	{
		enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		player = GetNode<Node2D>("player");
		
		if (player != null)
		{
			if (player.HasNode("Sprite2D"))
			{
				playerSprite = player.GetNode<Sprite2D>("Sprite2D");
			}
			else if (player.HasNode("sprite"))
			{
				playerSprite = player.GetNode<Sprite2D>("sprite");
			}
			else if (player is Sprite2D)
			{
				playerSprite = player as Sprite2D;
			}
			else
			{
				foreach (Node child in player.GetChildren())
				{
					if (child is Sprite2D sprite2D)
					{
						playerSprite = sprite2D;
						break;
					}
				}
			}
		}
		
		pauseMenu = GetNode<PauseMenu>("PauseLayer/PauseMenu");
		
		enemyTextures = new Texture2D[]
		{
			GD.Load<Texture2D>("res://sprites/enemy.png"),
			GD.Load<Texture2D>("res://sprites/enemy 2.png"),
			GD.Load<Texture2D>("res://sprites/enemy 3.png"),
			GD.Load<Texture2D>("res://sprites/enemy 4.png"),
			GD.Load<Texture2D>("res://sprites/enemy 5.png"),
			GD.Load<Texture2D>("res://sprites/enemy 6.png"),
			GD.Load<Texture2D>("res://sprites/enemy 7.png"),
			GD.Load<Texture2D>("res://sprites/enemy 8.png"),
			GD.Load<Texture2D>("res://sprites/enemy 9.png")
		};
		
		playerTextures = new Texture2D[]
		{
			GD.Load<Texture2D>("res://sprites/player.png"),
			GD.Load<Texture2D>("res://sprites/player 2.png"),
			GD.Load<Texture2D>("res://sprites/player 3.png")
		};
		
		if (HasNode("GameOverLayer/GameOverMenu"))
		{
			gameOverMenu = GetNode<GameOverMenu>("GameOverLayer/GameOverMenu");
		}
		else if (HasNode("GameOverMenu"))
		{
			gameOverMenu = GetNode<GameOverMenu>("GameOverMenu");
		}
		
		GetNode<Timer>("Timer").Timeout += OnTimerTimeout;
		
		if (gameOverMenu != null)
		{
			gameOverMenu.RestartGame += OnRestartGame;
		}
		
		ProcessMode = Node.ProcessModeEnum.Always;
		scoreLabel = GetNode<Label>("UI/ScoreLabel");
		
		// Hide mouse cursor and create custom crosshair
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		
		crosshair = new Sprite2D();
		crosshair.Texture = GD.Load<Texture2D>("res://sprites/crosshair.png");
		crosshair.ZIndex = 1000;
		AddChild(crosshair);
	}
	
	public override void _Process(double delta)
	{
		if (isPaused || isGameOver) return;

		survivalTime += (float)delta;

		if (scoreLabel != null)
		{
			int minutes = (int)(survivalTime / 60);
			int seconds = (int)(survivalTime % 60);
			scoreLabel.Text = $"Survival Time: {minutes:D2}:{seconds:D2}";
		}
		
		spriteSwapTimer += (float)delta;
		if (spriteSwapTimer >= playerSpriteSwapInterval)
		{
			SwapPlayerSprite();
			spriteSwapTimer = 0f;
		}
		
		// Update crosshair position to follow mouse
		if (crosshair != null)
		{
			crosshair.GlobalPosition = GetGlobalMousePosition();
		}
	}
	
	private void SwapPlayerSprite()
	{
		if (playerSprite == null || playerTextures == null || playerTextures.Length == 0)
		{
			return;
		}
		
		Texture2D currentTexture = playerSprite.Texture;
		Texture2D newTexture;
		
		do
		{
			int randIndex = GD.RandRange(0, playerTextures.Length - 1);
			newTexture = playerTextures[randIndex];
		} while (newTexture == currentTexture && playerTextures.Length > 1);
		
		playerSprite.Texture = newTexture;
	}

	public override void _Input(InputEvent @event)
	{
		if (isGameOver) return;
		
		if (@event.IsActionPressed("ui_cancel"))
		{
			TogglePause();
		}
	}
	
	private void TogglePause()
	{
		isPaused = !isPaused;
		
		if (isPaused)
		{
			pauseMenu.ShowPauseMenu();
			if (crosshair != null) crosshair.Visible = false;
		}
		else
		{
			pauseMenu.HidePauseMenu();
			if (crosshair != null) crosshair.Visible = true;
		}
		
		var timer = GetNode<Timer>("Timer");
		timer.Paused = isPaused;
		
		if (player != null)
		{
			player.SetProcess(!isPaused);
			player.SetPhysicsProcess(!isPaused);
		}
		
		var enemies = GetTree().GetNodesInGroup("enemies");
		foreach (Node enemy in enemies)
		{
			enemy.SetProcess(!isPaused);
			enemy.SetPhysicsProcess(!isPaused);
		}
	}
	
	private void OnTimerTimeout()
	{
		if (isPaused || isGameOver) return;

		float t = Mathf.Clamp(survivalTime / spawnIntervalRampUpTime, 0f, 1f);
		float easedT = Mathf.Sqrt(t);
		float newInterval = Mathf.Lerp(spawnIntervalStart, spawnIntervalMin, easedT);

		int enemyCount = GetTree().GetNodesInGroup("enemies").Count;
		if (enemyCount > maxEnemyCount)
		{
			newInterval = spawnIntervalMin;
		}

		var timer = GetNode<Timer>("Timer");
		timer.WaitTime = newInterval;
		timer.Start();

		var enemy = enemyScene.Instantiate<Node2D>();
		var spawnPos = GetRandomSpawnPositionOutsideViewport();
		enemy.GlobalPosition = spawnPos;

		if (enemy is Enemies enemyNode)
		{
			var sprite = enemyNode.GetNode<Sprite2D>("Sprite2D");
			int randIndex = (int)(GD.Randi() % enemyTextures.Length);
			sprite.Texture = enemyTextures[randIndex];

			Vector2 viewportSize = GetViewportRect().Size;
			if (spawnPos.X > viewportSize.X) sprite.FlipV = true;
			if (spawnPos.Y > viewportSize.Y) sprite.FlipV = true;
		}

		enemy.AddToGroup("enemies");
		AddChild(enemy);
	}
	
	private Vector2 GetRandomSpawnPositionOutsideViewport()
	{
		var viewportRect = GetViewportRect();
		var viewportSize = viewportRect.Size;
		int side = GD.RandRange(0, 3);
		Vector2 spawnPos;
		
		switch (side)
		{
			case 0:
				spawnPos = new Vector2(GD.RandRange(0, (int)viewportSize.X), -SpawnMargin);
				break;
			case 1:
				spawnPos = new Vector2(viewportSize.X + SpawnMargin, GD.RandRange(0, (int)viewportSize.Y));
				break;
			case 2:
				spawnPos = new Vector2(GD.RandRange(0, (int)viewportSize.X), viewportSize.Y + SpawnMargin);
				break;
			case 3:
				spawnPos = new Vector2(-SpawnMargin, GD.RandRange(0, (int)viewportSize.Y));
				break;
			default:
				spawnPos = Vector2.Zero;
				break;
		}
		
		return spawnPos;
	}
	
	public void TriggerGameOver()
	{
		if (isGameOver) return;

		isGameOver = true;

		var timer = GetNode<Timer>("Timer");
		timer.Paused = true;

		if (player != null)
		{
			player.SetProcess(false);
			player.SetPhysicsProcess(false);
		}

		var enemies = GetTree().GetNodesInGroup("enemies");
		foreach (Node enemy in enemies)
		{
			enemy.SetProcess(false);
			enemy.SetPhysicsProcess(false);
		}

		if (gameOverMenu != null)
		{
			gameOverMenu.ShowGameOver(survivalTime);
		}
	}
	
	private void OnRestartGame()
	{
		GetTree().ReloadCurrentScene();
	}
}
