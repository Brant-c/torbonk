"""Select source terrain/placements, resolve exact models, materials and maps."""
import json, struct, xml.etree.ElementTree as ET
from source_pipeline import ROOT
from korriban_source import source, local, AREA
from probe_korriban import area, room
from korriban_terrain import decode

RECIPE=json.loads((ROOT/'korriban-recipe.json').read_text())
PATCHES=RECIPE['terrainInstanceIds']

def canonical(p):return '/resources/'+p.replace('\\','/').lower().lstrip('/')
def vec(f,key,default):return [float(v) for v in f.get(key,default).strip('()').split(',')]
def main():
    src=source(); sec=area(); recs=room()
    assets={line.strip().split('=',1)[0]:canonical(line.strip().split('=',1)[1]) for line in sec['ASSETS'] if '=' in line}
    terrain=[]
    for id in PATCHES:
        rec=recs[id];data=decode(rec)
        assert rec['fields']['ParentInstance']=='0' and vec(rec['fields'],'Rotation','(0,0,0)')==[0,0,0] and vec(rec['fields'],'Scale','(1,1,1)')==[1,1,1]
        if data['embeddedPosition'] is not None:
            assert max(abs(a-b) for a,b in zip(data['embeddedPosition'],vec(rec['fields'],'Position','(0,0,0)')))<1e-8
        terrain.append(dict(id=id,asset=rec['asset'],room='baseroom',sourceLine=rec['line'],
            tessellation=rec['fields'].get('tesselation',''),
            position=vec(rec['fields'],'Position','(0,0,0)'),**data))
    # This bounded source-origin crop is refined by the full matrix stage in Blender.
    # Baseroom stores the terrain, valley rocks, statues and outdoor architecture.
    candidates=[]
    for rec in recs.values():
        path=assets.get(rec['asset'],'')
        if not path.endswith('.gr2'):continue
        f=rec['fields']; p=vec(f,'Position','(0,0,0)')
        bounds=RECIPE['propRootOriginBoundsXYZ']
        if f.get('ParentInstance','0')=='0' and bounds[0]<=p[0]<=bounds[1] and bounds[4]<=p[2]<=bounds[5] and bounds[2]<=p[1]<=bounds[3]:
            if any(s in path for s in ['/dbo_','occluder','fadeportal','collision']):continue
            if f.get('Hidden','false')=='false':candidates.append(rec['id'])
    # Include descendants and required ancestors, never discard rotated/scaled parents.
    selected=set(candidates)
    for _ in range(len(recs)):
        additions={r['id'] for r in recs.values() if r['fields'].get('ParentInstance','0') in selected}
        additions|={recs[id]['fields']['ParentInstance'] for id in selected if recs[id]['fields'].get('ParentInstance','0')!='0'}
        if additions<=selected:break
        assert additions<=recs.keys(),'parent outside selected source room'
        selected|=additions
    placements=[]
    for id in sorted(selected):
        rec=recs[id];f=rec['fields'];path=assets.get(rec['asset'],'')
        placements.append(dict(id=id,asset=rec['asset'],path=path,room='baseroom',sourceLine=rec['line'],
            parent=f.get('ParentInstance','0'),position=vec(f,'Position','(0,0,0)'),
            rotation=vec(f,'Rotation','(0,0,0)'),scale=vec(f,'Scale','(1,1,1)'),fields=f))
    modelpaths=sorted({p['path'] for p in placements if p['path'].endswith('.gr2') and p['fields'].get('Hidden','false')=='false'})
    assert len(modelpaths)<=RECIPE['maximumUniqueModels'] and len(placements)<=RECIPE['maximumPlacements'],(len(modelpaths),len(placements))
    materials={}
    def material(path,parent):
        if path in materials:return materials[path]
        xml=ET.fromstring(src.get(path,parent,'source material dependency'))
        textures={};values={}
        for inp in xml.findall('.//input'):
            semantic=inp.findtext('semantic');value=inp.findtext('value')
            if inp.findtext('type','').lower()=='texture':
                tex=canonical(value)
                if not tex.endswith('.dds'):tex+='.dds'
                src.get(tex,path,semantic);textures[semantic]=tex
            else:values[semantic]=value
        # All properties are also retained in the original MAT.
        info=dict(path=path,derived=xml.findtext('Derived'),alpha=xml.findtext('AlphaMode'),
                  twoSided=xml.findtext('IsTwoSided'),textures=textures,values=values)
        materials[path]=info;return info
    models=[]
    for path in modelpaths:
        b=src.get(path,'bounded baseroom placements','ASSETS exact model path')
        assert b[:4]==b'GAWB' and struct.unpack_from('<I',b,4)[0]==4
        count=struct.unpack_from('<H',b,26)[0];offset=struct.unpack_from('<I',b,88)[0]
        names=[]
        for i in range(count):
            off=struct.unpack_from('<I',b,offset+4*i)[0]
            name=b[off:b.index(b'\0',off)].decode('ascii')
            mat='/resources/art/shaders/materials/'+name+'.mat'
            material(mat,path);names.append(mat)
        models.append(dict(path=path,materials=names))
    layers={}
    used=sorted({int(k) for p in terrain for k,v in p['weights'].items() if max(v)>0})
    for line in sec['TERRAINTEXTURES']:
        id,raw=line.strip().split(':',1)
        if int(id) not in used:continue
        if raw.startswith('bwa:'):
            mat='/resources/art/shaders/materials/'+raw.split(':')[1]+'.mat'
            layers[id]=dict(source=raw,material=material(mat,'area terrain layer '+id))
        else:
            tex=canonical(raw.split(':')[0]);src.get(tex,'area terrain layer '+id,'direct terrain diffuse')
            layers[id]=dict(source=raw,texture=tex)
    out=dict(area=AREA,room='baseroom',selection='Valley of the Dark Lords statue approach',
        patchIds=PATCHES,cropXZ=[20.2,45.4,6.2,18.8],propOriginMarginXZ=[20.1,48,6.1,20],
        terrain=terrain,placements=placements,models=models,materials=materials,terrainLayers=layers,
        transformPolicy='raw Y-up, source units 1:1; local T * Euler ZXY * S; compose complete ancestry',
        lightingEvidence=sec['SCHEMES'])
    (ROOT/'work/korriban/selection.json').write_text(json.dumps(out))
    print('SELECTION',len(terrain),'patches',len(placements),'placements',len(models),'models',len(materials),'materials')
    print('FAMILIES',sorted({m['derived'] for m in materials.values()}))
    for m in models:print(m['path'])
    for id,layer in layers.items(): print('LAYER',id,layer)

if __name__=='__main__':main()
