using Godot;

// Armour is an actual leading plate. Exploders flash their body before contact.
public partial class BodyMark : Node2D
{
    private Sprite2D face;
    private bool flare;
    private float time;
    public override void _Ready()
    {
        if(GetParent() is not Body body)return;
        Visible=true;flare=body.Kind==BodyKind.Flare;face=body.GetNode<Sprite2D>("Sprite2D");
        if(body.Kind==BodyKind.Bulwark)AddChild(new Sprite2D {Texture=GD.Load<Texture2D>("res://art/cosmic/armour_plate.svg"),Scale=Vector2.One*.26f});
    }
    public override void _Process(double delta)
    {
        if(!flare)return;time+=(float)delta;
        face.SelfModulate=Colors.White.Lerp(new Color(1.4f,.65f,.55f),(.5f+.5f*Mathf.Sin(time*8))*.65f);
    }
}
