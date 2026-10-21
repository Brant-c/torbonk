import bpy,sys,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/addons'))
import addon_utils
addon_utils.enable('io_scene_gr2',default_set=False,persistent=False)
from io_scene_gr2.ops.import_jba import read
class Reporter:
    def report(self,*args):pass
out=[]
for stem in ['cb_bounty_flame_sweep','cb_saber_ground_smash_01','cb_saber_sp2_attack_right_1','cb_rifle_sniper_fire_big_1','cb_rifle_bracer_fire_1']:
    path=ROOT/f'source-assets/april2012/resources/anim/humanoid/bmnnew/{stem}.jba';raw=read(Reporter(),str(path))
    bone=next(b for b in raw.bones if b.name=='Bip01')
    out.append(dict(clip=stem,seconds=raw.length,rootRotations=[list(bone.rotations[round((len(bone.rotations)-1)*t)]) for t in [0,.25,.5,.75,1]],worldRotations=[list(raw.world_space.rotations[round((len(raw.world_space.rotations)-1)*t)]) for t in [0,.25,.5,.75,1]]))
(ROOT/'manifests/d3-pose-investigation.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out))
