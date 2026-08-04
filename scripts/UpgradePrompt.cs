using System.Collections.Generic;
using Godot;

/// <summary>
/// The wave-break offer. Two or three upgrades, priced in this run's stardust,
/// picked with a number key or a click.
///
/// It does not pause. The arena is still live, the world still moves, and the
/// window shuts when the next pack arrives whether or not anything was taken —
/// the pressure is the point. Skipping is always legal.
/// </summary>
public partial class UpgradePrompt : Control
{
	private const int OfferCount = 3;

	private static readonly Color Affordable = new Color(1f, 0.85f, 0.4f);
	private static readonly Color TooDear = new Color(0.55f, 0.55f, 0.62f);

	private const float CardWidth = 280f;
	private const float CardHeight = 150f;

	private RunState run;
	private HBoxContainer cards;
	private Label heading;
	private readonly List<RunUpgradeId> offer = new();
	private readonly List<Button> buttons = new();

	public override void _Ready()
	{
		cards = GetNodeOrNull<HBoxContainer>("Cards");
		heading = GetNodeOrNull<Label>("Heading");
		Visible = false;
	}

	/// <summary>Wires itself to the spawner's wave signals. Called by GameManager.</summary>
	public void Bind(RunState state, BodySpawner spawner)
	{
		run = state;
		if (spawner == null)
			return;

		spawner.WaveCleared += OnWaveCleared;
		spawner.WaveStarted += OnWaveStarted;
	}

	private void OnWaveCleared(int waveNumber)
	{
		if (run == null)
			return;

		RollOffer();

		// Nothing left to sell — every upgrade is maxed. Better to stay quiet
		// than to show an empty box.
		if (offer.Count == 0)
			return;

		Show();
	}

	private void OnWaveStarted(int waveNumber)
	{
		Visible = false;
	}

	/// <summary>
	/// Picks what is on the table. Always includes a mass-economy option when one
	/// is still available: it is the family tied to the gravity spine, and the
	/// one a player will walk past if it is not visibly competing for attention.
	/// </summary>
	private void RollOffer()
	{
		offer.Clear();

		var pool = new List<RunUpgradeId>();
		var massPool = new List<RunUpgradeId>();
		var unlockPool = new List<RunUpgradeId>();

		foreach (RunUpgrades.Profile profile in RunUpgrades.All)
		{
			if (run.IsMaxed(profile.Id))
				continue;

			// An improvement to something the run cannot do yet is a card that
			// cannot mean anything to the player reading it.
			if (profile.Requires is RunUpgradeId required && run.LevelOf(required) == 0)
				continue;

			if (profile.IsUnlock)
				unlockPool.Add(profile.Id);
			else if (profile.Family == UpgradeFamily.Mass)
				massPool.Add(profile.Id);
			else
				pool.Add(profile.Id);
		}

		// An ability the player does not have yet outranks a bigger number for
		// one they do. While any verb is still missing, one is always on offer,
		// so the opening breaks reliably hand back dash, then rapid fire, then
		// nova, instead of leaving it to the roll.
		if (unlockPool.Count > 0)
			offer.Add(TakeCheapest(unlockPool));

		if (massPool.Count > 0)
			offer.Add(Take(massPool));

		while (offer.Count < OfferCount && (pool.Count > 0 || massPool.Count > 0 || unlockPool.Count > 0))
		{
			List<RunUpgradeId> from = pool.Count > 0 ? pool : massPool.Count > 0 ? massPool : unlockPool;
			offer.Add(Take(from));
		}

		GuaranteeSomethingAffordable();
		BuildCards();
	}

	/// <summary>
	/// A break where every option is out of reach reads as a punishment for
	/// having played well enough to earn one. If the roll came up all-expensive
	/// but the run can afford *something*, swap the dearest offer for that.
	/// </summary>
	private void GuaranteeSomethingAffordable()
	{
		foreach (RunUpgradeId id in offer)
		{
			if (RunUpgrades.Get(id).CostAt(run.LevelOf(id)) <= run.Stardust)
				return;
		}

		RunUpgradeId cheapest = default;
		int cheapestCost = int.MaxValue;
		bool found = false;

		foreach (RunUpgrades.Profile profile in RunUpgrades.All)
		{
			if (run.IsMaxed(profile.Id) || offer.Contains(profile.Id))
				continue;

			if (profile.Requires is RunUpgradeId required && run.LevelOf(required) == 0)
				continue;

			int cost = profile.CostAt(run.LevelOf(profile.Id));
			if (cost > run.Stardust || cost >= cheapestCost)
				continue;

			cheapest = profile.Id;
			cheapestCost = cost;
			found = true;
		}

		// Nothing anywhere is affordable — the offer stands as it is, and the
		// greyed-out prices tell the player what to keep playing toward.
		if (!found || offer.Count == 0)
			return;

		int dearestSlot = 0;
		int dearestCost = -1;
		for (int i = 0; i < offer.Count; i++)
		{
			int cost = RunUpgrades.Get(offer[i]).CostAt(run.LevelOf(offer[i]));
			if (cost > dearestCost)
			{
				dearestCost = cost;
				dearestSlot = i;
			}
		}

		offer[dearestSlot] = cheapest;
	}

	private static RunUpgradeId Take(List<RunUpgradeId> from)
	{
		int index = RunState.Rng.RandiRange(0, from.Count - 1);
		RunUpgradeId id = from[index];
		from.RemoveAt(index);
		return id;
	}

