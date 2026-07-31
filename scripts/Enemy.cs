using Godot;

public enum EnemyKind
{
	/// <summary>Baseline: walks straight at the player.</summary>
	Chaser,

	/// <summary>Small and quick, spawns in packs. Dies to one hit.</summary>
	Swarmer,

	/// <summary>Large and slow, soaks several hits and has a big contact radius.</summary>
	Tank
}

public partial class Enemy : CharacterBody2D
{
	// How far outside the viewport an enemy may drift before it is culled.
	private const float CullMargin = 600.0f;

	private Node2D player;
	private float speedMultiplier = 1.0f;
	private int health = 1;
	private Color baseTint = Colors.White;
	private Tween hitFlash;

	public EnemyKind Kind { get; private set; } = EnemyKind.Chaser;

	public override void _Ready()
	{
		var gameManager = GetTree().GetFirstNodeInGroup("game_manager");
		player = gameManager?.GetNodeOrNull<Node2D>("player");
	}

	/// <summary>Applies the stats and look for a kind. Call before adding to the tree.</summary>
	public void Configure(EnemyKind kind)
	{
		Kind = kind;

		switch (kind)
		{
			case EnemyKind.Swarmer:
				speedMultiplier = 1.75f;
				health = 1;
				Scale = new Vector2(1.2f, 1.2f);
				baseTint = new Color(0.72f, 1.0f, 0.85f);
				break;

			case EnemyKind.Tank:
				speedMultiplier = 0.55f;
				health = 4;
				Scale = new Vector2(3.2f, 3.2f);
				baseTint = new Color(0.7f, 0.78f, 1.0f);
				break;

			default:
				speedMultiplier = 1.0f;
				health = 1;
				Scale = new Vector2(2.0f, 2.0f);
				baseTint = Colors.White;
				break;
		}

		Modulate = baseTint;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (player == null || !IsInstanceValid(player))
			return;

		// Read the spawner's ramp each frame so living enemies speed up too,
		// then apply this kind's multiplier on top.
		float speed = EnemySpawner.CurrentSpeed * speedMultiplier;

		Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * speed;
		LookAt(player.GlobalPosition);
		MoveAndSlide();

		CullIfLost();
	}

	/// <returns>True if this hit destroyed the enemy.</returns>
	public bool TakeDamage(int amount)
	{
		health -= amount;

		if (health > 0)
		{
			FlashHit();
			return false;
		}

		QueueFree();
		return true;
	}

	/// <summary>Brief white flash so chip damage on tanks reads clearly.</summary>
	private void FlashHit()
	{
		hitFlash?.Kill();
		Modulate = Colors.White;
		hitFlash = CreateTween();
		hitFlash.TweenProperty(this, "modulate", baseTint, 0.15f);
	}

	/// <summary>Safety net so an enemy that somehow drifts away cannot leak forever.</summary>
	private void CullIfLost()
	{
		var bounds = GetViewportRect().Size;
		if (GlobalPosition.X < -CullMargin || GlobalPosition.X > bounds.X + CullMargin ||
			GlobalPosition.Y < -CullMargin || GlobalPosition.Y > bounds.Y + CullMargin)
		{
			QueueFree();
		}
	}
}
