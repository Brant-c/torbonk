"""Recover bounded Agent presentation candidates; exact unique April lookups decide availability."""
import json
from pathlib import Path
import character_source
from source_pipeline import ROOT
character_source.MANIFEST=ROOT/'manifests/agent-presentation-source.json';s=character_source.SelectedSource()
requested={'RifleForward':'mv_rifle_run_forward','RifleBack':'mv_rifle_run_back','Shiv':'cb_spy_shiv_1','Backstab':'cb_agent_backstab','HiddenStrike':'cb_spy_waylay_01','KnifeCombo':'cb_agent_knife_combo_01','KnifeCombo2':'cb_agent_knife_combo_02','RifleGrenade':'cb_rifle_grenade_1','Detonator':'cb_rifle_detonator_01','ArtilleryStart':'cb_rifle_artillery_start_01','ArtilleryLoop':'cb_rifle_artillery_mid_01','ArtilleryEnd':'cb_rifle_artillery_end_01','RifleAutoStart':'cb_rifle_fullauto_start','RifleAutoLoop':'cb_rifle_fullauto_loop','RifleAutoEnd':'cb_rifle_fullauto_end','SniperSeries':'cb_rifle_sniper_fire_series_01','StealthIdle':'cb_rifle_stealth_idle_1','Medical':'cb_unarmed_heal_right_1','CoverIdle':'cb_rifle_cover_crouch_hide_idle_1','CoverEnter':'cb_rifle_cover_crouch_enter_far','CoverFire':'cb_rifle_cover_fire_right_1','CoverDetonator':'cb_rifle_cover_detonator_01'}
clips={};missing=[]
for key,stem in requested.items():
 path=f'/resources/anim/humanoid/bmnnew/{stem}.jba'
 if s.has(path):s.get(path,key,'Agent compatible original gesture/locomotion candidate');clips[key]=stem
 else:missing.append(path)
# Retain existing clips so all previous bindings remain valid during conversion.
old=json.loads((ROOT/'manifests/agent-conversion.json').read_text())['clips'];old.update(clips)
(ROOT/'manifests/agent-conversion.json').write_text(json.dumps(dict(clips=old,unavailable=missing),indent=2))
print(json.dumps(dict(recovered=clips,unavailable=missing),indent=2))
