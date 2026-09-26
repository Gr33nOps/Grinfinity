using Godot;

/// <summary>
/// A small framed loop that shows what an upgrade does, drawn with the game's
/// own planet, shots and enemies: faster shots stream out, spread shots fan
/// out, a dash cuts through a line of enemies, a Nova ring swells. Shown beside
/// the name in the upgrade screen, so a player can see an upgrade rather than
/// read about it.
/// </summary>
public partial class UpgradePreview : Control
{
	private const float Loop = 2.4f;
	private static readonly Color Back = new("211428");
	private static readonly Color Frame = new("986077");
	private static readonly Color Nova = new("ff7b8e");
	private static readonly Color Aura = new(1f, 0.48f, 0.56f, 0.2f);

	private RunUpgradeId upgrade;
	private float time;
	private Texture2D planet, face, gun, shot, drifter, boss;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ClipContents = true;
		planet = GD.Load<Texture2D>($"res://art/cosmic/planet_{GameSettings.Instance?.World ?? 1}.svg");
		face = Worlds.Face(GameSettings.Instance?.World ?? 1);
		gun = GD.Load<Texture2D>("res://art/cosmic/blaster.svg");
		shot = GD.Load<Texture2D>("res://art/cosmic/shot.svg");
		drifter = GD.Load<Texture2D>("res://art/cosmic/body_drifter.svg");
		boss = GD.Load<Texture2D>("res://art/cosmic/boss_coil.svg");
	}

	/// <summary>Switches to an upgrade's loop, from its start.</summary>
	public void Play(RunUpgradeId id)
	{
		if (id != upgrade)
			time = 0f;
		upgrade = id;
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var area = new Rect2(Vector2.Zero, Size);
		DrawStyleBox(Box(Back, Frame, 14, 3), area);
		// A few fixed stars, so it reads as the same space the game is in.
		foreach (Vector2 star in new[] { new Vector2(0.18f, 0.2f), new Vector2(0.52f, 0.12f), new Vector2(0.86f, 0.3f), new Vector2(0.34f, 0.85f), new Vector2(0.72f, 0.8f) })
			DrawCircle(star * Size, 1.3f, new Color(1f, 1f, 1f, 0.35f));

		float t = Mathf.PosMod(time, Loop) / Loop;
		float cy = Size.Y * 0.5f;
		switch (upgrade)
		{
			case RunUpgradeId.FireRate: Stream(t, cy, 0.09f, false); break;
			case RunUpgradeId.SpreadShot: Spread(t, cy); break;
			case RunUpgradeId.Piercing: Pierce(t, cy); break;
			case RunUpgradeId.DashReach: Dash(t, cy); break;
			case RunUpgradeId.DashBlink: Blink(t, cy); break;
			case RunUpgradeId.OverdrivePower: Stream(t, cy, 0.045f, true); break;
			case RunUpgradeId.OverdriveDuration: Duration(t, cy); break;
			case RunUpgradeId.BiggerNova: BigNova(t, cy); break;
			default: NovaPower(t, cy); break;
		}
	}

	// --- The loops --------------------------------------------------------------

	/// <summary>Shots pouring into an enemy. Overdrive is denser, wobblier and glowing.</summary>
	private void Stream(float t, float cy, float gap, bool overdrive)
	{
		var at = new Vector2(46f, cy);
		if (overdrive)
			DrawCircle(at, 30f + 3f * Mathf.Sin(time * 14f), Aura);
		float now = t * Loop;
		for (float fired = now - 1f; fired <= now; fired += gap)
		{
			float fire = Mathf.Floor(fired / gap) * gap;
			float age = now - fire;
			if (age < 0f || age > 0.6f)
				continue;
			float wobble = overdrive ? Mathf.Sin(fire * 37f) * 6f : 0f;
			Shot(new Vector2(78f + age * 260f, cy + wobble * age * 3f), 10f);
		}
		float hit = Mathf.PosMod(now, 0.2f) < 0.06f ? 1.4f : 1f;
		Enemy(new Vector2(Size.X - 34f, cy), 30f, hit);
		Planet(at, 22f, 1f);
	}

	private void Spread(float t, float cy)
	{
		var at = new Vector2(46f, cy);
		float age = Mathf.PosMod(t * Loop, 0.8f);
		foreach (float angle in new[] { -0.26f, 0f, 0.26f })
		{
			Vector2 dir = Vector2.FromAngle(angle);
			Vector2 target = at + dir * 150f;
			bool struck = age > 0.5f;
			Enemy(target, 24f, struck ? 1.35f - (age - 0.5f) : 1f);
			if (!struck)
				Shot(at + dir * (32f + age / 0.5f * (target - at).Length()), 9f);
		}
		Planet(at, 22f, 1f);
	}

	private void Pierce(float t, float cy)
	{
		var at = new Vector2(40f, cy);
		float x = 70f + t * (Size.X + 40f);
		foreach (float ex in new[] { 128f, 186f })
		{
			bool inside = Mathf.Abs(x - ex) < 16f;
			Enemy(new Vector2(ex, cy), 28f, inside ? 1.3f : 1f);
		}
		Shot(new Vector2(x, cy), 12f);
		Planet(at, 22f, 1f);
	}

	/// <summary>A long dash straight through a line of enemies, which pop as it passes.</summary>
	private void Dash(float t, float cy)
	{
		float travel = Mathf.Clamp((t - 0.25f) / 0.2f, 0f, 1f);
		float x = Mathf.Lerp(36f, Size.X - 38f, Mathf.SmoothStep(0f, 1f, travel));
		foreach (float ex in new[] { 100f, 150f })
		{
			if (x < ex - 8f)
				Enemy(new Vector2(ex, cy), 24f, 1f);
			else
				Pop(new Vector2(ex, cy), (x - ex) / 90f);
		}
		if (travel > 0f && travel < 1f)
			for (int g = 1; g <= 4; g++)
				Planet(new Vector2(x - g * 16f, cy), 20f, 0.35f - g * 0.07f);
		Planet(new Vector2(x, cy), 20f, t > 0.9f ? 1f - (t - 0.9f) * 10f : 1f);
	}

	/// <summary>A short dash, then a long safe blink an enemy drifts straight through.</summary>
	private void Blink(float t, float cy)
	{
		float travel = Mathf.Clamp(t / 0.15f, 0f, 1f);
		var at = new Vector2(Mathf.Lerp(40f, 110f, travel), cy);
		bool blinking = t > 0.15f && t < 0.85f;
		float alpha = blinking ? (Mathf.PosMod(time * 12f, 1f) < 0.5f ? 0.3f : 0.85f) : 1f;
		Enemy(new Vector2(Mathf.Lerp(Size.X + 20f, -20f, t), cy + 6f), 26f, 1f);
		Planet(at, 22f, alpha);
	}

	private void Duration(float t, float cy)
	{
		Stream(t, cy - 8f, 0.06f, true);
		float left = 1f - t;
		var bar = new Rect2(24f, Size.Y - 20f, Size.X - 48f, 8f);
		DrawStyleBox(Box(new Color("2a1a33"), Frame, 4, 1), bar);
		DrawStyleBox(Box(Nova, Nova, 4, 0), new Rect2(bar.Position, new Vector2(bar.Size.X * left, bar.Size.Y)));
	}

	/// <summary>A wide Nova ring rolling out and popping everything it reaches.</summary>
	private void BigNova(float t, float cy)
	{
		var at = new Vector2(Size.X * 0.5f, cy);
		float grow = Mathf.Clamp((t - 0.1f) / 0.45f, 0f, 1f);
		float radius = Mathf.Lerp(20f, 120f, 1f - Mathf.Pow(1f - grow, 2f));
		for (int i = 0; i < 6; i++)
		{
			Vector2 spot = at + Vector2.FromAngle(i * Mathf.Tau / 6f + 0.3f) * (i % 2 == 0 ? 52f : 88f) * new Vector2(1f, 0.55f);
			float reach = spot.DistanceTo(at);
			if (grow <= 0f || radius < reach)
				Enemy(spot, 20f, 1f);
			else
				Pop(spot, (radius - reach) / 60f);
		}
		if (grow > 0f && grow < 1f)
			Ring(at, radius, 1f - grow);
		Planet(at, 20f, 1f);
	}

	/// <summary>The Nova reaching a boss and knocking a big chunk off its health.</summary>
	private void NovaPower(float t, float cy)
	{
		var at = new Vector2(46f, cy + 6f);
		var bossAt = new Vector2(Size.X - 52f, cy + 6f);
		float grow = Mathf.Clamp((t - 0.1f) / 0.4f, 0f, 1f);
		float radius = Mathf.Lerp(20f, bossAt.X - at.X, grow);
		bool struck = grow >= 1f;
		float shake = struck && t < 0.62f ? Mathf.Sin(time * 60f) * 3f : 0f;
		DrawTextureRect(boss, Centred(bossAt + new Vector2(shake, 0f), 64f), false);
		var bar = new Rect2(bossAt.X - 34f, 12f, 68f, 8f);
		DrawStyleBox(Box(new Color("2a1a33"), Frame, 4, 1), bar);
		float health = struck ? 0.65f : 1f;
		DrawStyleBox(Box(new Color("dcb8ff"), new Color("dcb8ff"), 4, 0), new Rect2(bar.Position, new Vector2(bar.Size.X * health, bar.Size.Y)));
		if (grow > 0f && !struck)
			Ring(at, radius, 1f);
		Planet(at, 20f, 1f);
	}

	// --- Pieces -----------------------------------------------------------------

	private void Planet(Vector2 at, float radius, float alpha)
	{
		if (alpha <= 0f)
			return;
		var tint = new Color(1f, 1f, 1f, alpha);
		// Blaster first, so the planet sits over its base.
		DrawTextureRect(gun, new Rect2(at + new Vector2(radius * 0.55f, -radius * 0.5f), new Vector2(radius * 1.6f, radius * 1.0f)), false, tint);
		float side = radius * 2.6f;
		DrawTextureRect(planet, Centred(at, side), false, tint);
		DrawTextureRect(face, Centred(at, side), false, tint);
	}

	private void Enemy(Vector2 at, float size, float squash)
	{
		DrawTextureRect(drifter, Centred(at, size * squash), false, squash > 1f ? new Color(1.4f, 1.2f, 1.2f) : Colors.White);
	}

	private void Shot(Vector2 at, float size) => DrawTextureRect(shot, Centred(at, size), false);

	/// <summary>A little burst where an enemy popped, fading over <paramref name="age"/> 0..1.</summary>
	private void Pop(Vector2 at, float age)
	{
		if (age >= 1f)
			return;
		float a = 1f - age;
		DrawArc(at, 6f + age * 18f, 0f, Mathf.Tau, 20, new Color(ArcadeSkin.Orange, a), 2.5f, true);
		for (int i = 0; i < 5; i++)
			DrawCircle(at + Vector2.FromAngle(i * Mathf.Tau / 5f) * (8f + age * 16f), 2f * a + 0.5f, new Color(ArcadeSkin.Cream, a));
	}

	private void Ring(Vector2 at, float radius, float strength)
	{
		DrawCircle(at, radius, new Color(Nova, 0.1f * strength));
		DrawArc(at, radius, 0f, Mathf.Tau, 48, new Color(Nova, 0.9f * strength), 4f, true);
		DrawArc(at, radius, 0f, Mathf.Tau, 48, new Color(ArcadeSkin.Cream, 0.8f * strength), 1.5f, true);
	}

	private static Rect2 Centred(Vector2 at, float side) => new(at - Vector2.One * side * 0.5f, Vector2.One * side);

	private static StyleBoxFlat Box(Color fill, Color border, int radius, int line) => new()
	{
		BgColor = fill, BorderColor = border,
		BorderWidthLeft = line, BorderWidthRight = line, BorderWidthTop = line, BorderWidthBottom = line,
		CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius
	};
}
