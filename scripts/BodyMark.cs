using Godot;

// Armour is an actual leading plate. Exploders flash their body before contact,
// and wear a faint ring the size of their blast, so killing one too close is a
// choice the player can see rather than a surprise.
public partial class BodyMark : Node2D
{
    private static readonly Color BlastRing = new(1f, 0.55f, 0.4f, 0.35f);

    private Sprite2D face;
    private Body body;
    private bool flare;
    private float time;

    public override void _Ready()
    {
        if(GetParent() is not Body parent)return;
        body=parent;
        Visible=true;flare=body.Kind==BodyKind.Flare;face=body.GetNode<Sprite2D>("Sprite2D");
        if(body.Kind==BodyKind.Bulwark)AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/armour_plate.svg"),Scale=Vector2.One*.26f});
        if(!flare)SetProcess(false);
    }

    public override void _Process(double delta)
    {
        time+=(float)delta;
        face.SelfModulate=Colors.White.Lerp(new Color(1.4f,.65f,.55f),(.5f+.5f*Mathf.Sin(time*8))*.65f);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if(!flare||body==null)return;
        // Drawn in the body's space, so undo its scale to keep the ring true to size.
        float radius=FlareBehaviour.BlastRadius/Mathf.Max(Mathf.Abs(body.Scale.X),.01f);
        int dashes=28;float spin=time*.4f;
        for(int i=0;i<dashes;i++)
        {
            float from=spin+Mathf.Tau*i/dashes;
            DrawArc(Vector2.Zero,radius,from,from+Mathf.Tau/dashes*.55f,4,BlastRing,2.5f/Mathf.Max(Mathf.Abs(body.Scale.X),.01f),true);
        }
    }
}
