using Godot;

public partial class PlayerManager : Node
{
	[Export] public float SpriteInterval { get; set; } = 5.0f;

	private const int SkinCount = 12;

	private Sprite2D playerSprite;
	private Texture2D[] playerTextures;
	private float spriteTimer = 0f;

	public override void _Ready()
	{
		LoadPlayerTextures();

		var player = GetParent()?.GetNodeOrNull<Node2D>("player");
		if (player != null)
			playerSprite = FindPlayerSprite(player);
	}

	public override void _Process(double delta)
	{
		if (playerSprite == null)
			return;

		spriteTimer += (float)delta;
		if (spriteTimer >= SpriteInterval)
		{
			ChangePlayerSprite();
			spriteTimer = 0f;
		}
	}

	private static Sprite2D FindPlayerSprite(Node2D player)
	{
		if (player.HasNode("Sprite2D"))
			return player.GetNode<Sprite2D>("Sprite2D");

		foreach (Node child in player.GetChildren())
		{
			if (child is Sprite2D sprite2D)
				return sprite2D;
		}

		return null;
	}

	private void LoadPlayerTextures()
	{
		playerTextures = new Texture2D[SkinCount];
		for (int i = 0; i < SkinCount; i++)
		{
			playerTextures[i] = GD.Load<Texture2D>($"res://sprites/player {i + 1}.png");
		}
	}

	private void ChangePlayerSprite()
	{
		if (playerTextures == null || playerTextures.Length == 0)
			return;

		Texture2D currentTexture = playerSprite.Texture;
		Texture2D newTexture;

		do
		{
			int randomIndex = GD.RandRange(0, playerTextures.Length - 1);
			newTexture = playerTextures[randomIndex];
		} while (newTexture == currentTexture && playerTextures.Length > 1);

		playerSprite.Texture = newTexture;
	}
}
