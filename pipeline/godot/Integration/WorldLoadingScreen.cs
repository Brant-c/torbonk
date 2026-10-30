using Godot;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

/// <summary>Original April bitmaps with a Godot loading lifecycle; no Scaleform code is executed.</summary>
public partial class WorldLoadingScreen : CanvasLayer
{
    private const string Art = "res://Assets/UI/loading/";
    public GameplayConfiguration Run { get; set; } = null!;
    private Control _canvas = null!;
    private TextureProgressBar _bar = null!;
    private TextureRect _spinner = null!;
    private Label _status = null!;
    private bool _requested, _switched, _failed;
    private int _readyFrames;
    public override void _Ready()
    {
        Layer = 120; ProcessMode = ProcessModeEnum.Always;
        var root = new Control(); AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var black = new ColorRect { Color = Colors.Black }; root.AddChild(black);
        black.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _canvas = new Control { Size = new Vector2(1600, 900), Theme = FrontendSkin.MakeTheme() }; root.AddChild(_canvas);
        string prefix = Run.MapId switch { "soa_p3" => "eternity_vault", "balmorra" => "bal", "dromund" => "dro", _ => Run.MapId };
        Picture("background_tile", Vector2.Zero, new Vector2(1600,900));
        Picture(prefix + "_overlay", new Vector2(0,100), new Vector2(1600,675));
        Picture("slice_9", new Vector2(595,10), new Vector2(410,133));
        // The recovered trim is one edge, not a complete progress-bar frame.
        // Reflect it to enclose a centered channel, with the fill parented inside it.
        var frame=new Control {Position=new Vector2(400,770),Size=new Vector2(800,51)};
        _canvas.AddChild(frame);
        var top=FrontendSkin.Art(Art+"slice_12.png",frame.Size);
        var bottom=FrontendSkin.Art(Art+"slice_12.png",frame.Size);bottom.FlipV=true;bottom.FlipH=true;
        frame.AddChild(top);frame.AddChild(bottom);
        var channel=new Control {Position=new Vector2(80,22),Size=new Vector2(640,8),ClipContents=true};
        frame.AddChild(channel);
        var track=new ColorRect {Color=new Color("06151e"),Size=channel.Size,MouseFilter=Control.MouseFilterEnum.Ignore};channel.AddChild(track);
        _bar=new TextureProgressBar { Position=Vector2.Zero, Size=channel.Size,
            TextureProgress=GD.Load<Texture2D>(Art+"slice_24.png"), NinePatchStretch=true, MaxValue=100, MouseFilter=Control.MouseFilterEnum.Ignore };
        channel.AddChild(_bar);
        _spinner=Picture("slice_39",new Vector2(776,827),new Vector2(48,48)); _spinner.PivotOffset=new Vector2(24,24);
        _status=FrontendSkin.Label("Preparing journey…",18);_status.HorizontalAlignment=HorizontalAlignment.Center;
        _status.Position=new Vector2(420,878);_status.Size=new Vector2(760,22);_canvas.AddChild(_status);
    }
    private TextureRect Picture(string name,Vector2 position,Vector2 size)
    {
        // Set IgnoreSize before assigning the texture, so native dimensions never
        // become the minimum size of the control (notably for the logo).
        var image=new TextureRect {ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,
            Texture=GD.Load<Texture2D>(Art+name+".png"),
            StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter=Control.MouseFilterEnum.Ignore,Size=size,Position=position};
        _canvas.AddChild(image);return image;
    }
    public override void _Process(double delta)
    {
        var viewport=GetViewport().GetVisibleRect().Size;float scale=Mathf.Min(viewport.X/1600,viewport.Y/900);
        _canvas.Scale=Vector2.One*scale;_canvas.Position=(viewport-new Vector2(1600,900)*scale)/2;
        if(_failed)return;
        _spinner.Rotation+=(float)delta*1.8f;
        // Give the overlay a rendered frame before starting resource work.
        if(!_requested) { _requested=true;CallDeferred(MethodName.Request);return; }
        if(!_switched)
        {
            var progress=new Godot.Collections.Array();
            var state=ResourceLoader.LoadThreadedGetStatus(Run.WorldScene,progress);
            if(state==ResourceLoader.ThreadLoadStatus.InProgress)
            { _bar.Value=System.Math.Max(_bar.Value,progress.Count>0?progress[0].AsDouble()*85:0);_status.Text="Loading destination…";return; }
            if(state!=ResourceLoader.ThreadLoadStatus.Loaded){Fail("Unable to load destination.");return;}
            var packed=ResourceLoader.LoadThreadedGet(Run.WorldScene) as PackedScene;
            if(packed==null){Fail("Destination scene is unavailable.");return;}
            _bar.Value=90;_status.Text="Preparing world…";_switched=true;
            var error=GetTree().ChangeSceneToPacked(packed);
            if(error!=Error.Ok)Fail("Unable to enter destination: "+error);
            return;
        }
        if(GetTree().CurrentScene is GameWorld { ReadyForPlay:true })
        {
            _bar.Value=100;_status.Text="Ready";
            // Keep coverage through the first completed world draw.
            if(++_readyFrames>=3)QueueFree();
        }
    }
    private void Request()
    {
        var error=ResourceLoader.LoadThreadedRequest(Run.WorldScene,"PackedScene",true);
        if(error!=Error.Ok)Fail("Unable to load destination: "+error);
    }
    private void Fail(string message)
    {
        _failed=true;_status.Text=message;
        var back=new Button { Text="Return to selection",Position=new Vector2(650,815),Size=new Vector2(300,40) };
        _spinner.Hide();_canvas.AddChild(back);
        back.Pressed+=()=> {GameNavigation.CancelPendingRun();GetTree().ChangeSceneToFile(GameNavigation.MenuScene);QueueFree();};
        back.GrabFocus();
    }
}
