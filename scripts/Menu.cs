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
        var title=ArcadeSkin.Label("GRINFINITY",112);title.HorizontalAlignment=HorizontalAlignment.Left;Place(title,layer,.1f,.14f,.69f,.28f);
        var buttons=new VBoxContainer {Name="Buttons"};buttons.AddThemeConstantOverride("separation",15);Place(buttons,layer,.105f,.40f,.425f,.84f);
        var play=AddButton(buttons,"PlayButton","PLAY",()=>GoTo("game"),true);
        AddButton(buttons,"LeaderboardButton","HIGH SCORES",()=>GoTo("leaderboard"));
        AddButton(buttons,"SettingsButton","SETTINGS",()=>GoTo("settings"));
        AddButton(buttons,"StatsButton","MY PLANET",()=>GoTo("stats"));
        var footer=new HBoxContainer();footer.AddThemeConstantOverride("separation",16);buttons.AddChild(footer);
        AddButton(footer,"CreditsButton","CREDITS",()=>GoTo("credits"));
        AddButton(footer,"QuitButton","QUIT",()=>GameSettings.Instance.QuitGame());
        var best=ArcadeSkin.Label(ScoreManager.BestScore>0?$"YOUR BEST   {ScoreManager.BestScore:N0}":"YOUR BEST   0",26,ArcadeSkin.Muted);
        Place(best,layer,.56f,.80f,.94f,.88f);
        var note=ArcadeSkin.Label("Move • Aim • Shoot • Grow",24,ArcadeSkin.Muted);Place(note,layer,.105f,.92f,.425f,.98f);
        hero=new Node2D();AddChild(hero);
        hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/ring_2_back.svg"),Scale=Vector2.One*.78f});
        hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>($"res://art/cosmic/planet_{GameSettings.Instance.World}.svg")});
        expression=new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/face.svg")};hero.AddChild(expression);
        hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/ring_2_front.svg"),Scale=Vector2.One*.78f});
        hero.AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/blaster.svg"),Position=new Vector2(153,52),Scale=Vector2.One*.65f,Rotation=.12f});
        hero.Scale=Vector2.One*2;
        Input.MouseMode=Input.MouseModeEnum.Visible;play.GrabFocus();
    }
    private static void Place(Control control,Node parent,float left,float top,float right,float bottom)
    {parent.AddChild(control);control.AnchorLeft=left;control.AnchorRight=right;control.AnchorTop=top;control.AnchorBottom=bottom;}
    private Button AddButton(BoxContainer rows,string name,string text,System.Action action,bool primary=false)
    {
        var button=ArcadeSkin.Button(text,()=>{buttonSound.Play();action();},primary);button.Name=name;
        button.CustomMinimumSize=new Vector2(0,78);button.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        button.MouseEntered+=()=>hoverSound.Play();rows.AddChild(button);return button;
    }
    public override void _Process(double delta)
    {
        time+=(float)delta;var size=GetViewport().GetVisibleRect().Size;
        hero.Position=size*new Vector2(.73f,.53f)+new Vector2(0,Mathf.Sin(time*1.4f)*10);
        hero.Rotation=Mathf.Sin(time*.8f)*.055f;
        expression.Texture=GD.Load<Texture2D>(time%4.5f>4.35f?"res://art/cosmic/face_blink.svg":"res://art/cosmic/face.svg");
    }
    private void GoTo(string name)=>SceneTransition.Instance.ChangeScene($"res://scenes/{name}.tscn");
}
