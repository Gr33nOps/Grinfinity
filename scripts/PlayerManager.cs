using Godot;

public partial class PlayerManager : Node
{
	private Node2D player;
	private Sprite2D playerSprite;
	private Texture2D[] playerTextures;
	private float spriteSwapTimer = 0f;
	private const float playerSpriteSwapInterval = 5f;
	
	public override void _Ready()
	{
		LoadPlayerReference();
		LoadPlayerTextures();
	}
	
	private void LoadPlayerReference()
	{
		player = GetNode<Node2D>("/root/game/player");
		
		if (player != null)
		{
			playerSprite = FindPlayerSprite();
		}
	}
	
	private Sprite2D FindPlayerSprite()
	{
		if (player.HasNode("Sprite2D"))
			return player.GetNode<Sprite2D>("Sprite2D");
		
		if (player.HasNode("sprite"))
			return player.GetNode<Sprite2D>("sprite");
		
		if (player is Sprite2D sprite)
			return sprite;
		
		foreach (Node child in player.GetChildren())
		{
			if (child is Sprite2D sprite2D)
				return sprite2D;
		}
		
		return null;
	}
	
	private void LoadPlayerTextures()
	{
		playerTextures = new Texture2D[]
		{
			GD.Load<Texture2D>("res://sprites/player 1.png"),
			GD.Load<Texture2D>("res://sprites/player 2.png"),
			GD.Load<Texture2D>("res://sprites/player 3.png"),
			GD.Load<Texture2D>("res://sprites/player 4.png"),
			GD.Load<Texture2D>("res://sprites/player 5.png"),
			GD.Load<Texture2D>("res://sprites/player 6.png"),
			GD.Load<Texture2D>("res://sprites/player 7.png"),
			GD.Load<Texture2D>("res://sprites/player 8.png"),
			GD.Load<Texture2D>("res://sprites/player 9.png"),
			GD.Load<Texture2D>("res://sprites/player 10.png"),
			GD.Load<Texture2D>("res://sprites/player 11.png"),
			GD.Load<Texture2D>("res://sprites/player 12.png")
		};
	}
	
	public override void _Process(double delta)
	{
		spriteSwapTimer += (float)delta;
		if (spriteSwapTimer >= playerSpriteSwapInterval)
		{
			SwapPlayerSprite();
			spriteSwapTimer = 0f;
		}
	}
	
	private void SwapPlayerSprite()
	{
		if (playerSprite == null || playerTextures == null || playerTextures.Length == 0)
			return;
		
		Texture2D currentTexture = playerSprite.Texture;
		Texture2D newTexture;
		
		do
		{
			int randIndex = GD.RandRange(0, playerTextures.Length - 1);
			newTexture = playerTextures[randIndex];
		} while (newTexture == currentTexture && playerTextures.Length > 1);
		
		playerSprite.Texture = newTexture;
	}
	
	public void SetPaused(bool paused)
	{
		if (player != null)
		{
			player.SetProcess(!paused);
			player.SetPhysicsProcess(!paused);
		}
	}
}
