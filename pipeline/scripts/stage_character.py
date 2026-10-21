"""Stage generated asset and compact validation samples into Godot."""
import json, shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
recipe=json.loads((ROOT/'character-recipe.json').read_text())
report=json.loads((ROOT/'manifests/character-conversion.json').read_text())
for clip,animation in zip(recipe['animations'],report['animations']):clip['duration']=animation['sourceDuration']
target=ROOT/'godot/Assets'
(target/'character-recipe.json').write_text(json.dumps(recipe,indent=2))
out={'boneCount':len(report['skeleton']['bones'])+len(report['clothBones']),
     'bones':[{'name':b['name'],'parent':b['parent']} for b in report['skeleton']['bones']+report['clothBones']], 'samples':{}}
for name,samples in report['poseSamples'].items():
    duration=next(c['duration'] for c in recipe['animations'] if c['name']==name)
    out['samples'][name]=[]
    for idx,s in enumerate(samples):
        out['samples'][name].append({'time':duration*idx/4,'bones':[{
            'name':bone,'position':[row[3] for row in m[:3]],
            'x':[m[i][0] for i in range(3)],'y':[m[i][1] for i in range(3)],'z':[m[i][2] for i in range(3)]}
            for bone,m in s['boneMatrices'].items()]})
(target/'character-validation-input.json').write_text(json.dumps(out))
shutil.copyfile(ROOT/'generated/sorcerer.glb',target/'sorcerer.glb')
print('Staged original rig, seven clips, and Blender pose comparisons')
