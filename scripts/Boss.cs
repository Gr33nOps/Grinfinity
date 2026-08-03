using Godot;

/// <summary>
/// Shared spine for every boss: health, damage, the hit flash and the two
/// signals <see cref="GameManager"/> listens for. What makes each boss distinct
/// — movement, attacks, what it spawns — belongs entirely to the subclass.
///
/// A boss is deliberately not a body: none inherit from <see cref="Body"/>, none
/// join the "bodies" group, so none count toward the spawn cap, are absorbed by
/// a nova, or are targeted by moons. Beating one has to be worth something a
/// mass-menu total cannot buy.
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

	protected int health;
	protected bool defeated;
	private Tween hitFlash;

	public float HealthFraction => MaxHealth <= 0 ? 0f : (float)health / MaxHealth;

	public sealed override void _Ready()
	{
		health = MaxHealth;
		AddToGroup("hazards");
		AddToGroup("bosses");
		OnBossReady();
	}

	/// <summary>Subclass setup — scenes to preload, initial state. Health and groups are already set.</summary>
	protected virtual void OnBossReady() { }

	public bool TakeDamage(int amount, Vector2 impactDirection = default)
	{
		if (defeated)
			return false;

		health -= amount;
		EmitSignal(SignalName.HealthChanged, HealthFraction);

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

	/// <summary>Called on every hit that does not finish the boss off.</summary>
	protected virtual void OnDamaged() { }

	/// <summary>Called once, on the killing blow, before the node is freed.</summary>
	protected virtual void OnBossDefeated() { }

	private void FlashHit()
	{
		hitFlash?.Kill();
		Modulate = Colors.White;
		hitFlash = CreateTween();
		hitFlash.TweenProperty(this, "modulate", BossColor, 0.12f);
	}
}
