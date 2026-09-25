using System.Collections.Generic;
using Godot;

/// <summary>One untimed, free upgrade at each wave break. Simulation pauses while reading.</summary>
public partial class UpgradePrompt : Control
{
	private const int OfferCount = 3;





	private RunState run;
	private BodySpawner spawner;

	private HBoxContainer cards;
	private Label heading;
    private int clearedWave=1;
	private readonly List<RunUpgradeId> offer = new();
	private readonly List<Button> buttons = new();

    public override void _Ready()
    {
        foreach(Node child in GetChildren()) if(child is CanvasItem item) item.Hide();
        ProcessMode=ProcessModeEnum.Always;
        var rows=ArcadeSkin.Modal(this,"CHOOSE AN UPGRADE",1040);
        heading=ArcadeSkin.Label("WAVE CLEAR",23,ArcadeSkin.Orange);rows.AddChild(heading);
        cards=new HBoxContainer();cards.AddThemeConstantOverride("separation",18);rows.AddChild(cards);
        rows.AddChild(ArcadeSkin.Label("Choose one • 1 / 2 / 3 or D-pad + A • Take your time",22,ArcadeSkin.Muted));
        var skip=ArcadeSkin.Button("KEEP CURRENT BUILD",Skip);skip.Name="SkipUpgrade";
        skip.CustomMinimumSize=new Vector2(0,52);rows.AddChild(skip);
        Hide();
    }

	/// <summary>Wires itself to the spawner's wave signals. Called by GameManager.</summary>
	public void Bind(RunState state, BodySpawner spawner)
	{
		run = state;
		this.spawner = spawner;
		if (spawner == null)
			return;

		spawner.WaveCleared += OnWaveCleared;
		spawner.WaveStarted += OnWaveStarted;
	}

	private void OnWaveCleared(int waveNumber)
	{
		if (run == null)
			return;

        clearedWave=waveNumber;
        string milestone=run.GrantWaveMilestones(waveNumber);
		RollOffer();

		// Nothing left to offer — every upgrade is maxed. Better to stay quiet
		// than to show an empty box.
		if (offer.Count == 0)
			return;

		GameManager.Of(this)?.BeginBoostChoice();
		Show();
        heading.Text=string.IsNullOrEmpty(milestone)?$"WAVE {waveNumber:00} CLEAR • ONE FREE BOOST":milestone;
        RestoreFocus();
        Callable.From(()=>ArcadeSkin.Pop(cards.GetParent<Control>().GetParent<Control>())).CallDeferred();
	}


	private void OnWaveStarted(int waveNumber)
	{
		Visible = false;
	}

	/// <summary>
	/// Offers useful upgrades whose wave and ability prerequisites are satisfied.
	/// </summary>
    private void RollOffer()
    {
        offer.Clear();var pool=new List<RunUpgradeId>();
        foreach(var profile in RunUpgrades.All)
        {
            if(profile.IsUnlock||profile.RequiredWave(run.LevelOf(profile.Id))>clearedWave||run.IsMaxed(profile.Id))continue;
            if(profile.Requires is RunUpgradeId required&&run.LevelOf(required)==0)continue;
            if(profile.Equips!=null&&run.Weapon!=WeaponId.Comet)continue;
            pool.Add(profile.Id);
        }
        // Preserve a standard build option whenever an eligible one exists.
        var standard=pool.FindAll(id=>id!=RunUpgradeId.FanShot&&RunUpgrades.Get(id).Equips==null);
        if(standard.Count>0){var id=Take(standard);offer.Add(id);pool.Remove(id);}
        // Introduce spread ranks predictably, instead of making them a lucky roll.
        if(pool.Contains(RunUpgradeId.FanShot)&&RunUpgrades.FanShot.RequiredWave(run.LevelOf(RunUpgradeId.FanShot))==clearedWave)
        {offer.Add(RunUpgradeId.FanShot);pool.Remove(RunUpgradeId.FanShot);}
        foreach(var id in pool.ToArray())if(RunUpgrades.Get(id).Equips!=null&&RunUpgrades.Get(id).MinWave==clearedWave){offer.Add(id);pool.Remove(id);break;}
        if(offer.Exists(id=>RunUpgrades.Get(id).Equips!=null))pool.RemoveAll(id=>RunUpgrades.Get(id).Equips!=null);
        while(offer.Count<OfferCount&&pool.Count>0)
        {
            var id=Take(pool);offer.Add(id);
            if(RunUpgrades.Get(id).Equips!=null)pool.RemoveAll(other=>RunUpgrades.Get(other).Equips!=null);
        }
        BuildCards();
    }

