import bpy,sys,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools/addons'))
import addon_utils
addon_utils.enable('io_scene_gr2',default_set=False,persistent=False)
from io_scene_gr2.ops.import_jba import read
class Reporter:
    def report(self,*args):pass
raw=read(Reporter(),str(ROOT/'source-assets/april2012/resources/anim/humanoid/bmnnew/cb_bounty_flame_sweep.jba'))
records=[]
for b in raw.bones:
    if b.name in ['Bip01','Pelvis','RootSpine','LowerBack','Chest','Chest1','RightUpperArm','LeftUpperArm']:
        records.append(dict(bone=b.name,angles=[list(b.rotations[round((len(b.rotations)-1)*t)].to_euler()) for t in [0,.25,.5,.75,1]]))
(ROOT/'manifests/d3-spin-investigation.json').write_text(json.dumps(records,indent=2));print(json.dumps(records))
