"""Apply the reviewed C1 presentation to the existing C2 controller without replacing its launch logic.
Run once; original integrated files and a focused diff remain in C1 work/integration-review.
"""
from pathlib import Path
import re,json,hashlib,difflib
ROOT=Path(__file__).resolve().parents[1]
TARGET=ROOT.parent/'pipeline/godot/Frontend'
SOURCE=ROOT/'Frontend'
WORK=ROOT/'work/integration-review'
WORK.mkdir(parents=True,exist_ok=True)
def sha(b):return hashlib.sha256(b).hexdigest()
def method_span(text,name):
    pattern=r'^    (?:public|private|protected) (?:override )?[\w?<>]+ '+re.escape(name)+r'\('
    m=re.search(pattern,text,re.M);assert m,name
    following=re.search(r'^    (?:public|private|protected) (?:override )?[\w?<>]+ \w+\(',text[m.end():],re.M)
    return m.start(),m.end()+following.start() if following else text.rfind('\n}')
def method(text,name):
    a,b=method_span(text,name);return text[a:b]
def replace_method(text,name,new):
    a,b=method_span(text,name);return text[:a]+new+text[b:]
flowpath=TARGET/'Scripts/FrontendFlow.cs'
old=flowpath.read_text(encoding='utf-8-sig');flow=old;review=(SOURCE/'Scripts/FrontendFlow.cs').read_text(encoding='utf-8-sig')
assert 'StyleBoxFlat' in (TARGET/'Scripts/FrontendSkin.cs').read_text(encoding='utf-8-sig'),'Already updated or unexpected baseline; review before reapplying.'
for name in ['_Ready','Factions','Classes','Advanced','Appearance','Galaxy','MainMenu']:
    replacement=method(review,name)
    if name=='Advanced':replacement=replacement.replace('SORCERER COMBAT IMPLEMENTED\\nFrontend integration pending','SORCERER COMBAT AVAILABLE\\nSelect Korriban to play')
    if name=='Appearance':
        replacement=replacement.replace('Unavailable in C1: requires compatible source variations and conversion / material support.','This appearance option is not available for the current preset.')
        replacement=replacement.replace('Appearance editing is unavailable in C1. No unsupported visual combinations are applied.','Appearance editing is unavailable. The original Sorcerer preset is used.')
    if name=='Galaxy':replacement=replacement.replace('Requires Sith Inquisitor → Sorcerer. Gameplay launching is pending integration.','Requires Sith Empire → Sith Inquisitor → Sorcerer and the original appearance.')
    if name=='MainMenu':
        replacement=replacement.replace('C1 FRONTEND PREVIEW','SWTOR SURVIVORS').replace('Gameplay integration pending. Your selections remain here while browsing.','Sorcerer / Korriban survival available. Your selections remain here while browsing.')
        replacement=replacement.rstrip()[:-1]+'    menu.GetChild<Control>(0).GrabFocus();\n    }\n'
    flow=replace_method(flow,name,replacement)
flow=replace_method(flow,'Planet','')
oldcrumb='var crumb = FrontendSkin.Label(string.Join("   ›   ",_steps.Skip(1).Select((s,i)=>i+1==_step ? "[ "+s.ToUpper()+" ]" : s.ToUpper())),15);'
assert oldcrumb in flow
flow=flow.replace(oldcrumb,'var crumb = FrontendSkin.Label(string.Join("   ›   ",_steps.Skip(1).Select(s=>s.ToUpper())),15);')
flow=flow.replace('crumb.Position = new Vector2(630,55); crumb.Modulate=FrontendSkin.Blue;','crumb.Position = new Vector2(630,55);')
# Ready keeps its current availability/launch messaging; only remove the old bold flag.
ready=method(flow,'ReadyScreen')
ready=re.sub(r',(\d+),(true|false)(?:,(true|false))?\)',lambda m:','+m[1]+(','+m[3] if m[3] else '')+')',ready)
flow=replace_method(flow,'ReadyScreen',ready)
# These integration behaviors must survive byte-for-byte.
for name in ['StartRun','RestoreConfiguration','ShowSettings','_UnhandledKeyInput']:
    assert method(old,name)==method(flow,name),name
for token in ['public Func<SelectionConfiguration, string?>? ValidateForPlay','public Func<SelectionConfiguration, string?>? PlayRequested','if (_step == 6) { StartRun(); return; }','next.Disabled = _step==6 && UnavailableReason != null;','string? unavailable = UnavailableReason;','EmitSignal(SignalName.ConfigurationReady,c.ToJson());']:
    assert token in flow,token
changes={'Scripts/FrontendFlow.cs':flow.encode()}
for name in ['FrontendSkin.cs','GalaxyChart.cs']:
    s=(SOURCE/'Scripts'/name).read_text(encoding='utf-8-sig')
    imports='using System;\nusing System.Collections.Generic;\nusing System.Linq;\n'
    changes['Scripts/'+name]=(imports+s).encode()
# Only layout coordinates are synchronized: preserve integrated descriptions/content flags.
catalog=json.loads((TARGET/'Data/catalog.json').read_text(encoding='utf-8-sig'))
reviewcatalog=json.loads((SOURCE/'Data/catalog.json').read_text(encoding='utf-8-sig'))
byid={m['Id']:m for m in reviewcatalog['Maps']}
for m in catalog['Maps']:m['X'],m['Y']=byid[m['Id']]['X'],byid[m['Id']]['Y']
changes['Data/catalog.json']=json.dumps(catalog,indent=2).encode()
assert (TARGET/'Assets/Slices/bwaui_character_create_panels_9.png').read_bytes()==(SOURCE/'Assets/Slices/bwaui_character_create_panels_9.png').read_bytes()
protected=[ROOT.parent/'pipeline/godot/project.godot',ROOT.parent/'pipeline/Open-Survivors.ps1',TARGET/'Scenes/MainMenu.tscn',TARGET/'Scripts/SelectionConfiguration.cs']
protected+=list((ROOT.parent/'pipeline/godot/Integration').glob('*.cs'))
baseline={str(p):sha(p.read_bytes()) for p in protected}
records=[]
for rel,data in changes.items():
    p=TARGET/rel;before=p.read_bytes();backup=WORK/'before'/rel;backup.parent.mkdir(parents=True,exist_ok=True)
    assert not backup.exists(),'Backup already exists; review before reapplying.'
    backup.write_bytes(before)
    if p.suffix=='.cs':
        diff=''.join(difflib.unified_diff(before.decode().splitlines(True),data.decode().splitlines(True),fromfile='before/'+rel,tofile='after/'+rel))
        (WORK/(p.stem+'.diff')).write_text(diff,encoding='utf-8')
    p.write_bytes(data)
    records.append({'path':str(p),'beforeSha256':sha(before),'afterSha256':sha(data),'backup':str(backup)})
assert all(sha(Path(p).read_bytes())==digest for p,digest in baseline.items())
(ROOT/'manifests/integrated-visual-sync.json').write_text(json.dumps({'updated':records,'protectedIntegrationFiles':baseline,'protectedFilesUnchanged':True,'runtimeLaunched':False},indent=2),encoding='utf-8')
print('Updated 3 frontend scripts and destination coordinates; integration hooks, adapter, main scene, launcher and gameplay configuration unchanged.')
