using System.Collections.Generic;
using Godot;

/// <summary>
/// How one kind of body behaves: what it looks like, how it moves, what it takes
/// from a hit, and what it leaves when it dies.
///
/// Behaviours are stateless and shared between every body of a kind — anything
/// per-body lives on <see cref="Body"/>, including the one scratch timer a
/// behaviour is allowed to keep. That is what makes a bestiary of eight cost
/// eight small classes instead of one unreadable branch in _PhysicsProcess.
/// </summary>
public abstract class BodyBehaviour
{
	/// <summary>Stats and look. Called once, before the body enters the tree.</summary>
	public abstract void Apply(Body body);

	/// <summary>Steering for one physics frame. The default is to simply fall in.</summary>
	public virtual void Steer(Body body, float delta)
	{
		body.FallTowardWorld(delta);
	}

	/// <summary>
	/// A chance to refuse a hit. Used by armour: a deflected hit costs no health
	/// and reads as a clang rather than a wound.
	/// </summary>
	public virtual bool Deflects(Body body, Vector2 impactDirection) => false;

	/// <summary>Called on the killing blow, while the body is still valid.</summary>
	public virtual void OnDestroyed(Body body) { }
}

/// <summary>Lookup from kind to its shared behaviour.</summary>
public static class BodyBehaviours
{
	private static readonly Dictionary<BodyKind, BodyBehaviour> Table = new()
	{
		[BodyKind.Drifter] = new DrifterBehaviour(),
		[BodyKind.Shard] = new ShardBehaviour(),
		[BodyKind.Planetoid] = new PlanetoidBehaviour(),
		[BodyKind.Fracture] = new FractureBehaviour(),
		[BodyKind.Splinter] = new SplinterBehaviour(),
		[BodyKind.Flare] = new FlareBehaviour(),
		[BodyKind.Bulwark] = new BulwarkBehaviour()
	};

	public static BodyBehaviour For(BodyKind kind)
	{
		return Table.TryGetValue(kind, out BodyBehaviour behaviour)
			? behaviour
			: Table[BodyKind.Drifter];
	}
}

public sealed class DrifterBehaviour : BodyBehaviour
{
	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 1.0f;
		body.AccelMultiplier = 1.0f;
		body.SetHealth(1);
		body.BaseScale = new Vector2(2.0f, 2.0f);
		body.BaseTint = Colors.White;
		body.KnockbackStrength = 240.0f;
		body.DebrisCount = 2;
		body.SetBurst(55, 1.0f, new Color("bc7f83"));
	}
}

public sealed class ShardBehaviour : BodyBehaviour
{
	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 1.7f;
		body.AccelMultiplier = 1.55f;
		body.SetHealth(1);
		body.BaseScale = new Vector2(1.2f, 1.2f);
		body.BaseTint = new Color(0.72f, 1.0f, 0.85f);
		body.KnockbackStrength = 420.0f;
		body.DebrisCount = 1;
		body.SetBurst(26, 0.65f, new Color("eaa36f"));
	}
}

public sealed class PlanetoidBehaviour : BodyBehaviour
{
	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 0.5f;
		// Heavy bodies answer gravity slowly, which is what makes them read as
		// heavy rather than merely slow.
		body.AccelMultiplier = 0.45f;
		body.SetHealth(4);
		body.BaseScale = new Vector2(3.0f, 3.0f);
		body.BaseTint = new Color(0.7f, 0.78f, 1.0f);
		body.KnockbackStrength = 90.0f;
		body.DebrisCount = 6;
		body.SetBurst(120, 2.1f, new Color("958bbc"));
	}
}

