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
		[BodyKind.Satellite] = new SatelliteBehaviour(),
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
		body.SetBurst(55, 1.0f, new Color(0.91f, 0.35f, 0.45f));
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
		body.SetBurst(26, 0.65f, new Color(0.62f, 1.0f, 0.78f));
		body.TextureIndex = 3;
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
		body.SetBurst(120, 2.1f, new Color(0.66f, 0.76f, 1.0f));
		body.TextureIndex = 5;
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
		body.SetBurst(70, 1.3f, new Color(0.82f, 0.6f, 1.0f));
		body.TextureIndex = 6;
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
		body.SetBurst(22, 0.6f, new Color(0.82f, 0.6f, 1.0f));
		body.TextureIndex = 6;
	}
}

/// <summary>
/// Refuses to fall. It settles into a ring at a fixed radius and shoots inward,
/// so standing still — or letting it sit behind you — is punished.
/// </summary>
public sealed class SatelliteBehaviour : BodyBehaviour
{
	private const float OrbitRadius = 430.0f;
	private const float RadialCorrection = 2.4f;
	private const float FireInterval = 2.1f;

	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 1.05f;
		body.AccelMultiplier = 1.0f;
		body.SetHealth(2);
		body.BaseScale = new Vector2(1.8f, 1.8f);
		body.BaseTint = new Color(1.0f, 0.86f, 0.55f);
		body.KnockbackStrength = 300.0f;
		body.DebrisCount = 3;
		body.SetBurst(60, 1.0f, new Color(1.0f, 0.82f, 0.5f));
		body.TextureIndex = 7;
		body.BehaviourTimer = RunState.Rng.RandfRange(0.4f, FireInterval);
	}

	public override void Steer(Body body, float delta)
	{
		Vector2 toWorld = body.WorldOffset;
		float distance = Mathf.Max(toWorld.Length(), 1.0f);
		Vector2 inward = toWorld / distance;

		// Push out when too close, pull in when too far, and always keep going
		// round. The result is a body that circles at a readable, stable range.
		float error = distance - OrbitRadius;
		float speed = BodySpawner.CurrentSpeed * body.SpeedMultiplier;

		Vector2 tangent = inward.Orthogonal() * body.OrbitDirection;
		Vector2 target = tangent * speed + inward * Mathf.Clamp(error * RadialCorrection, -speed, speed);

		body.Drift = body.Drift.Lerp(target, 1f - Mathf.Exp(-3.0f * delta));

		body.BehaviourTimer -= delta;
		if (body.BehaviourTimer <= 0f)
		{
			body.BehaviourTimer = FireInterval;
			body.FireAt(body.WorldPosition);
		}
	}
}

/// <summary>
/// Bloated, and it takes its neighbourhood with it. The blast is lethal to the
/// world, so a Flare has to be killed at a distance — which is the lesson.
/// </summary>
public sealed class FlareBehaviour : BodyBehaviour
{
	private const float BlastRadius = 260.0f;

	public override void Apply(Body body)
	{
		body.SpeedMultiplier = 0.8f;
		body.AccelMultiplier = 0.8f;
		body.SetHealth(2);
		body.BaseScale = new Vector2(2.6f, 2.6f);
		body.BaseTint = new Color(1.0f, 0.62f, 0.42f);
		body.KnockbackStrength = 150.0f;
		body.DebrisCount = 3;
		body.SetBurst(150, 2.4f, new Color(1.0f, 0.6f, 0.35f));
		body.TextureIndex = 8;
	}

	public override void OnDestroyed(Body body)
	{
		body.Detonate(BlastRadius);
	}
}

/// <summary>
/// Plated across the face it is travelling with. Shots into the front arc bounce
/// off, so it has to be flanked — or dashed past and shot in the back.
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
		body.SetBurst(90, 1.6f, new Color(0.8f, 0.84f, 0.9f));
		body.TextureIndex = 4;
	}

	public override bool Deflects(Body body, Vector2 impactDirection)
	{
		if (impactDirection == Vector2.Zero)
			return false;

		// A shot travelling into the face is head-on to the way the body is
		// going, so the dot product against its heading is strongly negative.
		return impactDirection.Normalized().Dot(body.Forward) < ArmourCosine;
	}
}
