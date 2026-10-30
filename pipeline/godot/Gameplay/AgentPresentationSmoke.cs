using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class AgentPresentationSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();var rates=new List<object>();string? failure=null;
        void Check(string name,bool ok){if(!ok)throw new Exception(name);checks.Add(name);}
        bool DifferentMotion(Animation a,Animation b){
            for(int t=0;t<a.GetTrackCount();t++)if(a.TrackGetType(t)==Animation.TrackType.Rotation3D){
                int other=b.FindTrack(a.TrackGetPath(t),Animation.TrackType.Rotation3D);if(other<0)continue;
                foreach(double fraction in new[]{.2,.5,.8})if(Math.Abs(a.RotationTrackInterpolate(t,a.Length*fraction).Dot(b.RotationTrackInterpolate(other,b.Length*fraction)))<.999)return true;
            }
            return false;
        }
        try{
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            var actor=w.Player.Presentation;actor.ResetPresentation();
            Check("directional rifle locomotion enabled",actor.DirectionalLocomotionEnabled);
            if(w.RunConfiguration.AdvancedClassId=="operative")Check("Laceration follow-up contains different recovered poses",DifferentMotion(actor.Animator.GetAnimation("Agent/Laceration"),actor.Animator.GetAnimation("Agent/LacerationFollowup")));
            foreach(var direction in new[]{Vector3.Forward,Vector3.Back,Vector3.Left,Vector3.Right}){
                for(int i=0;i<30;i++){actor.Present(direction*w.Player.MoveSpeed,1.0/60,0);actor._Process(1.0/60);}
                GD.Print("RIFLE STRIDE "+string.Join(",",actor.NativeRunSpeeds)+" rate "+actor.LocomotionPlaybackRate);
                Check("stride rate finite "+direction,float.IsFinite(actor.LocomotionPlaybackRate) && actor.LocomotionPlaybackRate>.15f && actor.LocomotionPlaybackRate<6);
                rates.Add(new{direction=direction.ToString(),rate=actor.LocomotionPlaybackRate,blend=actor.LocomotionDirection.ToString()});
            }
            var target=new Combatant {Name="PresentationTarget",Team=CombatTeam.Hostile};target.AddChild(new Health{Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.15f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var a in w.Abilities.Abilities){
                actor.ResetPresentation();var visual=(D3AbilityPresentation)a.Presentation;visual.Release();
                Check(a.Definition.Id+" clip and presentation resolve",visual.Begin(target,target.GlobalPosition,a.Radius));
                if(a.Definition.Id=="explosiveprobe")Check("Explosive Probe deploys original model before impact",visual.ExplosiveProbeVisible && visual.ExplosiveProbeDeployments>0);
                visual._Process(.05);visual.Impact(target.AimPosition);visual._Process(.05);
                if(a.Definition.Id=="explosiveprobe"){
                    Check("Explosive Probe detonates once with layered source FX",visual.ExplosiveProbeDetonations>0 && !visual.ExplosiveProbeVisible && visual.EffectCount>=38);
                    Check("Explosive Probe plays recovered explosion event",visual.LastPlayedAudioPath.EndsWith("explosiveprobe_explode_layered.wav") && visual.PlayingAudioVoices>0);
                    int bursts=visual.ExplosiveProbeDetonations;visual.Impact(target.AimPosition);Check("area victims do not duplicate probe detonation",visual.ExplosiveProbeDetonations==bursts);
                }
                if(a.Definition.Id is "shiv" or "backstab" or "hiddenstrike" or "laceration" or "severtendon"){
                    Check(a.Definition.Id+" creates one fitted knife per activation",visual.AgentKnivesPresented>0);
                    var knife=visual.ActiveKnife!;knife._Process(.1);
                    string socket=a.Definition.Id=="laceration"?"LeftWeapon":"RightWeapon";
                    Check(a.Definition.Id+" uses source weapon socket and hides rifle",knife.SocketName==socket && knife.HiddenRifleCount>0 && knife.RifleHidden);
                    var expected=actor.Skeleton.GlobalTransform*actor.Skeleton.GetBoneGlobalPose(actor.Skeleton.FindBone(socket));
                    Check(a.Definition.Id+" follows animated hand",knife.GlobalPosition.DistanceTo(expected.Origin)<.0001f);
                    visual._Process(.25);Check(a.Definition.Id+" plays recovered knife audio",(a.Definition.WeaponAudio.Contains("Agent/Audio/") || a.Definition.ImpactAudio.Contains("Agent/Audio/")) && visual.PlayingAudioVoices>0);
                    if(a.Definition.Id=="laceration")Check("Laceration plays complete layered event at calibrated knife gain",visual.LastPlayedAudioPath.EndsWith("laceration_impact_layered.wav") && visual.LastPlayedAudioGainDb==-4);
                    int impacts=visual.AgentKnifeImpactsPresented;visual.Impact(target.AimPosition);visual.Impact(target.AimPosition);
                    Check(a.Definition.Id+" cleave does not restart impact audio",visual.AgentKnifeImpactsPresented==impacts);
                    if(a.Definition.Id=="laceration"){
                        knife._Process(.6);Check("follow-up restores the rifle for its original strike motion",!knife.RifleHidden);
                        visual.PrepareAgentKnifeStrike(2);visual.Impact(target.AimPosition);
                        Check("follow-up plays a distinct original-media cue",visual.LastPlayedAudioPath==a.Definition.FollowupImpactAudio && a.Definition.FollowupImpactAudio!=a.Definition.ImpactAudio);
                    }
                }
                visual.Release();Check(a.Definition.Id+" reset clears transient effects",visual.EffectCount==0);
            }
            if(OS.GetCmdlineUserArgs().Contains("--agent-audio-capture")){
                var measurements=new List<object>();
                int bus=AudioServer.GetBusIndex("B7Combat");var capture=new AudioEffectCapture {BufferLength=4};
                int effect=AudioServer.GetBusEffectCount(bus);AudioServer.AddBusEffect(bus,capture);
                try{
                    foreach(string id in new[]{"laceration","explosiveprobe"}){
                        if(w.Abilities.Find(id) is not {} a)continue;
                        foreach(var ability in w.Abilities.Abilities)ability.Presentation.Release();
                        actor.ResetPresentation();a.SetUnlocked(true);a.ResetCombat();w.Targeting.Update(0,true);
                        for(int i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
                        var abilityVisual=(D3AbilityPresentation)a.Presentation;int knifeContacts=abilityVisual.AgentKnifeImpactsPresented;
                        capture.ClearBuffer();Check(id+" real cast starts for output capture",a.TryBegin());
                        for(int i=0;i<110;i++){a.Advance(1.0/60);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
                        var frames=capture.GetBuffer(capture.GetFramesAvailable());double energy=0;float peak=0;var bytes=new byte[frames.Length*4];
                        for(int i=0;i<frames.Length;i++)for(int c=0;c<2;c++){
                            float value=c==0?frames[i].X:frames[i].Y;energy+=value*value;peak=Math.Max(peak,Math.Abs(value));
                            short sample=(short)Math.Round(Math.Clamp(value,-1,1)*32767);int offset=i*4+c*2;bytes[offset]=(byte)(sample&255);bytes[offset+1]=(byte)((sample>>8)&255);
                        }
                        double rms=Math.Sqrt(energy/Math.Max(1,frames.Length*2));
                        if(id=="laceration")Check("real Laceration cast plays two contact sounds",abilityVisual.AgentKnifeImpactsPresented==knifeContacts+2);
                        Check(id+" real combat bus produces audible non-clipping PCM",frames.Length>1000 && rms>.001 && peak<1);
                        var wav=new AudioStreamWav {Format=AudioStreamWav.FormatEnum.Format16Bits,Stereo=true,MixRate=(int)AudioServer.GetMixRate(),Data=bytes};
                        wav.SaveToWav(ProjectSettings.GlobalizePath("res://../manifests/agent-"+id+"-combat-output.wav"));
                        measurements.Add(new{id,frames=frames.Length,rms,peak});a.ResetCombat();
                    }
                }finally{AudioServer.RemoveBusEffect(bus,effect);}
                System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/agent-"+w.RunConfiguration.AdvancedClassId+"-audio-output.json"),JsonSerializer.Serialize(measurements,new JsonSerializerOptions{WriteIndented=true}));
            }
            foreach(string id in new[]{"dart","corrosivegrenade","interrogationprobe"}){
                if(w.Abilities.Find(id) is not {} source)continue;
                w.Periodic.Apply(source,target);w.Periodic.Apply(source,target);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check(id+" refresh keeps one attached visual",target.GetChildren().OfType<AgentStatusVisual>().Count()==1);
                if(id=="interrogationprobe"){
                    var status=target.GetChildren().OfType<AgentStatusVisual>().Single();status._Process(.1);
                    Check("Interrogation Probe displays recovered drone and three electric links",status.InterrogationProbeVisible && status.InterrogationZapCount==3);
                }
                w.Periodic.RemoveSource(source);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check(id+" removal clears attached visual",!target.GetChildren().OfType<AgentStatusVisual>().Any());
            }
            if(OS.GetCmdlineUserArgs().Contains("--agent-capture")){
                foreach(var layer in GetTree().Root.FindChildren("*","CanvasLayer",true,false).OfType<CanvasLayer>())layer.Visible=false;
                var camera=new Camera3D();w.AddChild(camera);camera.GlobalPosition=actor.GlobalPosition+new Vector3(.24f,.18f,.28f);camera.LookAt(actor.GlobalPosition+Vector3.Up*.10f);camera.MakeCurrent();
                foreach(string pose in new[]{"running","shiv","backstab","acidblade","hiddenstrike","laceration","laceration_followup","orbitalstrike","koltoprobe","suppressivefire","plasmaprobe","carbineburst","debilitate","explosiveprobe","explosiveprobe_deploy","interrogationprobe"}){
                    foreach(var ability in w.Abilities.Abilities)ability.Presentation.Release();
                    foreach(var ability in w.Abilities.Abilities)w.Periodic.RemoveSource(ability);
                    actor.ResetPresentation();
                    var a=w.Abilities.Find(pose=="explosiveprobe_deploy"?"explosiveprobe":pose=="laceration_followup"?"laceration":pose=="acidblade"?"backstab":pose);if(pose!="running" && a==null)continue;
                    if(a!=null){actor.TryBeginGesture(a,a.Definition.StartClip,0,false);a.Presentation.Begin(target,target.GlobalPosition,a.Radius);}
                    if(pose=="interrogationprobe")w.Periodic.Apply(a!,w.PlayerActor);
                    int frames=pose=="laceration_followup"?54:24;
                    for(int i=0;i<frames;i++){if(i==frames-4 && a!=null && pose!="explosiveprobe_deploy"){if(pose=="laceration_followup")((D3AbilityPresentation)a.Presentation).PrepareAgentKnifeStrike(2);a.Presentation.Impact(target.AimPosition);if(pose=="acidblade")((D3AbilityPresentation)a.Presentation).PresentAcidBladeDetonation(target);}actor.Present(pose=="running"?Vector3.Right*w.Player.MoveSpeed:Vector3.Zero,1.0/60,0);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);}
                    await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/agent-"+w.RunConfiguration.AdvancedClassId+"-"+pose+".png"));
                }
            }
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/agent-"+w.RunConfiguration.AdvancedClassId+"-presentation.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks,rates},new JsonSerializerOptions{WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