/// <summary>Breaks into three splinters. Killing one in your lap is a mistake.</summary>
public sealed class FractureBehaviour : BodyBehaviour
{
	private const int SplinterCount = 3;
	private const float SplinterSpread = 190.0f;

	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 0.85f;
		body.AccelMultiplier = 0.9f;
		body.SetHealth(2);
		body.BaseScale = new Vector2(2.4f, 2.4f);
		body.BaseTint = new Color(0.86f, 0.66f, 1.0f);
		body.KnockbackStrength = 170.0f;
		body.DebrisCount = 2;
		body.SetBurst(70, 1.3f, new Color("88bfb7"));
	}

	public override void OnDestroyed(Body body)
	{
		for (int i = 0; i < SplinterCount; i++)
		{
			float angle = Mathf.Tau * i / SplinterCount + RunState.Rng.RandfRange(-0.4f, 0.4f);
			body.SpawnChild(BodyKind.Splinter, Vector2.FromAngle(angle) * SplinterSpread);
		}
	}
}

public sealed class SplinterBehaviour : BodyBehaviour
{
	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 1.45f;
		body.AccelMultiplier = 1.35f;
		body.SetHealth(1);
		body.BaseScale = new Vector2(1.1f, 1.1f);
		body.BaseTint = new Color(0.86f, 0.66f, 1.0f);
		body.KnockbackStrength = 380.0f;
		body.DebrisCount = 1;
		body.SetBurst(22, 0.6f, new Color("88bfb7"));
	}
}

/// <summary>
/// Bloated, and it takes its neighbourhood with it. The blast is lethal to the
/// world, so a Flare has to be killed at a distance — which is the lesson.
/// </summary>
public sealed class FlareBehaviour : BodyBehaviour
{
	/// <summary>Shown around every Flare by <see cref="BodyMark"/>, so the danger zone is never a guess.</summary>
	public const float BlastRadius = 230.0f;
	/// <summary>Seconds between a Flare dying and its blast landing.</summary>
	public const float Fuse = 0.3f;

	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 0.8f;
		body.AccelMultiplier = 0.8f;
		body.SetHealth(2);
		body.BaseScale = new Vector2(2.6f, 2.6f);
		body.BaseTint = new Color(1.0f, 0.62f, 0.42f);
		body.KnockbackStrength = 150.0f;
		body.DebrisCount = 3;
		body.SetBurst(150, 2.4f, new Color("e7876f"));
	}

	public override void OnDestroyed(Body body)
	{
		body.Detonate(BlastRadius);
	}
}

/// <summary>
/// Plated across the face it is travelling with. Most shots into the front arc
/// bounce off, so it is best flanked — or dashed through, which ignores armour.
/// </summary>
public sealed class BulwarkBehaviour : BodyBehaviour
{
	/// <summary>Cosine of the half-angle of the armoured arc. -0.5 is a 120 degree face.</summary>
	private const float ArmourCosine = -0.5f;

	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 0.7f;
		body.AccelMultiplier = 0.65f;
		body.SetHealth(3);
		body.BaseScale = new Vector2(2.6f, 2.6f);
		body.BaseTint = new Color(0.78f, 0.82f, 0.86f);
		body.KnockbackStrength = 110.0f;
		body.DebrisCount = 4;
		body.SetBurst(90, 1.6f, new Color("769cb4"));
	}

	/// <summary>Every this-many shots into the armour, one gets through.</summary>
	private const int HitsToChip = 5;

	public override bool Deflects(Body body, Vector2 impactDirection)
	{
		if (impactDirection == Vector2.Zero)
			return false;

		// A shot travelling into the face is head-on to the way the body is
		// going, so the dot product against its heading is strongly negative.
		if (impactDirection.Normalized().Dot(body.Forward) >= ArmourCosine)
			return false;

		// Shooting the plate is slow, not useless: every fifth shot chips
		// through. Flanking is still far quicker, but a player who has not
		// worked that out yet is not left helpless while it walks into them.
		body.BehaviourTimer += 1f;
		if (body.BehaviourTimer < HitsToChip)
			return true;

		body.BehaviourTimer = 0f;
		return false;
	}
}
