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

	private RunState run;
	private VBoxContainer cards;
	private Label heading;
	private readonly List<RunUpgradeId> offer = new();
	private readonly List<Button> buttons = new();

	public override void _Ready()
	{
		cards = GetNodeOrNull<VBoxContainer>("Cards");
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

		foreach (RunUpgrades.Profile profile in RunUpgrades.All)
		{
			if (run.IsMaxed(profile.Id))
				continue;

			if (profile.Family == UpgradeFamily.Mass)
				massPool.Add(profile.Id);
			else
				pool.Add(profile.Id);
		}

		if (massPool.Count > 0)
			offer.Add(Take(massPool));

		while (offer.Count < OfferCount && (pool.Count > 0 || massPool.Count > 0))
			offer.Add(Take(pool.Count > 0 ? pool : massPool));

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

	private void BuildCards()
	{
		if (cards == null)
			return;

		foreach (Node child in cards.GetChildren())
			child.QueueFree();
		buttons.Clear();

		if (heading != null)
			heading.Text = string.Format(TranslationServer.Translate("UI_UPGRADE_HEADING"), run.Stardust);

		for (int i = 0; i < offer.Count; i++)
		{
			RunUpgrades.Profile profile = RunUpgrades.Get(offer[i]);
			int cost = profile.CostAt(run.LevelOf(profile.Id));
			bool canAfford = cost <= run.Stardust;

			var button = new Button
			{
				Text = $"{i + 1}   {profile.Name}   ·   {profile.Effect}   ·   {cost}",
				Flat = true,
				Disabled = !canAfford,
				CustomMinimumSize = new Vector2(0, 56)
			};

			button.AddThemeColorOverride("font_color", canAfford ? Affordable : TooDear);
			button.AddThemeColorOverride("font_disabled_color", TooDear);
			button.AddThemeColorOverride("font_hover_color", Colors.White);
			button.AddThemeColorOverride("font_focus_color", Colors.White);
			button.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
			button.AddThemeConstantOverride("outline_size", 8);
			button.AddThemeFontSizeOverride("font_size", 32);

			int index = i;
			button.Pressed += () => Buy(index);

			cards.AddChild(button);
			buttons.Add(button);
		}
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
