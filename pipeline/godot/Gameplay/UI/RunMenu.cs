using Godot;
using System.Linq;
using SwtorSurvivors.Equipment;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.UI;

/// <summary>Compact recovered-art toolbar and a pause-owned in-run wardrobe.</summary>
public partial class RunMenu : CanvasLayer
{
    public GameWorld World {get;set;}=null!;
    public bool IsOpen => _overlay!=null;
    private Control _root=null!,_design=null!;
    private Control? _overlay;
    private bool _wasPaused;
    private ProcessModeEnum _progressionMode;
    public override void _Ready()
    {
        Layer=20;ProcessMode=ProcessModeEnum.Always;
        _root=new Control {Theme=SwtorSkin.MakeTheme(),MouseFilter=Control.MouseFilterEnum.Ignore};
        AddChild(_root);_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var bar=new PanelContainer {Name="RunToolbar",MouseFilter=Control.MouseFilterEnum.Stop};_root.AddChild(bar);
        bar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        bar.OffsetLeft=-108;bar.OffsetRight=108;bar.OffsetTop=0;bar.OffsetBottom=42;
        bar.AddThemeStyleboxOverride("panel",SwtorSkin.ButtonArtwork());
        var row=new HBoxContainer {Alignment=BoxContainer.AlignmentMode.Center};row.AddThemeConstantOverride("separation",16);bar.AddChild(row);
        void Icon(string name,string art,string hint,System.Action action)
        {
            var b=new SwtorButton {Name=name,Icon=SwtorSkin.Slice(art),TooltipText=hint,ExpandIcon=true,CustomMinimumSize=new Vector2(38,30)};
            b.AddThemeConstantOverride("icon_max_width",24);
            b.AddThemeColorOverride("icon_hover_color",SwtorSkin.Blue);
            b.AddThemeColorOverride("icon_focus_color",SwtorSkin.Blue);
            b.AddThemeStyleboxOverride("normal",new StyleBoxEmpty());b.AddThemeStyleboxOverride("hover",new StyleBoxEmpty());
            b.Pressed+=action;row.AddChild(b);
        }
        Icon("RunReturn","bwaui_buttons_66","Return to Main Menu",()=>World.ReturnToMainMenu());
        Icon("RunRestart","bwaui_buttons_69","Restart Run [R]",Restart);
        Icon("RunCollections","bwaui_buttons_242","Collections",OpenCollections);
        _root.Resized+=Layout;
    }
    private void BeginOverlay()
    {
        _wasPaused=GetTree().Paused;_progressionMode=World.Progression!.ProcessMode;
        World.Progression.ProcessMode=ProcessModeEnum.Disabled;
        GetTree().Paused=true;World.Rig.SetControlsEnabled(false);
        Integration.SwtorCursor.Instance.RestoreVisible();
        _overlay=new Control {MouseFilter=Control.MouseFilterEnum.Stop};_root.AddChild(_overlay);
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade=new ColorRect {Color=new Color(0.005f,0.015f,0.025f,.94f)};_overlay.AddChild(shade);shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _design=new Control {Size=new Vector2(1600,900),MouseFilter=Control.MouseFilterEnum.Ignore};_overlay.AddChild(_design);Layout();
    }
    private void Layout()
    {
        if(!IsOpen)return;
        float scale=Mathf.Min(_root.Size.X/1600,_root.Size.Y/900);
        _design.Scale=Vector2.One*scale;_design.Position=(_root.Size-new Vector2(1600,900)*scale)/2;
    }
    public void OpenCollections()
    {
        if(IsOpen)Close();BeginOverlay();
        var screen=new CollectionsScreen {Name="RunWardrobe",Position=new Vector2(76,150),Size=new Vector2(1448,650),CloseLabel="Resume Run",Closed=Close};
        screen.SelectClass(World.RunConfiguration.BaseClassId,World.RunConfiguration.AdvancedClassId);
        _design.AddChild(screen);
    }
    public void OpenPause()
    {
        if(IsOpen){Close();return;}BeginOverlay();
        var col=new VBoxContainer();col.AddThemeConstantOverride("separation",6);var frame=new SwtorPanel {Position=new Vector2(580,270),Size=new Vector2(440,300)};_design.AddChild(frame);frame.AddChild(col);
        col.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);col.OffsetLeft=24;col.OffsetTop=20;col.OffsetRight=-24;col.OffsetBottom=-20;
        col.AddChild(SwtorSkin.Text("RUN PAUSED",24,true));
        void Button(string title,System.Action action) {var b=new SwtorButton {Text=title,CustomMinimumSize=new Vector2(0,50)};b.Pressed+=action;col.AddChild(b);}
        Button("Resume Run",Close);Button("Collections",OpenCollections);Button("Restart Run",Restart);Button("Main Menu",()=>World.ReturnToMainMenu());
        col.GetChild<Control>(1).GrabFocus();
    }
    public void Close()
    {
        if(!IsOpen)return;
        _root.RemoveChild(_overlay!);_overlay!.QueueFree();_overlay=null;
        var collection=new CosmeticCollection();var next=World.Equipment.Loadout.Current.Copy();
        next.Cosmetics.Clear();foreach(var entry in collection.Profile(World.RunConfiguration.BaseClassId,World.RunConfiguration.AdvancedClassId).Equipped)next.Cosmetics[entry.Key]=entry.Value;
        if(!World.Equipment.Loadout.TryApply(next,out string error))GD.PushWarning(error);
        World.Progression!.ProcessMode=_progressionMode;GetTree().Paused=_wasPaused;
        World.Rig.SetControlsEnabled(!_wasPaused && World.PlayerActor.Alive);
    }
    public void Restart() {Close();World.ResetCombatWorld();}
    public override void _Input(InputEvent e)
    {
        if(e is not InputEventKey {Pressed:true,Echo:false} k)return;
        if(k.PhysicalKeycode==Key.Escape) {OpenPause();GetViewport().SetInputAsHandled();}
        else if(IsOpen && k.PhysicalKeycode==Key.R) {Restart();GetViewport().SetInputAsHandled();}
    }
}
