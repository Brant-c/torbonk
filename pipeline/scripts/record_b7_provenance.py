import json
from source_pipeline import ROOT,sha,pathhash
import character_source
character_source.MANIFEST=ROOT/'manifests/b7-source.json';src=character_source.SelectedSource()
p='/resources/bnk2/swtor_startup.bnk';data=src.get(p,'all_level_up_red.fxspec','typed HIRC level-up event and exact unique April path')
e=json.loads((ROOT/'manifests/b7-gui-bank.json').read_text());assert e['archiveHash']==f'0x{pathhash(p):016X}'
e['path']=p;e['evidence']='Client-derived: unique exact April lookup; FNV-1 event IDs match typed HIRC events referenced by all_level_up_red.fxspec';(ROOT/'manifests/b7-gui-bank.json').write_text(json.dumps(e,indent=2))
visual=json.loads((ROOT/'manifests/b7-visuals.json').read_text())
for name,p,binding in [('loot_glow','/resources/art/fx/texture/glow/bw_flare_gray.dds','loot_common.fxspec -> loot_core_glow.prt -> loot_core_glow_p.prt -> TextureName'),('loot_ring','/resources/art/fx/texture/symbol/tri_ring_blurred.dds','loot_common.fxspec -> _fxProjectionTexture')]:
 output=ROOT/f'godot/Assets/Presentation/{name}.png';source=next(x for x in src.manifest['resources'] if x['path']==p)
 visual['assets']=[r for r in visual['assets'] if r['output']!=str(output.relative_to(ROOT))]
 visual['assets'].append({'source':p,'sourceSha256':source['sha256'],'binding':binding,'output':str(output.relative_to(ROOT)),'sha256':sha(output),'conversion':'Pillow DDS -> RGBA PNG at native size; no resample or pixel tint'})
(ROOT/'manifests/b7-visuals.json').write_text(json.dumps(visual,indent=2))
r=json.loads((ROOT/'work/b7/research.json').read_text());r['evidence']='Client-derived; historical names are candidates until exact unique April lookup and decoded source hashes in b7-source.json.';r['limits']=['Level-popup root is 1 frame at 24fps; all recovered sprites also 1 frame. Native animation timing is not recoverable from these timelines.','Achievement popup/library and loot_generic/loot_glow FXSpec exact paths had no unique April match; not proof of absence from unnamed entries.','Original boss health frame native widget linkage not recovered; existing recovered bars and window art adapted.','Original dedicated hover/upgrade/boss audio bindings not recovered; recovered pickup, level-up probes and generic creature impacts reused.'];(ROOT/'manifests/b7-research.json').write_text(json.dumps(r,indent=2))
print('Recorded original startup path, selected visual hashes and bounded research limits.')
