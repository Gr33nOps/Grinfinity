using Godot;

/// <summary>
/// Shared spine for every boss: health, damage, the hit flash, the arrival and
/// the two signals <see cref="GameManager"/> listens for. What makes each boss
/// distinct — movement, attacks, what it spawns — belongs to the subclass.
///
/// A boss is deliberately not a body: it never joins the "bodies" group, so it
/// never counts toward the spawn cap and is never simply deleted by a Dash or a
/// Nova. Those land a fixed share of its health instead, once each.
/// </summary>
public abstract partial class Boss : CharacterBody2D, IShootable
{
	[Signal] public delegate void HealthChangedEventHandler(float fraction);
	[Signal] public delegate void DefeatedEventHandler();

	[Export] public int MaxHealth { get; set; } = 90;
	/// <summary>Shown on the boss bar and in the arrival announcement.</summary>
	[Export] public string BossName { get; set; } = "BOSS";
	[Export] public Color BossColor { get; set; } = new Color(0.86f, 0.72f, 1.0f);
	/// <summary>One line shown under the name when it arrives — what to expect.</summary>
	[Export] public string ArrivalLine { get; set; } = "";

	/// <summary>
	/// Which time round the bosses this is, from 1. Set before the boss enters the
	/// tree. Later cycles are a little tougher and attack a little faster; most of
	/// their difficulty comes from the enemies fighting alongside them.
	/// </summary>
	public int Cycle { get; set; } = 1;

	protected int health;
	protected bool defeated;
	private Tween hitFlash;
	protected Sprite2D Art;
	private float visualTime;
	protected float Windup;
	protected Player World;

	public int ScaledMaxHealth => Mathf.Max(1, Mathf.RoundToInt(MaxHealth * (1f + Balance.BossHealthPerCycle * (Cycle - 1))));
	public float HealthFraction => (float)health / ScaledMaxHealth;

	/// <summary>Attack gaps shrink a little each cycle, never below three quarters.</summary>
	protected float CycleTempo => Mathf.Max(1f - 0.08f * (Cycle - 1), 0.75f);

	public sealed override void _Ready()
	{
		health = ScaledMaxHealth;
		AddToGroup("hazards");
		AddToGroup("bosses");
		foreach (Node child in GetChildren()) if (child is Polygon2D polygon) polygon.Hide();
		string asset = this is BossCoil ? "coil" : this is BossBrood ? "brood" : "black_hole";
		Art = new Sprite2D { Texture = GD.Load<Texture2D>($"res://art/cosmic/boss_{asset}.svg"), Scale = Vector2.One * ArtScale }; AddChild(Art);
		// The hit circle grows with the art, so dashes, shots and Novas land
		// where the boss looks to be. The shape is copied first: the scene's
		// own resource is shared by every instance.
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape2D { Shape: CircleShape2D circle } shape)
				shape.Shape = new CircleShape2D { Radius = circle.Radius * Size };
		}
		Modulate = Colors.White;
		World = GameManager.Of(this)?.GetNodeOrNull<Player>("player");

		// Arrives with a pop rather than simply appearing.
		Scale = Vector2.One * 0.2f;
		CreateTween().TweenProperty(this, "scale", Vector2.One, 0.5f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		OnBossReady();
	}

	/// <summary>
	/// How big this boss is next to the Coil. Each boss is bigger than the one
	/// before it, so the Black Hole is the largest thing in the arena.
	/// </summary>
	protected virtual float Size => 1f;
	private float ArtScale => 0.88f * Size;

	/// <summary>Subclass setup — scenes to preload, initial state. Health and groups are already set.</summary>
	protected virtual void OnBossReady() { }

	public bool TakeDamage(int amount, Vector2 impactDirection = default)
	{
		if (defeated)
			return false;

		health -= amount;
		EmitSignal(SignalName.HealthChanged, Mathf.Max(HealthFraction, 0f));

		if (health > 0)
		{
			FlashHit();
			OnDamaged();
			return false;
		}

		defeated = true;
		EmitSignal(SignalName.Defeated);
		OnBossDefeated();
		QueueFree();
		return true;
	}

	/// <summary>
	/// Takes a share of full health — how a Dash and a Nova hurt a boss. A share
	/// rather than a number, so the ability is worth the same against every boss
	/// and every cycle, and never ends a fight on its own.
	/// </summary>
	public bool TakeShareOfHealth(float share, Vector2 impactDirection = default)
	{
		return TakeDamage(Mathf.Max(1, Mathf.CeilToInt(ScaledMaxHealth * share)), impactDirection);
	}

	/// <summary>Called on every hit that does not finish the boss off.</summary>
	protected virtual void OnDamaged() { }

	/// <summary>Called once, on the killing blow, before the node is freed.</summary>
	protected virtual void OnBossDefeated() { }

	/// <summary>
	/// Somewhere to drift to, near the planet but not on it, and inside the
	/// arena. Bosses keep the fight where the player is rather than wandering off.
	/// </summary>
	protected Vector2 PointNearPlayer(float minDistance, float maxDistance)
	{
		Vector2 around = World != null && IsInstanceValid(World) ? World.GlobalPosition : GlobalPosition;
		Vector2 offset = Vector2.FromAngle(RunState.Rng.Randf() * Mathf.Tau) * RunState.Rng.RandfRange(minDistance, maxDistance);
		// Squashed vertically: the screen is wider than tall, and a boss above
		// the top edge is a boss the player cannot read.
		offset.Y *= 0.6f;
		return Arena.ClampToPlayable(around + offset, 160f);
	}

	public override void _Process(double delta)
	{
		visualTime += (float)delta;
		float pulse = 1 + .035f * Mathf.Sin(visualTime * 3) + Windup * .1f;
		Art.Scale = new Vector2(ArtScale / pulse, ArtScale * pulse);
		Art.Rotation = -Rotation + Mathf.Sin(visualTime * 1.5f) * .08f;
		Art.SelfModulate = Colors.White.Lerp(new Color("ffc47c"), Windup);
	}

	private void FlashHit()
	{
		hitFlash?.Kill();
		Modulate = new Color(1.6f, 1.5f, 1.3f);
		hitFlash = CreateTween();
		hitFlash.TweenProperty(this, "modulate", Colors.White, 0.12f);
	}
}
