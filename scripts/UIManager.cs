using Godot;
using System.Collections.Generic;

public partial class UIManager : Node
{
    private Control hud;
    private Control bossBar;
    private Label score, detail, wave, time, hint;
    private HBoxContainer abilityRow, effectsRow;
    private Player player;
    private RunState run;
    private BodySpawner spawner;
    private Sprite2D crosshair;
    private float refresh;
    private readonly Dictionary<string, Label> abilityLabels = new();
    private readonly Dictionary<string, Control> abilityChips = new();
    private readonly Dictionary<PowerUpKind, Control> effectChips = new();
    public override void _Ready()
    {
        var root=GetParent(); var layer=root.GetNode<CanvasLayer>("UI");
        foreach(Node child in layer.GetChildren())
            if(child is CanvasItem item && child.Name!="UpgradePrompt" && child.Name!="Announcer" && child.Name!="BossBar") item.Hide();
        player=root.GetNode<Player>("player"); run=GameManager.Of(this).Run; spawner=root.GetNode<BodySpawner>("BodySpawner");
        crosshair=root.GetNode<Sprite2D>("CrosshairLayer/Crosshair");
        crosshair.Texture=GD.Load<Texture2D>("res://art/cosmic/crosshair.svg");
        hud=new Control {Name="Hud", MouseFilter=Control.MouseFilterEnum.Ignore, Theme=ArcadeSkin.Theme()}; layer.AddChild(hud); ArcadeSkin.Fill(hud);
        var scoreBox=new VBoxContainer {Position=new Vector2(34,24),MouseFilter=Control.MouseFilterEnum.Ignore}; hud.AddChild(scoreBox);
        var caption=ArcadeSkin.Label("YOUR SCORE",18,ArcadeSkin.Muted);caption.HorizontalAlignment=HorizontalAlignment.Left;scoreBox.AddChild(caption);
        score=ArcadeSkin.Label("0",58);score.Name="Score";score.HorizontalAlignment=HorizontalAlignment.Left;scoreBox.AddChild(score);
        detail=ArcadeSkin.Label("x1.0",23,ArcadeSkin.Orange);detail.Name="LiveScore";detail.HorizontalAlignment=HorizontalAlignment.Left;scoreBox.AddChild(detail);
        var info=new VBoxContainer {AnchorLeft=1,AnchorRight=1,OffsetLeft=-320,OffsetRight=-34,OffsetTop=30,MouseFilter=Control.MouseFilterEnum.Ignore};hud.AddChild(info);
        wave=ArcadeSkin.Label("WAVE 01",26,ArcadeSkin.Cream);wave.Name="RunInfo";wave.HorizontalAlignment=HorizontalAlignment.Right; info.AddChild(wave);
        time=ArcadeSkin.Label("00:00",22,ArcadeSkin.Muted);time.HorizontalAlignment=HorizontalAlignment.Right;info.AddChild(time);
        abilityRow=new HBoxContainer {AnchorLeft=.5f,AnchorRight=.5f,AnchorTop=1,AnchorBottom=1,OffsetLeft=-320,OffsetRight=320,OffsetTop=-92,OffsetBottom=-28,Alignment=BoxContainer.AlignmentMode.Center};hud.AddChild(abilityRow);abilityRow.AddThemeConstantOverride("separation",12);
        foreach(var pair in new[]{("dash","dash"),("rapid_fire","rapid"),("nova","nova")})
        {
            var chip=new PanelContainer();chip.AddThemeStyleboxOverride("panel",ArcadeSkin.Box(new Color("392339"),new Color("986077"),18,2));
            var row=new HBoxContainer();chip.AddChild(row);row.AddChild(ArcadeSkin.Icon(pair.Item2,36));
            var key=ArcadeSkin.Label("",22);row.AddChild(key);abilityRow.AddChild(chip);abilityChips[pair.Item1]=chip;abilityLabels[pair.Item1]=key;
        }
        effectsRow=new HBoxContainer {AnchorTop=1,AnchorBottom=1,OffsetTop=-84,OffsetBottom=-28,OffsetLeft=34};hud.AddChild(effectsRow);effectsRow.AddThemeConstantOverride("separation",10);
        foreach(var pair in new[]{(PowerUpKind.Shield,"shield"),(PowerUpKind.Damage,"rapid")})
        {var icon=ArcadeSkin.Icon(pair.Item2,42);icon.TooltipText=PowerUps.Get(pair.Item1).Name;effectsRow.AddChild(icon);effectChips[pair.Item1]=icon;}
        hint=ArcadeSkin.Label("WASD / left stick to move     •     Mouse / right stick to aim     •     Click / RT to shoot",23,ArcadeSkin.Muted);
        hint.Name="HowTo";hint.AnchorLeft=.5f;hint.AnchorRight=.5f;hint.AnchorTop=1;hint.AnchorBottom=1;hint.OffsetLeft=-650;hint.OffsetRight=650;hint.OffsetTop=-140;hint.OffsetBottom=-100;hud.AddChild(hint);
        float scale=GameSettings.Instance?.UiScale??1;
        foreach(var label in new[]{score,detail,wave,time,hint}) label.AddThemeFontSizeOverride("font_size",Mathf.RoundToInt(label.GetThemeFontSize("font_size")*scale));
        bossBar=root.GetNode<Control>("UI/BossBar");
        bossBar.AnchorLeft=.5f;bossBar.AnchorRight=.5f;bossBar.AnchorTop=0;bossBar.AnchorBottom=0;
        bossBar.OffsetLeft=-300;bossBar.OffsetRight=300;bossBar.OffsetTop=144;bossBar.OffsetBottom=222;
        if(bossBar.FindChild("Name",true,false) is Label bossName)bossName.AddThemeFontSizeOverride("font_size",23);
        if(bossBar.FindChild("Health",true,false) is ProgressBar health)
        {health.AddThemeStyleboxOverride("background",ArcadeSkin.Box(new Color("392339"),ArcadeSkin.Ink,8,2));health.AddThemeStyleboxOverride("fill",ArcadeSkin.Box(ArcadeSkin.Berry,ArcadeSkin.Orange,8,1));}
        HideCursor(); UpdateLabels();
    }
    private static string KeyName(string action)=>OS.GetKeycodeString(GameSettings.GetActionKey(action)).ToUpperInvariant();
    public override void _Process(double delta)
    {
        if(crosshair.Visible)crosshair.GlobalPosition=GetViewport().CanvasTransform*player.AimPosition;
        refresh-=(float)delta;if(refresh<=0){refresh=.1f;UpdateLabels();}
    }
    private void UpdateLabels()
    {
        score.Text=$"{run.Score:N0}";detail.Text=$"x{run.ScoreMultiplier:0.0}"+(run.Streak>=2?$"   •   {run.Streak} COMBO":"");
        wave.Text=$"WAVE {spawner.WaveNumber:00}";time.Text=ScoreManager.FormatTime(run.SurvivalTime);
        hint.Visible=run.SurvivalTime<8;
        SetAbility("dash",run.HasDash,player.CanDash&&player.GetDashCooldownPercent()>=1,player.GetDashCooldownPercent(),"B");
        SetAbility("rapid_fire",run.HasRapidFire,player.GetRapidFireCooldownPercent()>=1,player.GetRapidFireCooldownPercent(),"X");
        SetAbility("nova",run.HasNova,run.Mass>=player.NovaVent,run.Mass/player.NovaVent,"Y");
        foreach(var pair in effectChips) {pair.Value.Visible=pair.Key==PowerUpKind.Shield?run.HasShield:run.TimeLeft(pair.Key)>0;pair.Value.Modulate=new Color(1,1,1,pair.Key!=PowerUpKind.Shield&&run.TimeLeft(pair.Key)<2?.5f:1);}
    }
    private void SetAbility(string key,bool owned,bool ready,float fraction,string pad)
    {abilityChips[key].Visible=owned;abilityChips[key].Modulate=ready?Colors.White:new Color(.65f,.6f,.7f);abilityLabels[key].Text=ready?$"{KeyName(key)} / {pad}":$"{Mathf.Clamp(fraction,0,1):P0}";}
    public void SetGameplayVisible(bool visible) {hud.Visible=visible;if(!visible)GetParent().GetNode<Control>("UI/Announcer").Hide();}
    public void ShowCursor() {Input.MouseMode=Input.MouseModeEnum.Visible;crosshair.Hide();}
    public void HideCursor() {Input.MouseMode=Input.MouseModeEnum.Hidden;crosshair.Show();}
}