	private static RunUpgradeId Take(List<RunUpgradeId> from)
	{
		int index = RunState.Rng.RandiRange(0, from.Count - 1);
		RunUpgradeId id = from[index];
		from.RemoveAt(index);
		return id;
	}

	/// <summary>
	/// One box per offer, side by side. A row of full-width lines read as a
	/// wall of text over the arena; a card is a thing you point at, and three
	/// of them side by side can be compared at a glance — which is the whole
	/// job, given how little time the window gives you.
	/// </summary>
    public void RestoreFocus(){if(buttons.Count>0)buttons[0].GrabFocus();}
    private static string IconFor(RunUpgradeId id)=>id switch
    {
        RunUpgradeId.UnlockDash or RunUpgradeId.QuickerDash=>"dash",
        RunUpgradeId.UnlockNova or RunUpgradeId.BiggerNova=>"nova",
        RunUpgradeId.Piercing or RunUpgradeId.IonLance=>"pierce",
        RunUpgradeId.FanShot or RunUpgradeId.DebrisCannon=>"spread",
        _=>"rapid"
    };
    private void BuildCards()
    {
        foreach(Node child in cards.GetChildren()){cards.RemoveChild(child);child.QueueFree();}
        buttons.Clear();
        for(int i=0;i<offer.Count;i++)
        {
            var profile=RunUpgrades.Get(offer[i]);int index=i;
            var card=ArcadeSkin.Button("",()=>Buy(index));
            card.CustomMinimumSize=new Vector2(318,320);card.SizeFlagsHorizontal=SizeFlags.ExpandFill;
            var stack=new VBoxContainer {MouseFilter=MouseFilterEnum.Ignore,OffsetLeft=18,OffsetTop=22,OffsetRight=-18,OffsetBottom=-20,AnchorRight=1,AnchorBottom=1};
            stack.AddThemeConstantOverride("separation",14);card.AddChild(stack);
            stack.AddChild(ArcadeSkin.Icon(IconFor(offer[i]),78));
            var name=ArcadeSkin.Label(profile.Name,29);name.AutowrapMode=TextServer.AutowrapMode.WordSmart;stack.AddChild(name);
            var effect=ArcadeSkin.Label(profile.Effect,23,ArcadeSkin.Muted);effect.AutowrapMode=TextServer.AutowrapMode.WordSmart;effect.SizeFlagsVertical=SizeFlags.ExpandFill;stack.AddChild(effect);
            string rank=profile.MaxLevel>1?$"LEVEL {run.LevelOf(profile.Id)+1}/{profile.MaxLevel}":"SELECT";
            stack.AddChild(ArcadeSkin.Label($"{i+1}   •   {rank}",23,ArcadeSkin.Orange));cards.AddChild(card);buttons.Add(card);
        }
    }

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (Visible && GameManager.Of(this)?.IsPaused != true && inputEvent is InputEventJoypadButton && (inputEvent.IsActionPressed("ui_left") || inputEvent.IsActionPressed("ui_right")))
		{
			int selected = buttons.FindIndex(b => b.HasFocus());
			for (int attempt = 0; attempt < buttons.Count; attempt++)
			{
				selected = Mathf.PosMod(selected + (inputEvent.IsActionPressed("ui_left") ? -1 : 1), buttons.Count);
				if (!buttons[selected].Disabled) { buttons[selected].GrabFocus(); break; }
			}
			GetViewport().SetInputAsHandled();
			return;
		}
		if (!Visible || GameManager.Of(this)?.IsPaused == true || inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
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

    private void Skip()
    {
        if(!Visible||GameManager.Of(this)?.IsPaused==true)return;
        Hide();GameManager.Of(this)?.EndBoostChoice();spawner?.FinishCalm();
    }

	private void Buy(int index)
	{
		if (!Visible || GameManager.Of(this)?.IsPaused == true || index < 0 || index >= offer.Count || !run.TryBuy(offer[index]))
			return;

		RunUpgrades.Profile profile = RunUpgrades.Get(offer[index]);
		Visible=false;
        GameManager.Of(this)?.EndBoostChoice();
        GameManager.Of(this)?.Announce(profile.Name, "BOOST EQUIPPED", ArcadeSkin.Orange);
		GameManager.Of(this)?.PlayUpgradeSound();
		spawner?.FinishCalm();

		// Exactly one boost per break, even if two inputs arrive together.
		Visible = false;
	}
}
