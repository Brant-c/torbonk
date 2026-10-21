"""Stage the supplied 17892505 WEM for XP collection with a 12 dB reduction."""
import array
import json
import math
import shutil
import subprocess
import wave
from pathlib import Path
from source_pipeline import ROOT, sha

originals = ROOT/'source-assets/user-supplied/xp-pickup-17892505'
originals.mkdir(parents=True, exist_ok=True)
sources = []
for suffix in ['wem', 'ogg']:
    supplied = Path('C:/Users/brant/Downloads')/f'17892505.{suffix}'
    preserved = originals/supplied.name
    if preserved.exists():
        assert sha(preserved) == sha(supplied), 'Preserve existing supplied source bytes'
    else:
        shutil.copyfile(supplied, preserved)
    sources.append(dict(suppliedPath=str(supplied), preservedPath=str(preserved.relative_to(ROOT)), sha256=sha(preserved)))

decoded = ROOT/'work/b7/xp-17892505-decoded.wav'
subprocess.run([str(ROOT/'tools/vgmstream/vgmstream-cli.exe'), '-i', '-o', str(decoded), str(originals/'17892505.wem')], check=True, stdout=subprocess.DEVNULL)
with wave.open(str(decoded)) as w:
    params = w.getparams()
    assert w.getsampwidth() == 2
    samples = array.array('h', w.readframes(w.getnframes()))
gain_db = -12
gain = 10**(gain_db/20)
quiet = array.array('h', (round(s*gain) for s in samples))
output = ROOT/'godot/Assets/Presentation/Audio/xp_17892505_quiet.wav'
with wave.open(str(output), 'wb') as w:
    w.setparams(params); w.writeframes(quiet.tobytes())
output.with_suffix('.wav.import').write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\nedit/loop_mode=0\n')

config_path = ROOT/'godot/Gameplay/presentation-audio.json'
config = json.loads(config_path.read_text())
config['cues']['pickup']['path'] = 'res://Assets/Presentation/Audio/xp_17892505_quiet.wav'
config['cues']['pickup']['gainDb'] = 0
config_path.write_text(json.dumps(config, indent=2))
manifest = dict(sources=sources, selectedSource='17892505.wem',
    selection='WEM is the supplied Wwise source; supplied OGG is preserved as an alternate, not mixed with it.',
    output=str(output.relative_to(ROOT)), sha256=sha(output), processingGainDb=gain_db,
    sampleRate=params.framerate, channels=params.nchannels, frames=params.nframes, seconds=params.nframes/params.framerate,
    peak=max(abs(s) for s in quiet)/32768,
    rmsDb=20*math.log10(max(1e-12, math.sqrt(sum(s*s for s in quiet)/len(quiet))/32768)),
    conversion='vgmstream WEM to PCM16, then constant -12 dB amplitude; no normalization, resampling, pitch change, trimming or looping.',
    provenance='User-supplied files; archive origin, creator and license were not established from the filename. Not asserted to be recovered April media.',
    recipe='scripts/stage_user_xp_audio.py')
(ROOT/'manifests/xp-pickup-user-source.json').write_text(json.dumps(manifest, indent=2))
print(json.dumps(manifest, indent=2))
