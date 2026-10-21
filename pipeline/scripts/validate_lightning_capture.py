"""Measure the actual Godot WASAPI/master-bus capture, including silent cleanup."""
import array, json, math, wave
from source_pipeline import ROOT, sha

path=ROOT/'generated/lightning/godot-channel-audio.wav'
with wave.open(str(path)) as w:
    assert w.getsampwidth()==2
    channels=w.getnchannels(); rate=w.getframerate(); count=w.getnframes()
    samples=array.array('h',w.readframes(count))
def rms(values): return math.sqrt(sum(float(v)*v for v in values)/max(1,len(values)))/32768
tail=samples[-int(rate*channels*0.1):]
result={'passed':rms(samples)>0.001 and rms(tail)<0.00001,'captureSha256':sha(path),
        'sampleRate':rate,'channels':channels,'frames':count,'seconds':count/rate,
        'rms':rms(samples),'last100msRms':rms(tail),'peak':max(abs(v) for v in samples)/32768,
        'clippedSamples':sum(abs(v)>=32767 for v in samples),
        'scope':'Actual reconstructed presentation mixed by Godot, not a capture from the SWTOR client.'}
(ROOT/'manifests/lightning-runtime-audio-validation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2)); assert result['passed']
