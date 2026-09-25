using Godot;

/// <summary>
/// One static drawing. The engine culls a canvas item by the bounds of what it
/// drew, so splitting scenery into these is what lets off-screen parts cost
/// nothing. Draws once; only <see cref="Pulse"/> animates, through Modulate.
/// </summary>
public partial class Painted : Node2D
{
	private readonly System.Action<CanvasItem> paint;
	private float time;

	/// <summary>Gently breathes its opacity. Changing Modulate does not redraw.</summary>
	public bool Pulse { get; init; }

	public Painted() { }

	/// <param name="area">Documents the region it covers. Culling uses what is actually drawn.</param>
	public Painted(Rect2 area, System.Action<CanvasItem> paint)
	{
		this.paint = paint;
	}

	public override void _Ready()
	{
		SetProcess(Pulse);
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		Modulate = new Color(1f, 1f, 1f, 0.75f + 0.25f * Mathf.Sin(time * 2.2f));
	}

	public override void _Draw() => paint?.Invoke(this);
}
