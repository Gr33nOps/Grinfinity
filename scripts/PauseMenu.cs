using Godot;

public partial class PauseMenu : Control
{
    [Signal] public delegate void ResumeGameEventHandler();
    [Signal] public delegate void GiveUpGameEventHandler();
    private Control pausePage, settingsPage, restartPage;
    private Button resumeButton;
    private Control panel;
    public override void _Ready()
    {
        foreach(Node child in GetChildren()) if(child is CanvasItem item)item.Hide();
        ProcessMode=ProcessModeEnum.Always;
        pausePage=new Control();AddChild(pausePage);
        var rows=ArcadeSkin.Modal(pausePage,"TAKE A BREATHER",600);
        panel=rows.GetParent<Control>();
        rows.AddChild(ArcadeSkin.Label("Your orbit can wait.",24,ArcadeSkin.Muted));
        resumeButton=ArcadeSkin.Button("KEEP GOING",()=>{GameManager.Of(this)?.PlayButtonSound();EmitSignal(SignalName.ResumeGame);},true);rows.AddChild(resumeButton);
        var restart=ArcadeSkin.Button("RESTART",()=>{pausePage.Hide();restartPage.Show();});restart.Name="Restart";rows.AddChild(restart);
        var options=ArcadeSkin.Button("SETTINGS",ShowOptions);options.Name="Options";rows.AddChild(options);
        rows.AddChild(ArcadeSkin.Button("END RUN",()=>EmitSignal(SignalName.GiveUpGame)));
        restartPage=new Control();AddChild(restartPage);
        var confirm=ArcadeSkin.Modal(restartPage,"A FRESH ORBIT?",660);
        confirm.AddChild(ArcadeSkin.Label("This unfinished run will not be recorded.",24,ArcadeSkin.Muted));
        confirm.AddChild(ArcadeSkin.Button("RESTART NOW",()=>GameManager.Of(this)?.RestartOrbit(),true));
        confirm.AddChild(ArcadeSkin.Button("KEEP THIS RUN",()=>{restartPage.Hide();pausePage.Show();resumeButton.GrabFocus();}));restartPage.Hide();
        settingsPage=new Control();AddChild(settingsPage);
        var settings=ArcadeSkin.Modal(settingsPage,"MAKE IT COMFY",660);
        var config=GameSettings.Instance;
        AddSlider(settings,"MASTER",config.MasterVolume,config.SetMasterVolume);
        AddSlider(settings,"MUSIC",config.MusicVolume,config.SetMusicVolume);
        AddSlider(settings,"SOUND EFFECTS",config.SfxVolume,config.SetSfxVolume);
        AddSlider(settings,"SCREEN SHAKE",config.ShakeIntensity,config.SetShakeIntensity);
        settings.AddChild(ArcadeSkin.Button("BACK",()=>CloseOptions(),true));settingsPage.Hide();Hide();
    }
    private void ShowOptions(){pausePage.Hide();settingsPage.Show();}
    private void AddSlider(VBoxContainer rows,string title,float value,System.Action<float> apply)
    {
        var label=ArcadeSkin.Label($"{title}   {value:P0}",24);rows.AddChild(label);
        var slider=new HSlider {MinValue=0,MaxValue=1,Step=.05,Value=value,CustomMinimumSize=new Vector2(0,30)};rows.AddChild(slider);
        slider.ValueChanged+=number=>{apply((float)number);label.Text=$"{title}   {number:P0}";GameSettings.Instance.SaveSettings();};
    }
    public bool CloseOptions()
    {
        if(!settingsPage.Visible&&!restartPage.Visible)return false;
        settingsPage.Hide();restartPage.Hide();pausePage.Show();resumeButton.GrabFocus();return true;
    }
    public void ShowPauseMenu(){Show();pausePage.Show();settingsPage.Hide();restartPage.Hide();resumeButton.GrabFocus();Callable.From(()=>ArcadeSkin.Pop(panel)).CallDeferred();}
    public void HidePauseMenu(){Hide();}
}
