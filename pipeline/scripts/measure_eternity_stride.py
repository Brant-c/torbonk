"""Estimate in-place run stride speed from low-contact ankle/paw travel in exported clips."""
import bpy,json,statistics,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
result={}
for name,bones in [('icecat',['LeftAnkle','RightAnkle','LeftWrist','RightWrist']),('guard',['LeftAnkle','RightAnkle']),('xrr3',['LeftAnkle','RightAnkle','CenterAnkle'])]:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath=str(ROOT/'godot/Assets/Maps/eternity_vault/Enemies'/(name+'.glb')))
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 rig.animation_data.action=None
 for track in rig.animation_data.nla_tracks:track.mute=track.name!='Walk'
 track=next(t for t in rig.animation_data.nla_tracks if t.name=='Walk');strip=track.strips[0];end=float(strip.frame_end);start=float(strip.frame_start)
 fps=bpy.context.scene.render.fps/bpy.context.scene.render.fps_base;duration=(end-start)/fps;samples={bone:[] for bone in bones if bone in rig.pose.bones}
 steps=128
 for i in range(steps+1):
  f=start+(end-start)*i/steps;bpy.context.scene.frame_set(math.floor(f),subframe=f%1);bpy.context.view_layer.update()
  for bone in samples:samples[bone].append(list((rig.matrix_world@rig.pose.bones[bone].matrix).translation))
 estimates=[];evidence={}
 for bone,points in samples.items():
  zs=sorted(p[2] for p in points);cutoff=zs[int(len(zs)*.35)];dt=duration/steps;velocities=[]
  for i in range(1,len(points)):
   if max(points[i][2],points[i-1][2])<=cutoff:
    v=math.hypot(points[i][0]-points[i-1][0],points[i][1]-points[i-1][1])/dt
    if abs(v)>.005:velocities.append(abs(v))
  if velocities:
   speed=statistics.median(velocities);estimates.append(speed);evidence[bone]=dict(contactSpeed=speed,contactSamples=len(velocities))
 result[name]=dict(clip='Walk',unitScaleStrideSpeed=(statistics.median(estimates) if estimates else None),duration=duration,feet=evidence,method='Median horizontal paw/ankle speed during lowest 35% of foot height; approximate contact fit, not native movement metadata.')
(ROOT/'manifests/eternity-stride-calibration.json').write_text(json.dumps(result,indent=2))
print('STRIDE_FIT',json.dumps(result))
