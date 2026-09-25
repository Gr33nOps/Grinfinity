using Godot;

// Two illustrated halves let the near band pass in front of the planet.
public partial class WorldRings : Node2D
{
    [Export] public bool NearHalf {get;set;}
    private RunState run;
    private Node2D world;
    private Sprite2D band;
    private int shownTier=-1;
    private float presence;
    public override void _Ready()
    {
        run=GameManager.Of(this)?.Run;world=GetParent<Node2D>();TopLevel=true;
        band=new Sprite2D();AddChild(band);
    }
    public override void _Process(double delta)
    {
        if(run==null)return;
        GlobalPosition=world.GlobalPosition;Scale=world.Scale;
        int tier=run.RingTier;
        if(tier>0&&tier!=shownTier)
        {shownTier=tier;band.Texture=GD.Load<Texture2D>($"res://art/cosmic/ring_{Mathf.Min(tier,3)}_{(NearHalf?"front":"back")}.svg");}
        presence=Mathf.MoveToward(presence,tier>0?1:0,(float)delta*3);
        band.Visible=presence>.01f;band.Modulate=new Color(1,1,1,presence);
        band.Scale=Vector2.One*.25f*(1+.055f*Mathf.Max(tier-1,0))*Mathf.Lerp(.85f,1,presence);
    }
}
