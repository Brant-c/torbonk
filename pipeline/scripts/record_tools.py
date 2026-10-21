"""Freeze local packages/executables and source code provenance."""
import json,sys
from pathlib import Path
from source_pipeline import ROOT,sha,save
release=json.loads((ROOT/'manifests/zg-release.json').read_text(encoding='utf-8-sig'))
packages=[]
for asset in release['assets']:
    if asset['name'].endswith('.zip'):
        p=ROOT/'tools/packages'/asset['name']; digest=sha(p)
        if asset.get('digest'): assert asset['digest']=='sha256:'+digest
        packages.append({'name':p.name,'url':asset['browser_download_url'],'bytes':p.stat().st_size,'sha256':digest})
executables=[]
for p,version in [
 (Path('C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'),'5.2.1 LTS 9e2066aef7ef'),
 (ROOT/'tools/godot/Godot.exe','4.7.2.stable.mono.official.ed1daf0bf'),
 (ROOT/'tools/godot/Godot_console.exe','4.7.2 console launcher'),
 (Path(sys.executable),sys.version),
 (Path('C:/Program Files/dotnet/dotnet.exe'),'SDK 10.0.401; engine loads runtime 10.0.12')]:
    executables.append({'path':str(p),'version':version,'bytes':p.stat().st_size,'sha256':sha(p)})
tree=[]
for p in sorted((ROOT/'tools/addons').rglob('*')):
    if p.is_file() and '__pycache__' not in str(p):
        tree.append({'path':str(p.relative_to(ROOT)),'bytes':p.stat().st_size,'sha256':sha(p)})
for p in sorted((ROOT/'tools/godot/GodotSharp').rglob('*')):
    if p.is_file(): tree.append({'path':str(p.relative_to(ROOT)),'bytes':p.stat().st_size,'sha256':sha(p)})
save('tooling.lock.json',{'releaseUrl':release['html_url'],'releaseTag':release['tag_name'],
 'releasePublished':release['published_at'],'packages':packages,'executables':executables,
 'toolFiles':tree, 'addons':{'io_scene_gr2':'4.2.1','zg_swtor_tools':'2.0.14'},
 'gltfExporter':'5.2.40 bundled with Blender',
 'readerReference':[{'path':str(p),'sha256':sha(p)} for p in [
 Path('D:/SWTORClassic/extracTOR/extracTOR/TorArchive/Archive.cs'),
 Path('D:/SWTORClassic/extracTOR/extracTOR/Helper/HashLittle2.cs')]],
 'scope':'Packages unmodified; context and temp workarounds live only in pipeline scripts.'})
print('Tool lock recorded')
