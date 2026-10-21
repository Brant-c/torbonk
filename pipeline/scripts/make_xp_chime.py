"""Author a soft XP reward tone from sine oscillators; no third-party samples."""
import array, hashlib, json, math, wave
from source_pipeline import ROOT, sha

rate = 44100
duration = 0.28
output = ROOT / 'godot/Assets/Presentation/Audio/xp_soft_chime.wav'
samples = []
for i in range(round(rate * duration)):
    t = i / rate
    # Rounded attack, warm fundamental and quieter fifth. All oscillators start at zero.
    attack = 1 - math.exp(-t / 0.012)
    tail = min(1, max(0, (duration-t) / 0.045))
    tone = math.sin(math.tau * 659.255 * t) * math.exp(-t * 15)
    tone += 0.24 * math.sin(math.tau * 987.767 * t) * math.exp(-t * 20)
    tone += 0.08 * math.sin(math.tau * 1318.51 * t) * math.exp(-t * 27)
    samples.append(round(32767 * 0.26 * attack * tail * tone))
pcm = array.array('h', samples)
with wave.open(str(output), 'wb') as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate); w.writeframes(pcm.tobytes())
output.with_suffix('.wav.import').write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\n')
rms = math.sqrt(sum(s*s for s in samples)/len(samples))/32768
manifest = dict(output=str(output.relative_to(ROOT)), sha256=sha(output), sampleRate=rate,
    frames=len(samples), seconds=duration, peak=max(abs(s) for s in samples)/32768,
    rmsDb=20*math.log10(rms), source='Authored procedural sine synthesis; no external media or samples',
    recipe='scripts/make_xp_chime.py', frequenciesHz=[659.255, 987.767, 1318.51],
    attribution='New project audio; not recovered SWTOR media. No third-party sample attribution required.')
(ROOT/'manifests/xp-chime.json').write_text(json.dumps(manifest, indent=2))
print(json.dumps(manifest, indent=2))
