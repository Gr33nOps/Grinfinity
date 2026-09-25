using Godot;

/// <summary>Gameplay-only illustrated space layers. The main menu background is untouched.</summary>
public partial class CosmicBackdrop : Node2D
{
    private float time;
    public override void _Ready() { ZIndex=-90; }
    public override void _Process(double delta) { time+=(float)delta; QueueRedraw(); }
    public override void _Draw()
    {
        Vector2 size=GetViewportRect().Size;
        DrawRect(new Rect2(Vector2.Zero,size),new Color("211b35"));
        // Wide, low-contrast ribbons create depth without competing with threats.
        for(int band=0;band<3;band++)
        {
            var points=new Vector2[34];
            for(int i=0;i<17;i++) {float x=size.X*i/16f; float y=size.Y*(.15f+band*.32f)+Mathf.Sin(i*.28f+band+time*.025f)*size.Y*.1f;points[i]=new Vector2(x,y);points[33-i]=new Vector2(x,y+size.Y*.15f);}
            DrawColoredPolygon(points,new Color(.27f+band*.015f,.17f,.31f,.16f));
        }
        DrawCircle(new Vector2(size.X*.84f,size.Y*.22f),130,new Color("352640"));
        DrawCircle(new Vector2(size.X*.87f,size.Y*.18f),122,new Color("211b35"));
        DrawArc(new Vector2(size.X*.16f,size.Y*.8f),190,.1f,2.8f,60,new Color(.56f,.35f,.47f,.14f),18,true);
        for(int i=0;i<110;i++)
        {
            float x=Mathf.PosMod(i*317.71f+Mathf.Sin(i*1.71f)*140+time*(i%3+1)*.9f,size.X);
            float y=Mathf.PosMod(i*173.23f+Mathf.Cos(i*2.3f)*120,size.Y);
            float radius=i%13==0?2.5f:1f;
            Color color=new Color(1,.88f,.73f,i%3==0?.45f:.18f);
            DrawCircle(new Vector2(x,y),radius,color);
            if(i%13==0) {DrawLine(new Vector2(x-5,y),new Vector2(x+5,y),color,1.3f,true);DrawLine(new Vector2(x,y-5),new Vector2(x,y+5),color,1.3f,true);}
        }
    }
}
