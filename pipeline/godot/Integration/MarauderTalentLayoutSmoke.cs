using Godot;
using System;
using System.Linq;
using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Integration;
public partial class MarauderTalentLayoutSmoke : Control
{
 public override async void _Ready() {
  try {
   var background=new ColorRect {Color=new Color("02080c")};AddChild(background);background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
   var canvas=new Control {Size=new Vector2(1600,900)};AddChild(canvas);
   void Fit(){float scale=Mathf.Min(Size.X/1600,Size.Y/900);canvas.Scale=Vector2.One*scale;canvas.Position=(Size-new Vector2(1600,900)*scale)*.5f;}
   Resized+=Fit;Fit();
   string cls=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--talent-class="))?.Split('=')[1] ?? (OS.GetCmdlineUserArgs().Contains("--juggernaut-layout")?"juggernaut":"marauder");
   var screen=new TalentsScreen {ClassId=cls,Size=new Vector2(1448,700),Position=new Vector2(76,150)};canvas.AddChild(screen);
   for(int i=0;i<5;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
   var buttons=screen.FindChildren("*","",true,false).OfType<TalentIconButton>().ToArray();
   if(buttons.Length!=27 || buttons.Any(b=>b.Position.X<0 || b.Position.X+b.Size.X>410 || b.Position.Y+b.Size.Y+50>440))throw new Exception("Talent outside frame");
   await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
   var result=GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/"+cls+"-nine-talents.png"));
   if(result!=Error.Ok)throw new Exception("Screenshot failed: "+result);
   GD.Print("Marauder talent layout: 27 talents fit; screenshot saved.");GetTree().Quit();
  }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
 }
}

