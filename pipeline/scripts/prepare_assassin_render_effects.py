"""Build a bounded render recipe from selected April FXSpec -> PRT leaf dependencies."""
import json,re
from pathlib import Path
P=Path(__file__).resolve().parents[1]/'godot/Assets/Assassin';cfg=json.loads((P/'source-parameters.json').read_text());fx=json.loads((P/'fx.json').read_text())
def canon(v):
 v=v.strip().strip('"').replace('\\','/').lower();v='/resources/'+v.lstrip('/') if not v.startswith('/resources/') else v
 if '/' not in v[len('/resources/'):]:v='/resources/art/fx/particles/'+v.split('/')[-1]
 return v

def leaves(path,seen=None):
 seen=set() if seen is None else seen
 if path in seen:return []
 seen.add(path);fields=dict(cfg.get(path,[]));out=[]
 if fields.get('TextureName'):
  t=Path(fields['TextureName'].replace('\\','/')).stem
  if (P/(t+'.png')).exists():
   colors=re.findall(r'#([0-9.]+),([0-9.]+),([0-9.]+),([0-9.]+)',fields.get('DiffuseColor',''))
   colors=[list(map(float,c)) for c in colors];color=max(colors,key=lambda c:sum(c[:3])*c[3]) if colors else [1,1,1,1]
   hues=[list(map(float,c)) for c in re.findall(r'#([0-9.]+),([0-9.]+),([0-9.]+),([0-9.]+)',fields.get('ParticleHueColors',''))]
   hue=next((c for c in hues if max(c[:3])>.2 and min(c[:3])<max(c[:3])*.7),None)
   if hue and fields.get('UseParticleHueing')=='true':color=[color[i]*hue[i] for i in range(4)]
   atlas=2 if '2x2' in t or '2x2' in fields.get('TextureName','') else 5 if '5x5' in t else 1
   out.append(dict(texture=t,atlas=atlas,alpha=fields.get('DestBlend')=='D3DBLEND_INVSRCALPHA',color=color,source=path))
 for k,v in fields.items():
  if v.endswith('.prt') and k!='Name':out+=leaves(canon(v),seen)
 return out
result={}
for name in fx:
 textures=[]
 for e in fx[name].get('_fxEmitterList',[]):textures+=leaves(canon(e['_fxResourceName']))
 unique={}
 for t in textures:unique.setdefault(t['texture'],t)
 result[name]=list(unique.values())[:8]
 if name=='si_spike_target_02':result[name]=[t for t in result[name] if t['texture'] in ['electric_rings_bw_02','sparks_core_halo']]
(P/'render-effects.json').write_text(json.dumps(result,indent=2))
print({k:[t['texture'] for t in v] for k,v in result.items() if k in ['si_spike_target_02','si_wither_debuff_target','si_maul_01','si_mindtrick_loop_01','si_reflection_buff_loop','jc_lowslash_impact_01']})
