using Godot;

public partial class Menu : Node
{
    private AudioStreamPlayer buttonSound,hoverSound;
    private Node2D hero;
    private Sprite2D expression;
    private float time;
    public override void _Ready()
    {
        AddChild(new CosmicBackdrop());
        buttonSound=new AudioStreamPlayer {Stream=GD.Load<AudioStream>("res://sounds/button_tap.wav"),Bus="SFX"};AddChild(buttonSound);
        hoverSound=new AudioStreamPlayer {Stream=GD.Load<AudioStream>("res://sounds/button_tick.wav"),Bus="SFX"};AddChild(hoverSound);
        var layer=new CanvasLayer {Name="UI"};AddChild(layer);
        // One column, centred top to bottom: the title, what you do, then the
        // buttons right under it. Nothing parked at the screen's edges.
        var column=new VBoxContainer {Name="Column"};column.AddThemeConstantOverride("separation",0);
        Place(column,layer,.105f,.5f,.425f,.5f);column.GrowVertical=Control.GrowDirection.Both;
        var title=ArcadeSkin.Label("GRINFINITY",112);title.HorizontalAlignment=HorizontalAlignment.Left;column.AddChild(title);
        var note=ArcadeSkin.Label("Move • Aim • Shoot • Survive",26,ArcadeSkin.Muted);note.HorizontalAlignment=HorizontalAlignment.Left;column.AddChild(note);
        column.AddChild(new Control {CustomMinimumSize=new Vector2(0,44)});
        var buttons=new VBoxContainer {Name="Buttons"};buttons.AddThemeConstantOverride("separation",15);column.AddChild(buttons);
        var play=AddButton(buttons,"PlayButton","PLAY","play",()=>GoTo("game"),true);
        AddButton(buttons,"LeaderboardButton","LEADERBOARD","trophy",()=>GoTo("leaderboard"));
        AddButton(buttons,"SettingsButton","SETTINGS","gear",()=>GoTo("settings"));
        AddButton(buttons,"StatsButton","MY PLANET","planet",()=>GoTo("stats"));
        var footer=new HBoxContainer();footer.AddThemeConstantOverride("separation",16);buttons.AddChild(footer);
        AddButton(footer,"CreditsButton","CREDITS","star",()=>GoTo("credits"));
        AddButton(footer,"QuitButton","QUIT","power",()=>GameSettings.Instance.QuitGame());
        var best=ArcadeSkin.Label($"BEST TIME   {ScoreManager.FormatTime(ScoreManager.BestTime)}",28,ArcadeSkin.Orange);
        Place(best,layer,.56f,.77f,.9f,.83f);
        best.Visible=ScoreManager.BestTime>0f;
        hero=new Node2D();AddChild(hero);
        // The ring is a badge: worn once the player has a run on the leaderboard.
        bool ringed=WorldRings.Earned;
        if(ringed)hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/ring_2_back.svg"),Scale=Vector2.One*.78f});
        hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>($"res://art/cosmic/planet_{GameSettings.Instance.World}.svg")});
        expression=new Sprite2D {Texture=Worlds.Face(GameSettings.Instance.World)};hero.AddChild(expression);
        if(ringed)hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/ring_2_front.svg"),Scale=Vector2.One*.78f});
        hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/blaster.svg"),Position=new Vector2(153,52),Scale=Vector2.One*.65f,Rotation=.12f});
        hero.Scale=Vector2.One*2;
        Input.MouseMode=Input.MouseModeEnum.Visible;play.GrabFocus();
    }
    private static void Place(Control control,Node parent,float left,float top,float right,float bottom)
    {parent.AddChild(control);control.AnchorLeft=left;control.AnchorRight=right;control.AnchorTop=top;control.AnchorBottom=bottom;}
    private Button AddButton(BoxContainer rows,string name,string text,string icon,System.Action action,bool primary=false)
    {
        var button=ArcadeSkin.Button(text,()=>{buttonSound.Play();action();},primary,icon);button.Name=name;
        UiIcons.On(button,icon,38);
        button.CustomMinimumSize=new Vector2(0,78);button.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        button.MouseEntered+=()=>hoverSound.Play();rows.AddChild(button);return button;
    }
    public override void _Process(double delta)
    {
        time+=(float)delta;var size=GetViewport().GetVisibleRect().Size;
        hero.Position=size*new Vector2(.73f,.53f)+new Vector2(0,Mathf.Sin(time*1.4f)*10);
        hero.Rotation=Mathf.Sin(time*.8f)*.055f;
        expression.Texture=Worlds.Face(GameSettings.Instance.World,time%4.5f>4.35f);
    }
    private void GoTo(string name)=>SceneTransition.Instance.ChangeScene($"res://scenes/{name}.tscn");
}
