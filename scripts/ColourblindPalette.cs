using Godot;

/// <summary>
/// An alternate tint and burst colour per <see cref="BodyKind"/>, spaced along a
/// blue/orange/yellow axis rather than the red/green/purple one the normal
/// palette leans on — the pairing most likely to collide for deuteranopia and
/// protanopia, the two most common forms. Applied in <see cref="Body.Configure"/>
/// when <see cref="GameSettings.ColourblindMode"/> is on, after the normal
/// per-kind behaviour has already set its own colours.
/// </summary>
public static class ColourblindPalette
{
	private readonly struct Entry
	{
		public Entry(Color tint, Color burst)
		{
			Tint = tint;
			Burst = burst;
		}

		public Color Tint { get; }
		public Color Burst { get; }
	}

	private static readonly System.Collections.Generic.Dictionary<BodyKind, Entry> Table = new()
	{
		[BodyKind.Drifter] = new Entry(Colors.White, new Color(0.85f, 0.85f, 0.9f)),
		[BodyKind.Shard] = new Entry(new Color(0.35f, 0.85f, 1.0f), new Color(0.3f, 0.8f, 1.0f)),
		[BodyKind.Planetoid] = new Entry(new Color(0.3f, 0.5f, 1.0f), new Color(0.35f, 0.55f, 1.0f)),
		[BodyKind.Fracture] = new Entry(new Color(1.0f, 0.4f, 0.85f), new Color(1.0f, 0.45f, 0.85f)),
		[BodyKind.Splinter] = new Entry(new Color(1.0f, 0.55f, 0.9f), new Color(1.0f, 0.55f, 0.9f)),
		[BodyKind.Satellite] = new Entry(new Color(1.0f, 0.92f, 0.25f), new Color(1.0f, 0.88f, 0.3f)),
		[BodyKind.Flare] = new Entry(new Color(1.0f, 0.55f, 0.15f), new Color(1.0f, 0.5f, 0.1f)),
		[BodyKind.Bulwark] = new Entry(new Color(0.75f, 0.8f, 0.9f), new Color(0.78f, 0.82f, 0.9f))
	};

	/// <summary>The safe tint for a kind, or the given fallback if the kind is not in the table.</summary>
	public static Color Tint(BodyKind kind, Color fallback) =>
		Table.TryGetValue(kind, out Entry entry) ? entry.Tint : fallback;

	/// <summary>The safe burst colour for a kind, or the given fallback.</summary>
	public static Color Burst(BodyKind kind, Color fallback) =>
		Table.TryGetValue(kind, out Entry entry) ? entry.Burst : fallback;
}
