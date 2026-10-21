"""Recover only event-linked frontend sounds from the already identified original GUI bank.
Freeze bank and parsed XML inside C1 first; no shared tool invocation or writes.
"""
from pathlib import Path
import hashlib,json,shutil,struct,zlib,re,subprocess,wave,xml.etree.ElementTree as ET
from ui_formats import tags
R=Path(__file__).resolve().parents[1];P=R.parent/'pipeline'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def copy(src,dest):dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dest)
bank=R/'source-assets/gui.bnk';xml=R/'work/gui.xml'
copy(P/'source-assets/april2012/audio-by-hash/b7-gui-d634b5f5260713cd.bnk',bank)
copy(P/'work/b7/gui.xml',xml)
copy(P/'manifests/b7-gui-bank.json',R/'manifests/gui-bank-prior.json')
decoder=R/'tools/vgmstream'
for p in (P/'tools/vgmstream').glob('*'):
    if p.is_file():copy(p,decoder/p.name)
root=ET.parse(xml).getroot();objects={}
for obj in root.iter('object'):
    if obj.get('name','').startswith('CAk'):
        idfield=next((x for x in obj.findall('field') if x.get('name')=='ulID'),None)
        if idfield is not None:objects[int(idfield.get('value'))]=obj
def fnv(s):
    h=2166136261
    for b in s.lower().encode():h=((h*16777619)^b)&0xffffffff
    return h
blob=(R/'source-assets/resources/gfx/gfx_production/bwaui_character_create.gfx').read_bytes()
if blob[:3]==b'CFX':blob=blob[:8]+zlib.decompress(blob[8:])
names=sorted(set(x.decode() for x in re.findall(rb'[Pp]lay_gui_[A-Za-z0-9_]+',blob)))
names=[n for n in names if re.search('char|galaxy|button|select|back',n,re.I)]
bankdata=bank.read_bytes();chunks={};pos=0
while pos+8<=len(bankdata):
    t,n=struct.unpack_from('<4sI',bankdata,pos);chunks[t]=(pos+8,n);pos+=8+n
start,n=chunks[b'DIDX'];media={}
for pos in range(start,start+n,12):
    mid,off,size=struct.unpack_from('<III',bankdata,pos);media[mid]=(chunks[b'DATA'][0]+off,size)
def walk(oid,seen=None):
    seen=set() if seen is None else seen
    if oid in seen or oid not in objects:return set(),[]
    seen.add(oid);obj=objects[oid];found=set();edges=[]
    for f in obj.iter('field'):
        name=f.get('name','');value=f.get('value','')
        if name in ('sourceID','ulSourceID','uFileID') and value.isdigit() and int(value) in media:found.add(int(value))
        if name in ('ulActionID','ulTargetID','ulChildID','ulID') and value.isdigit() and int(value)!=oid:
            child=int(value)
            if child in objects:
                a,b=walk(child,seen);found|=a;edges.append({'from':str(oid),'field':name,'to':str(child)});edges+=b
    return found,edges
events=[]
for name in names:
    eid=fnv(name)
    if eid in objects and objects[eid].get('name')=='CAkEvent':
        mids,edges=walk(eid);events.append({'name':name,'id':str(eid),'media':sorted(mids),'edges':edges})
print('linked events',[(e['name'],e['media']) for e in events])
(R/'manifests/frontend-audio-discovery.json').write_text(json.dumps({'bankSha256':sha(bank),'xmlSha256':sha(xml),'events':events,'candidateNames':names},indent=2))
selected={}
for role,needle in [('map','galaxymap_mapitem_activate'),('select','interface_buttonclick'),('back','galaxymap_mapclose')]:
    choices=[e for e in events if needle in e['name'].lower() and e['media']]
    if not choices and role=='map':choices=[e for e in events if 'galaxymap' in e['name'].lower() and 'click' in e['name'].lower() and e['media']]
    if not choices and role=='select':choices=[e for e in events if re.search('select|click|button',e['name'],re.I) and e['media'] and 'ambient' not in e['name'].lower()]
    if not choices:continue
    event=choices[0];mid=event['media'][0];off,size=media[mid]
    wem=R/'source-assets/audio'/f'{mid}.wem';wem.parent.mkdir(exist_ok=True);wem.write_bytes(bankdata[off:off+size])
    out=R/'Frontend/Assets/Audio'/f'{role}.wav';out.parent.mkdir(parents=True,exist_ok=True)
    subprocess.run([str(decoder/'vgmstream-cli.exe'),'-i','-o',str(out),str(wem)],check=True,cwd=R,stdout=subprocess.DEVNULL)
    with wave.open(str(out)) as w:seconds=w.getnframes()/w.getframerate()
    selected[role]={'event':event,'mediaId':str(mid),'bankDecodedOffset':off,'bytes':size,'wemSha256':sha(wem),'output':str(out.relative_to(R)).replace('\\','/'),'sha256':sha(out),'seconds':seconds,'binding':'Name embedded in original character-create GFX; Wwise FNV1 match to typed event; parsed HIRC references reach selected embedded media. Runtime role is C1 adaptation.'}
(R/'manifests/frontend-audio.json').write_text(json.dumps(selected,indent=2))
print('Recovered',selected.keys())
