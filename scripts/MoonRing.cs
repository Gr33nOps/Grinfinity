using Godot;
using System.Collections.Generic;

/// <summary>
/// Keeps the moons in sync with the mass tiers <see cref="RunState"/> reports.
///
/// Moons are top-level nodes rather than children of the world, so their orbit
/// is unaffected by the player's own scale, which grows with mass.
/// </summary>
public partial class MoonRing : Node2D
{
	[Export] public PackedScene MoonScene { get; set; }
	/// <summary>Orbit radius of the first moon; each further one sits further out.</summary>
	[Export] public float BaseRadius { get; set; } = 130.0f;
	[Export] public float RadiusStep { get; set; } = 34.0f;
	[Export] public float BaseSpeed { get; set; } = 1.55f;

	private readonly List<Moon> moons = new();
	private RunState run;

	public override void _Ready()
	{
		MoonScene ??= GD.Load<PackedScene>("res://scenes/moon.tscn");

		run = GameManager.Of(this)?.Run;
		if (run == null)
			return;

		run.MoonCountChanged += OnMoonCountChanged;
		OnMoonCountChanged(run.Moons);
	}

	public override void _ExitTree()
	{
		if (run != null && IsInstanceValid(run))
			run.MoonCountChanged -= OnMoonCountChanged;

		// The moons are parented elsewhere, so leaving the tree has to take them.
		foreach (Moon moon in moons)
		{
			if (IsInstanceValid(moon))
				moon.QueueFree();
		}
		moons.Clear();
	}

	private void OnMoonCountChanged(int count)
	{
		moons.RemoveAll(moon => !IsInstanceValid(moon));

		while (moons.Count > count)
		{
			Moon last = moons[^1];
			moons.RemoveAt(moons.Count - 1);
			if (IsInstanceValid(last))
				last.QueueFree();
		}

		while (moons.Count < count)
			moons.Add(AddMoon(moons.Count));
	}

	private Moon AddMoon(int index)
	{
		var moon = MoonScene.Instantiate<Moon>();
		moon.OrbitRadius = BaseRadius + RadiusStep * index;
		// Alternating direction and evenly spread phase, so a full set sweeps
		// the whole ring instead of clustering on one side.
		moon.OrbitSpeed = BaseSpeed * (index % 2 == 0 ? 1.0f : -0.82f);
		moon.PhaseOffset = Mathf.Tau * index / 3.0f;

		GameManager.Spawn(this, moon);
		return moon;
	}
}
