"""Render representative April Wwise layered events into finite PCM cues.

All inputs are recovered client audio. Gains, the 200ms layer delay and random
container choices are taken from the saved wwiser graphs; native random history
and bus processing remain adaptations. No synthesized sound is added.
"""
from pathlib import Path
import array,hashlib,json,math,re,sys,wave
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'godot/Assets/Agent/Audio'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p):
 with wave.open(str(p)) as w:
  assert w.getsampwidth()==2 and w.getframerate()==44100 and w.getnchannels() in (1,2)
  ch=w.getnchannels();data=array.array('h',w.readframes(w.getnframes()))
  if sys.byteorder!='little':data.byteswap()
  return data,ch
def rms(values):return math.sqrt(sum(v*v for v in values)/max(1,len(values)))/32768
def mix(name,event,layers):
 inputs=[];decoded=[];end=0
 for media,gain,delay in layers:
  p=OUT/f'{media}.wav';data,ch=read(p);offset=round(delay*44100)
  decoded.append((data,ch,10**(gain/20),offset));end=max(end,offset+len(data)//ch)
  inputs.append(dict(media=str(media),source=str(p.relative_to(ROOT)),sha256=digest(p),gainDb=gain,delaySeconds=delay,rms=rms(data)))
 samples=[0.]*(end*2)
 for data,ch,gain,offset in decoded:
  for i in range(len(data)//ch):
   samples[(i+offset)*2]+=data[i*ch]*gain;samples[(i+offset)*2+1]+=data[i*ch+(1 if ch==2 else 0)]*gain
 peak=max(map(abs,samples));headroom=min(1,32767*.95/max(1,peak))
 result=array.array('h',(round(v*headroom) for v in samples));p=OUT/(name+'.wav')
 with wave.open(str(p),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(44100);w.writeframes(result.tobytes())
 p.with_suffix('.wav.import').write_text('[remap]\nimporter="wav"\ntype="AudioStreamWAV"\n\n[params]\ncompress/mode=0\nedit/trim=false\nedit/normalize=false\n')
 return dict(event=event,inputs=inputs,output=str(p.relative_to(ROOT)),sha256=digest(p),peakReductionDb=20*math.log10(headroom),rms=rms(result),seconds=end/44100)
records=[mix('laceration_impact_layered','Play_ability_spy_laceration_impact',[(142346317,-3,.2),(355335624,0,0),(213894810,-6,0)]),
 mix('laceration_followup_layered','TorBonk follow-up adapter: original alternate slash and recovered contact media',[(247334649,0,0),(82513035,-3,0),(52499594,-3,.12)]),
 mix('explosiveprobe_explode_layered','Play_ability_spy_explosiveprobe_explode',[(3188403,-3,0),(97066083,-3,0),(26887271,0,0),(35854337,-3,0)])]
assert records[0]['rms']>records[0]['inputs'][0]['rms']*3, 'Main Laceration impact must be present and audible above the old lone layer'
for id,fields in {'laceration':{'ImpactAudio':'res://Assets/Agent/Audio/laceration_impact_layered.wav','FollowupImpactAudio':'res://Assets/Agent/Audio/laceration_followup_layered.wav'},'explosiveprobe':{'CastAudio':'res://Assets/Agent/Audio/149086699.wav','ImpactAudio':'res://Assets/Agent/Audio/explosiveprobe_explode_layered.wav'}}.items():
 p=ROOT/f'godot/Gameplay/Abilities/{id}.tres';s=p.read_text()
 for key,value in fields.items():
  line=f'{key} = "{value}"';s=re.sub('^'+key+' = .*$',lambda m:line,s,flags=re.M) if re.search('^'+key+' = ',s,re.M) else s+'\n'+line+'\n'
 p.write_text(s)
(ROOT/'manifests/agent-layered-audio-repair.json').write_text(json.dumps(dict(evidence='Client-derived original Wwise event graph composition; representative random choices',records=records),indent=2)+'\n')
for r in records:print(r['event'],'rms',round(r['rms'],4),'peak reduction dB',round(r['peakReductionDb'],2))
