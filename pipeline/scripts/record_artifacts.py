import json
from source_pipeline import ROOT,sha,save
files=[]
for folder in ['generated','work']:
    for p in sorted((ROOT/folder).rglob('*')):
        if p.is_file() and (p.suffix in ['.png','.glb','.blend']):
            files.append({'path':str(p.relative_to(ROOT)),'bytes':p.stat().st_size,'sha256':sha(p)})
pipeline_files=[]
for pattern in ['*.ps1','recipe.json','scripts/*.py','godot/*.cs','godot/*.csproj','godot/*.tscn','godot/project.godot','godot/Assets/*.import','godot/Assets/recipe.json']:
    for p in sorted(ROOT.glob(pattern)):
        pipeline_files.append({'path':str(p.relative_to(ROOT)),'sha256':sha(p)})
save('artifacts.json',{'files':files,'pipelineFiles':pipeline_files,'totalArtifactBytes':sum(x['bytes'] for x in files),
                     'pipelineBytes':sum(p.stat().st_size for p in ROOT.rglob('*') if p.is_file())})
print('Artifact hashes recorded')