	/// <summary>
	/// Takes the cheapest of a pool rather than a random one. Used for the
	/// missing abilities, so they arrive in a sensible order — dash, then rapid
	/// fire, then nova — instead of dangling the dearest one first at a player
	/// who cannot yet afford any of them.
	/// </summary>
	private RunUpgradeId TakeCheapest(List<RunUpgradeId> from)
	{
		int best = 0;
		int bestCost = int.MaxValue;

		for (int i = 0; i < from.Count; i++)
		{
			int cost = RunUpgrades.Get(from[i]).CostAt(run.LevelOf(from[i]));
			if (cost >= bestCost)
				continue;

			best = i;
			bestCost = cost;
		}

		RunUpgradeId id = from[best];
		from.RemoveAt(best);
		return id;
	}

	/// <summary>
	/// One box per offer, side by side. A row of full-width lines read as a
	/// wall of text over the arena; a card is a thing you point at, and three
	/// of them side by side can be compared at a glance — which is the whole
	/// job, given how little time the window gives you.
	/// </summary>
	private void BuildCards()
	{
		if (cards == null)
			return;

		foreach (Node child in cards.GetChildren())
			child.QueueFree();
		buttons.Clear();

		if (heading != null)
			heading.Text = string.Format(TranslationServer.Translate("UI_UPGRADE_HEADING"), run.Stardust);

		Font font = heading?.GetThemeFont("font");

		for (int i = 0; i < offer.Count; i++)
		{
			RunUpgrades.Profile profile = RunUpgrades.Get(offer[i]);
			int cost = profile.CostAt(run.LevelOf(profile.Id));
			bool canAfford = cost <= run.Stardust;
			Color tint = canAfford ? Affordable : TooDear;

			var card = new Button
			{
				Flat = false,
				Disabled = !canAfford,
				CustomMinimumSize = new Vector2(CardWidth, CardHeight),
				// The label stack below draws the text; the button itself is the
				// box and the hit target.
				Text = string.Empty
			};

			card.AddThemeStyleboxOverride("normal", CardStyle(new Color(0.09f, 0.07f, 0.13f, 0.88f), tint));
			card.AddThemeStyleboxOverride("hover", CardStyle(new Color(0.16f, 0.13f, 0.22f, 0.94f), Colors.White));
			card.AddThemeStyleboxOverride("pressed", CardStyle(new Color(0.2f, 0.16f, 0.26f, 0.96f), Colors.White));
			card.AddThemeStyleboxOverride("focus", CardStyle(new Color(0.16f, 0.13f, 0.22f, 0.94f), Colors.White));
			card.AddThemeStyleboxOverride("disabled", CardStyle(new Color(0.07f, 0.06f, 0.1f, 0.8f), TooDear));

			var stack = new VBoxContainer
			{
				MouseFilter = MouseFilterEnum.Ignore,
				AnchorRight = 1f,
				AnchorBottom = 1f,
				OffsetLeft = 12f,
				OffsetRight = -12f,
				OffsetTop = 10f,
				OffsetBottom = -10f
			};
			stack.AddThemeConstantOverride("separation", 2);

			stack.AddChild(CardLabel($"{i + 1}", font, 26, new Color(0.6f, 0.6f, 0.7f), HorizontalAlignment.Center));
			stack.AddChild(CardLabel(profile.Name, font, 30, tint, HorizontalAlignment.Center));

			Label effect = CardLabel(profile.Effect, font, 21, new Color(0.8f, 0.8f, 0.88f), HorizontalAlignment.Center);
			effect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			effect.SizeFlagsVertical = SizeFlags.ExpandFill;
			stack.AddChild(effect);

			stack.AddChild(CardLabel($"{cost}", font, 28, tint, HorizontalAlignment.Center));

			card.AddChild(stack);

			int index = i;
			card.Pressed += () => Buy(index);

			cards.AddChild(card);
			buttons.Add(card);
		}
	}

	private static StyleBoxFlat CardStyle(Color background, Color border)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = new Color(border, 0.55f),
			BorderWidthTop = 2,
			BorderWidthBottom = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10,
			CornerRadiusBottomRight = 10
		};
	}

	private static Label CardLabel(string text, Font font, int size, Color colour, HorizontalAlignment align)
	{
		var label = new Label
		{
			Text = text,
			HorizontalAlignment = align,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		if (font != null)
			label.AddThemeFontOverride("font", font);

		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", colour);
		return label;
	}

	/// <summary>
	/// Number keys, because the mouse is busy aiming. The window is short and the
	/// arena is still live — reaching for a card with the cursor costs a dodge.
	/// </summary>
	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (!Visible || inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
			return;

		int index = key.Keycode switch
		{
			Key.Key1 => 0,
			Key.Key2 => 1,
			Key.Key3 => 2,
			_ => -1
		};

		if (index < 0 || index >= offer.Count)
			return;

		Buy(index);
		GetViewport().SetInputAsHandled();
	}

	private void Buy(int index)
	{
		if (index < 0 || index >= offer.Count || !run.TryBuy(offer[index]))
			return;

		RunUpgrades.Profile profile = RunUpgrades.Get(offer[index]);
		GameManager.Of(this)?.Announce(profile.Name, profile.Effect, Affordable);

		// One purchase per break. The choice is meant to cost something, and a
		// break that empties the wallet is a different game.
		Visible = false;
	}
}
