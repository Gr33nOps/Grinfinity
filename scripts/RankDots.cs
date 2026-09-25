using Godot;

/// <summary>Rank dots: one per level, filled for each one owned. ● ● ○</summary>
public partial class RankDots : Control
{
	public int Filled { get; set; }
	public int Total { get; set; } = 1;
	public Color Tint { get; set; } = ArcadeSkin.Orange;

	private const float Dot = 14f;
	private const float Gap = 8f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(Total * (Dot + Gap), Dot + 4f);
	}

	public override void _Draw()
	{
		for (int i = 0; i < Total; i++)
		{
			var centre = new Vector2(Dot * 0.5f + i * (Dot + Gap), Size.Y * 0.5f);
			if (i < Filled)
				DrawCircle(centre, Dot * 0.5f, Tint);
			DrawArc(centre, Dot * 0.5f, 0f, Mathf.Tau, 24, i < Filled ? Tint.Lightened(0.3f) : ArcadeSkin.Muted, 2f, true);
		}
	}
}
