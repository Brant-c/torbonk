"""Split Sniper's Lethality from Operative; run after older Agent generators."""
import copy
import json
from pathlib import Path

path = Path(__file__).resolve().parents[1] / 'godot/Gameplay/Talents/agent.json'
data = json.loads(path.read_text(encoding='utf-8-sig'))
data['Talents'] = [t for t in data['Talents'] if not t['Id'].startswith('sniper_lethality.')]

def effect(kind, ability='', amount=1, key=''):
    return dict(Kind=kind, Ability=ability, Amount=amount, Key=key)

changes = {
    'lethal_injectors': ('Prepared Toxins', 'Dart lasts two seconds longer. While in cover, your Dart and Corrosive Grenade deal 20% additional damage.', [effect('duration', 'dart', 2), effect('hook', key='sn_lethal_cover')]),
    'corrosive_microbes': ('Corrosive Payload', 'Corrosive Grenade gains 30% base radius and 15% base damage.', [effect('radius', 'corrosivegrenade', .3), effect('damage', 'corrosivegrenade', .15)]),
    'lingering_toxins': ('Cull Discipline', 'Cull gains 15% base damage and 15% cooldown recovery rate.', [effect('damage', 'agentcull', .15), effect('cooldown', 'agentcull', .15)]),
    'devouring_microbes': ('Hemotoxic Rounds', 'Weakening Blast extends your active Dart and Corrosive Grenade on each victim by two seconds, up to four seconds beyond their normal duration.', [effect('hook', key='sn_lethal_extend')]),
    'lethal_purpose': ('Lethal Alignment', 'While in cover, poison ticks recover Cull by 0.75 seconds and Weakening Blast by 0.5 seconds. Shared two-second cooldown across all victims and both poisons.', [effect('hook', key='sn_lethal_alignment')]),
    'license_to_kill': ('License to Kill', 'Cull deals 35% more base damage.', [effect('damage', 'agentcull', .35)]),
    'lethal_training': ('Toxic Precision', 'Each Cull hit echoes 25% of one tick of each of your Dart and Corrosive Grenade effects on that victim, without consuming or refreshing them.', [effect('hook', key='sn_lethal_echo')]),
    'targeted_demolition': ('Targeted Contagion', 'Weakening Blast strikes a 0.30-radius group at full damage and spreads your existing Dart and Corrosive Grenade across that group. It does not spread probes.', [effect('hook', key='sn_lethal_spread')]),
    'toxic_resilience': ('Toxic Focus', 'Cull hits while in cover grant an eight-health shield, at most once every six seconds. Weakening Blast hits recover Shield Probe by one second, at most once every two seconds.', [effect('hook', key='sn_lethal_focus')]),
}

for original in list(data['Talents']):
    if original['Tree'] != 'agent_lethality':
        continue
    original['Classes'] = ['operative']
    talent = copy.deepcopy(original)
    suffix = original['Id'].split('.', 1)[1]
    name, description, effects = changes[suffix]
    talent.update(Id='sniper_lethality.' + suffix, Classes=['sniper'], Name=name,
                  Description=description, Effects=effects,
                  Source='TorBonk Sniper Lethality adaptation; recovered artwork retained. Legacy Sniper allocations migrate by talent position.')
    data['Talents'].append(talent)

path.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')
