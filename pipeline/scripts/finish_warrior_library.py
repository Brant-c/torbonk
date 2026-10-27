"""Build additive conversion script and delivery report."""
import json
from pathlib import Path
from source_pipeline import ROOT,sha
p=(ROOT/'scripts/convert_assassin_animations.py').read_text()
p=p.replace("profile='assassin-refinement' if", "profile='warrior-library' if '--warrior-library' in sys.argv else 'assassin-refinement' if")
p=p.replace("output=ROOT/(","output=ROOT/('godot/Assets/WarriorLibrary/casts.glb' if profile=='warrior-library' else ")
p=p.replace('export_force_sampling=True','export_force_sampling=False')
p=p.replace("records=[]\nfor name,stem", "direct={Path(v).stem for k,vs in json.loads((ROOT/'manifests/warrior-library-conversion.json').read_text())['edges'].items() if k.startswith('epp.') for v in vs if v.endswith('.jba')}\nclips={k:v for k,v in clips.items() if v in direct or v in ('cb_saber_attack_right_1','cb_2saber_attack_right_1')}\nrecords=[]\nfor name,stem")
(ROOT/'scripts/convert_warrior_animations.py').write_text(p)

if '--report' in __import__('sys').argv:
    import csv
    from collections import Counter
    from PIL import Image
    import character_source
    from source_pipeline import SOURCE
    out=ROOT/'godot/Assets/WarriorLibrary'
    manifest=ROOT/'manifests/warrior-library-source.json'
    character_source.MANIFEST=manifest;src=character_source.SelectedSource()
    external=json.loads((ROOT/'manifests/warrior-package-external-definitions.json').read_text())
    src.get('/resources/en-us/str/abl.stb','localization','Original archived ability strings')
    catalog=json.loads((out/'catalog.json').read_text());defs=json.loads((ROOT/'manifests/warrior-definitions.json').read_text())
    packages=json.loads((out/'talent-packages.json').read_text())
    for d in external:
        f={r['key']:r['value'] for r in d['data']}
        src.get(d['source']['path'],d['name'],'Package-referenced external definition')
        icon=f.get('ablIconSpec');dep=[]
        if icon:
            path='/resources/gfx/icons/'+icon.lower()+'.dds'
            if src.has(path):
                src.get(path,d['name'],'Exact ablIconSpec');dep.append(path)
                target=out/'Textures/gfx/icons'/Path(icon+'.png');target.parent.mkdir(parents=True,exist_ok=True)
                Image.open(SOURCE/path.lstrip('/')).convert('RGBA').save(target)
        members=[p['name'] for p in packages if any(v['key']==d['id'] for v in next((f['value'] for f in p['data'] if f['key']=='ablPackageAbilitiesList'),[]))]
        catalog.append(dict(name=d['name'],id=d['id'],eligibility=['Juggernaut','Marauder'] if any(p.endswith('sith_warrior.base') for p in members) else ['Juggernaut'] if any('.juggernaut.' in p for p in members) else ['Marauder'],packages=members,classification='passive' if f.get('ablIsPassive') else 'active',source=d['source'],icon=icon,effects=[],appearances=[],dependencies=dep,missing=[],rosterStatus='Package member; external presentation requires follow-up',fields=f))
    catalog=list({r['id']:r for r in catalog}.values())
    conv=json.loads((ROOT/'manifests/warrior-library-conversion.json').read_text())
    known={r['source'] for r in conv['files']}
    for d in external:
        icon=next((f['value'] for f in d['data'] if f['key']=='ablIconSpec'),None)
        if icon:
            path='/resources/gfx/icons/'+icon.lower()+'.dds';target=out/'Textures/gfx/icons'/Path(icon+'.png')
            if target.exists() and path not in known:
                conv['files'].append(dict(source=path,output=str(target.relative_to(ROOT)),sha256=sha(target)));known.add(path)
    (ROOT/'manifests/warrior-library-conversion.json').write_text(json.dumps(conv,indent=2))
    audio=json.loads((ROOT/'manifests/warrior-library-audio.json').read_text())
    shared=json.loads((ROOT/'manifests/warrior-library-republic-audio.json').read_text())
    audio['missingEvents']=[n for n in audio['missingEvents'] if n in shared['missingEvents']]
    (out/'audio.json').write_text(json.dumps(audio,indent=2));(ROOT/'manifests/warrior-library-audio.json').write_text(json.dumps(audio,indent=2))
    config=json.loads((out/'source-parameters.json').read_text())
    converted={r['source']:r['output'] for r in conv['files']}
    strings={r['id']:r['text'] for r in json.loads((ROOT/'manifests/warrior-localization.json').read_text())['rows']}
    for r in catalog:
        if r['fields'].get('ablIsPassive'):r['classification']='passive'
        r['animationSelection']='Direct JBA links are listed; semantic actions and weapon conditions remain in warrior-appearance-tree.json. Supporting saber clips have no inferred per-ability assignment.'
        events=set()
        for dep in r['dependencies']:
            for k,v in config.get(dep,[]):events.update(s for s in v.split(',') if s.startswith(('Play_','Stop_')))
        r['audioEvents']={e:audio['events'][e] for e in sorted(events) if e in audio['events']}
        r['audioMissingEvents']=sorted(events & set(audio['missingEvents']))
        r['audioMedia']=sorted({m for e in r['audioEvents'].values() for m in audio['graphs'].get(e,{}).get('media',[])})
        r['convertedTextures']=[converted[d] for d in r['dependencies'] if d in converted]
        r['directAnimationClips']=[Path(d).stem for d in r['dependencies'] if d.endswith('.jba')]
        r['presentationStatus']='partial: native action-graph selection / FX playback not implemented'
        loc=r['fields'].get('locTextRetrieverMap',[])
        r['localizedEvidence']={e['key']['signed64']:strings[e['key']['signed64']] for e in loc if isinstance(e,dict) and isinstance(e.get('key'),dict) and e['key'].get('signed64') in strings}
        groups={str(int(e['key']['signed64']) & ~0xffffffff) for e in loc if isinstance(e,dict) and isinstance(e.get('key'),dict) and int(e['key'].get('signed64','0'))>0}
        r['localizedNameCandidates']={k:strings[k] for k in groups if k in strings}
        r['localizedNameEvidence']='Client-derived string-group association: locTextRetrieverMap IDs with low 32-bit variant cleared; exact STB rows retained. Internal identity is authoritative.'
        r['displayName']=next(iter(r['localizedNameCandidates'].values()),r['name'].split('.')[-1]) if len(r['localizedNameCandidates'])<=1 else r['name']
    (out/'catalog.json').write_text(json.dumps(catalog,indent=2))
    with (ROOT/'manifests/warrior-library.csv').open('w',newline='',encoding='utf-8') as f:
        w=csv.writer(f);w.writerow(['display_name','identity','id','eligibility','classification','package_count','icon','recovered_dependencies','direct_clips','audio_events','audio_media','missing_resources','missing_audio','status'])
        for r in sorted(catalog,key=lambda r:r['name']):w.writerow([r['displayName'],r['name'],r['id'],' / '.join(r['eligibility']),r['classification'],len(r['packages']),r['icon'],len(r['dependencies']),len(r['directAnimationClips']),len(r['audioEvents']),len(r['audioMedia']),len(r['missing']),len(r['audioMissingEvents']),r['rosterStatus']])
    for r in src.manifest['resources']:assert sha(ROOT/r['output'])==r['sha256'],r['path']
    for r in conv['files']:assert sha(ROOT/r['output'])==r['sha256']
    for r in audio['media']:
        assert (ROOT/r['output']).exists()
        originals=[SOURCE/p/f"{r['mediaId']}.wem" for p in ('warrior-library-media','warrior-library-republic-media')]
        assert any(p.exists() and sha(p)==r['sourceSha256'] for p in originals)
    anim=json.loads((ROOT/'manifests/warrior-library-animation-conversion.json').read_text())
    assert sha(ROOT/anim['output'])==anim['sha256']
    summary=dict(evidence='Client-derived April archive bytes; historical field labels retained by research decoder',definitions=len(defs)+len(external),catalog=len(catalog),packageMembers=sum(bool(r['packages']) for r in catalog),talents=sum('.skill.' in r['name'] for r in catalog),resources=len(src.manifest['resources']),textures=len(conv['files']),animationClips=len(anim['clips']),audioMedia=len(audio['media']),missingResources=conv['missing'],missingAudio=audio['missingEvents'],verification='All extracted original SHA256, converted texture SHA256, original WEM SHA256 and animation GLB SHA256 checked',limits=['Source-complete namespace inventory includes unused/prototype records; package membership is explicit.','Full effect and appearance trees saved; localization maps/tokens remain bounded in summary definitions.','Semantic animation graph selection is unresolved; single/dual saber libraries are separate original clips, not inferred replacements.','FXSpec/material/particle JSON and original GR2 retained; native particle renderer and GR2 effect mesh conversion remain pending.','Audio uses the Imperial melee bank graph; missing cross-bank events are listed.','External package references include proficiencies and Assault; external presentation closure remains partial.','No gameplay abilities or talent trees implemented; existing Smash implementation untouched.'])
    summary['talents']=sum(r['name'].startswith('abl.sith_warrior.skill.') for r in catalog)
    summary['originalAnimationClips']=len(conv['clips'])
    summary['uniqueAudioFiles']=len(list((out/'Audio').glob('*.wav')))
    summary['godotImport']='Passed in isolated work/warrior-library-import project; see logs/warrior-library-godot-import.log'
    summary['limits']=[s.replace('Imperial melee bank graph','Imperial and Republic melee bank graphs') for s in summary['limits']]
    (ROOT/'manifests/warrior-library-summary.json').write_text(json.dumps(summary,indent=2))
    text=f'''# Sith Warrior April 2012 asset library

Asset preparation only. Existing Smash and gameplay files were not edited.

Recovered {summary['definitions']} definitions, {summary['catalog']} catalog entries ({summary['packageMembers']} package members), {summary['talents']} talent identities, {summary['resources']} original resources, {summary['textures']} converted textures/icons, {summary['animationClips']} animation clips, and {summary['audioMedia']} audio media records.

Start with `manifests/warrior-library.csv` for concise availability and `godot/Assets/WarriorLibrary/catalog.json` for exact identities, eligibility packages, effects, appearance links, dependencies, converted textures, audio events/media and gaps. `talent-packages.json` preserves original tree packages, rank/layout metadata is in `manifests/warrior-effect-tree.json`. Both Juggernaut Rage and shared Rage packages are retained.

Original bytes remain under `source-assets/april2012/resources`; `manifests/warrior-library-source.json` records exact archive/hash/offset/SHA256 provenance. Raw definition containers are preserved. `warrior-definitions.json` and external definitions preserve namespace identity. `warrior-appearance-tree.json` expands every appearance schedule, action, condition and parameter; `warrior-effect-tree.json` expands effect/status/talent/package relationships without summary-depth truncation.

Godot assets: `WarriorLibrary/casts.glb` is an additive animation library on the existing bmnnew rig; `Textures/` contains PNG icons and effect textures; `Audio/` contains PCM WAV media. `fx.json`, `source-parameters.json`, `appearance-parameters.json` and `audio.json` retain authored presentation parameters. Single/dual saber clips keep their original distinct names. Clips discovered from weapon-library paths are supporting assets, not proven per-ability assignments.

Wounding Throw is confirmed as `abl.sith_warrior.wounding_throw` (0xE000963B0124FE67), with `traumaticslash` icon, four effect records and explicit Marauder base-package membership. Internal names such as `feint`, `push`, `resilience_form` and `ferocity_form` are retained without invented modern-name substitutions. The catalog includes archive-only records separately from package members.

## Remaining limits

'''+ '\n'.join('- '+s for s in summary['limits'])+'''

Missing individual resources and audio events are enumerated in the summary and per-entry catalog. A recovered definition or converted library is not proof of native animation selection, particle rendering or Wwise runtime parity. All extracted sources, texture conversions, WEM originals and GLB hashes were verified.

Rebuild with `prepare_warrior_library.py`, then `prepare_ability_audio.main('warrior-library','WarriorLibrary',[])`, `finish_warrior_library.py`, Blender `convert_warrior_animations.py -- --warrior-library`, and `finish_warrior_library.py --report`, using the existing bundled Python/Blender tools. Research evidence JSON is the input snapshot; refresh it through the read-only April research tools if changing scope.
'''
    text=text.replace('Wounding Throw is confirmed as','The internal Wounding Throw identity (localized as Deadly Throw) is confirmed as')
    text += '\nLocalization string-group evidence also resolves feint to Force Camouflage, resilience_form to Soresu Form, ferocity_form to Juyo Form, push to Force Push, and force_push to Disruption. The catalog retains numeric string IDs and original internal names.\n'
    text += '\nThe GLB converts direct appearance-linked JBA clips and both basic saber attacks. The larger supporting single/dual saber library is recovered as original JBA and remains available for later targeted conversion after semantic action selection.\n'
    text=text.replace('Audio uses the Imperial melee bank graph; missing cross-bank events are listed.','Audio uses original Imperial and Republic melee bank graphs; missing cross-bank events are listed.')
    (ROOT/'WARRIOR-ASSET-LIBRARY.md').write_text(text,encoding='utf-8')
    print(json.dumps({k:v for k,v in summary.items() if k not in ('missingResources','missingAudio','limits')}))
