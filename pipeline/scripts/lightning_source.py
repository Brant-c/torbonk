"""Bounded Force Lightning dependencies using the unchanged selected TOR reader."""
import json, re
import xml.etree.ElementTree as ET
import character_source
from source_pipeline import ROOT, sha

MANIFEST = ROOT/'manifests/lightning-source.json'
character_source.MANIFEST = MANIFEST

def source():
    if not MANIFEST.exists():
        MANIFEST.write_text(json.dumps({'profile':'April2012 (operator labelled)',
            'maximumFiles':80,'maximumDecodedBytes':64*1024*1024,'resources':[],
            'dependencies':[],'unresolved':[], 'selection':'Force Lightning only'}))
    return character_source.SelectedSource()

def decode_xml(data):
    return ET.fromstring(data.decode('utf-16') if data[:2] in (b'\xff\xfe',b'\xfe\xff') else data)

def main():
    src=source()
    work=ROOT/'work/lightning'; work.mkdir(parents=True,exist_ok=True)
    for name in ['si_force_lightning_caster','si_lightningdrain_bodyarcs']:
        path=f'/resources/art/fx/fxspec/abilities/sith_inquisitor/{name}.fxspec'
        data=src.get(path,'epp.sith_inquisitor.force_lightning.channel_loop','appearance FX action')
        xml=decode_xml(data)
        ET.indent(xml)
        (work/(name+'.xml')).write_text(ET.tostring(xml,encoding='unicode'),encoding='utf-8')
        print('FXSpec',name,len(data),'decoded bytes')
    queue=[r['path'] for r in src.manifest['resources'] if r['path'].endswith('.fxspec')]
    seen=set()
    while queue:
        path=queue.pop(0)
        if path in seen: continue
        seen.add(path)
        data=src.get(path,'Force Lightning dependency traversal','read selected source')
        if path.endswith('.prt'):
            fields=[(m.group(1),m.group(2)) for m in re.finditer(r'^\s*\.(\w+)=(.*)$',data.decode().replace('\r',''),re.M)]
            (work/(path.rsplit('/',1)[-1]+'.json')).write_text(json.dumps(dict(fields),indent=2))
        else:
            fields=[(f.get('name'),f.text or '') for f in decode_xml(data).iter('f')]
        for field,raw in fields:
            value=raw.strip().replace('\\','/').lower()
            if not value.endswith(('.dds','.gr2','.prt','.mat','.zzp','.lit')): continue
            p='/resources/'+value.lstrip('/')
            if src.has(p):
                data=src.get(p,path,field)
                if p.endswith(('.prt','.mat')):
                    queue.append(p)
            else:
                rec={'from':path,'to':p,'field':field,'status':'unsupported engine primitive' if '/engine/' in p else 'unresolved: no unique archive entry'}
                if rec not in src.manifest['unresolved']: src.manifest['unresolved'].append(rec)
    MANIFEST.write_text(json.dumps(src.manifest,indent=2))
    for p in ['/resources/bnk2/abilities_imperialmelee_data.bnk','/resources/bnk2/abilities_imperialmelee_media.bnk','/resources/bnk2/init.bnk','/resources/bnk/init.bnk']:
        if src.has(p):
            print('BANK',p,len(src.get(p,'Force Lightning Wwise events','bank graph / media bank')))
        else: print('BANK ABSENT',p)
    for bucket in [132,435]:
        src.get(f'/resources/systemgenerated/buckets/{bucket}.bkt','Force Lightning GOM trace','ability or appearance evidence bucket')

if __name__=='__main__': main()
