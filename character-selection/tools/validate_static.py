"""Lightweight C1 resource/data validation. Does not import, execute or launch Godot."""
from pathlib import Path
import json,re,struct
R=Path(__file__).resolve().parents[1]
c=json.loads((R/'Frontend/Data/catalog.json').read_text())
assert len(c['Factions'])==2 and len(c['Classes'])==8 and len(c['Maps'])==6
assert all(len(x['AdvancedClasses'])==2 for x in c['Classes'])
assert {x['Id'] for x in c['Classes']}=={'sith_warrior','sith_inquisitor','imperial_agent','bounty_hunter','jedi_knight','jedi_consular','smuggler','trooper'}
aa=[a for x in c['Classes'] for a in x['AdvancedClasses']]
assert len({a['Id'] for a in aa})==16
assert [a['Id'] for a in aa if a['GameplayImplemented']]==['sorcerer']
assert [m['Id'] for m in c['Maps'] if m['GameplayImplemented']]==['korriban']
assert [a['Id'] for a in aa if a.get('PreviewScene')]==['sorcerer']
paths=[]
def walk(value):
    if isinstance(value,dict):
        for v in value.values():walk(v)
    elif isinstance(value,list):
        for v in value:walk(v)
    elif isinstance(value,str) and value.startswith('res://'):paths.append(value)
walk(c)
for p in (R/'Frontend').rglob('*.tscn'):
    paths.extend(re.findall(r'path="(res://[^"]+)"',p.read_text()))
for rel in ['UI/Eurostile.ttf','UI/EurostileBold.ttf','UI/bwaui_standard_window_1.png','UI/bwaui_standard_window_24.png','UI/bwaui_standard_window_9.png','UI/bwaui_standard_window_18.png','UI/bwaui_tooltip_1.png','Slices/bwaui_character_create_window_104.png','Slices/bwaui_character_create_window_101.png','Slices/ui_galaxymap_panel_3.png','Recovered/char_select_stars.png','Recovered/char_select_tython_high_res_d.png','Recovered/gal_map_galaxy_core.png','planet.gdshader','Audio/select.wav','Audio/back.wav','Audio/map.wav']:
    paths.append('res://Frontend/Assets/'+rel)
for p in paths:assert (R/p.removeprefix('res://')).is_file(),p
for p in (R/'Frontend').rglob('*.cs'):
    text=p.read_text(); assert 'res://Assets/' not in text and 'pipeline/' not in text
assert 'GetTree().ChangeScene' not in (R/'Frontend/Scripts/FrontendFlow.cs').read_text()
for name in ['MainMenu','Faction','BaseClass','AdvancedClass','Customization','Galaxy','Ready','CharacterPreview']:
    assert (R/'Frontend/Scenes'/f'{name}.tscn').is_file()
size=sum(p.stat().st_size for p in (R/'Frontend').rglob('*') if p.is_file())
manifest=json.loads((R/'manifests/recovery.json').read_text())
report={'passed':True,'catalog':{'factions':2,'baseClasses':8,'advancedClasses':16,'maps':6},'checkedResourcePaths':len(set(paths)),
    'frontendBytes':size,'originalDecodedResources':len(manifest['resources']),'originalGfxMovies':len(manifest['definitions']),
    'missingHistoricalCandidates':len(manifest['missing']),'scenes':8,'runtimeLaunched':False,
    'scope':'Static resource presence, catalog identifiers/cardinalities, implementation flags, preview-model exclusivity, scene references, absence of gameplay launch code. No visual, import, shader, animation or audio playback validation.'}
(R/'manifests/static-validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
# Document a portable example; the real runtime snapshot uses SelectionConfiguration.ToJson().
sample={'SchemaVersion':1,'FactionId':'empire','BaseClassId':'sith_inquisitor','AdvancedClassId':'sorcerer','Appearance':{
'PresetId':'sorcerer_april2012_bmn','SpeciesId':'human','BodyPresentationId':'male','BodyTypeId':'bmn',
'HeadId':'head_human_bmn_caucasian_a01','HairId':'hair_human_bmn_non_a01','SkinToneId':'source_material_a01c01',
'ComplexionId':'source_default','EquipmentAppearanceId':'ss_a04_c01_s01','IsSupportedPreset':True},
'MapId':'korriban','Playable':True,'PlayableStatus':'content_available_integration_pending'}
(R/'Frontend/Data/selection-example.json').write_text(json.dumps(sample,indent=2))
