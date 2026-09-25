using Godot;

/// <summary>Smiling planet and blaster rotate together; animation never changes the hitbox.</summary>
public partial class PlanetVisual : Node2D
{
    private Player player;
    private Sprite2D body, face, gun, shield;
    private float time, recoil, celebration, hurt;
    private Texture2D normal, blink, happy;
    public override void _Ready()
    {
        player = GetParent<Player>();
        body = player.GetNode<Sprite2D>("Sprite2D");
        body.Texture = GD.Load<Texture2D>($"res://art/cosmic/planet_{GameSettings.Instance?.World ?? 1}.svg");
        body.Position = Vector2.Zero; body.Scale = Vector2.One * .32f; body.FlipH = body.FlipV = false;
        normal=GD.Load<Texture2D>("res://art/cosmic/face.svg"); blink=GD.Load<Texture2D>("res://art/cosmic/face_blink.svg"); happy=GD.Load<Texture2D>("res://art/cosmic/face_happy.svg");
        face = new Sprite2D { Texture=normal, ZIndex=2 }; body.AddChild(face);
        gun = new Sprite2D { Texture=GD.Load<Texture2D>("res://art/cosmic/blaster.svg"), Position=new Vector2(49,12), Scale=Vector2.One*.27f, ZIndex=3 }; AddChild(gun);
        player.GetNode<Node2D>("shootyPart").Position=new Vector2(75,12);
        shield=new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/shield_shell.svg"),Scale=Vector2.One*.39f,ZIndex=4};AddChild(shield);
        player.HitTaken += () => hurt=.3f;
    }
    public void Kick() { recoil=1; }
    public void Celebrate() { celebration=1.1f; }
    public override void _Process(double delta)
    {
        float step=(float)delta; time+=step; recoil=Mathf.MoveToward(recoil,0,step*9); celebration=Mathf.Max(0,celebration-step); hurt=Mathf.Max(0,hurt-step);
        body.Rotation= Mathf.Clamp(player.Velocity.X*.00015f,-.12f,.12f);
        body.FlipV=false;
        float pulse=1f + .018f*Mathf.Sin(time*3) + .065f*recoil;
        body.Scale=new Vector2(.32f/pulse,.32f*pulse);
        face.Texture=celebration>0 ? happy : time%4.2f>4.06f ? blink : normal;
        body.SelfModulate=hurt>0 ? new Color(1.5f,.8f,.85f) : Colors.White;
        gun.Position=new Vector2(49-recoil*5,12);
        gun.Rotation=-recoil*.1f;
        gun.FlipV=Mathf.Cos(player.Rotation)<0;
        gun.Visible=body.Visible;
        shield.Visible=body.Visible && GameManager.Of(this)?.Run.HasShield==true;
        shield.Scale=Vector2.One*(.39f+.003f*Mathf.Sin(time*2));
    }
}
