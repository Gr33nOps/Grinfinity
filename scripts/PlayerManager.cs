using Godot;

/// <summary>
/// Applies the world chosen before the orbit to the player's sprite.
///
/// Used to randomly cycle through all twelve skins every few seconds — a
/// pre-M5 placeholder from when the skins were purely cosmetic. Now that a
/// world is a chosen, unlockable identity (see <see cref="Worlds"/>), cycling
/// away from it mid-orbit would undercut the choice, so this simply sets the
/// chosen skin once.
/// </summary>
public partial class PlayerManager : Node
{
	public override void _Ready()
	{
		var player = GetParent()?.GetNodeOrNull<Node2D>("player");
		Sprite2D sprite = player != null ? FindPlayerSprite(player) : null;
		if (sprite == null)
			return;

		// Cosmetic only, and never picked on the way into a run — the world is
		// whatever was last chosen on the Stats screen.
		int worldId = GameSettings.Instance?.World ?? 1;
		Texture2D texture = GD.Load<Texture2D>($"res://sprites/player {worldId}.png");
		if (texture != null)
			sprite.Texture = texture;
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
}
