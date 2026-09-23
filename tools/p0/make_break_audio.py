"""Original transient layers over the project's existing metal recordings; no external game audio."""
from pathlib import Path
import math,random,wave,array,json
root=Path(__file__).resolve().parents[2]
out=root/'Assets/Resources/Audio/Combat/R7';out.mkdir(parents=True,exist_ok=True)
source=root/'Assets/Resources/Audio/Combat/RaikenV7/armor_break_heavy.wav'
with wave.open(str(source),'rb') as w:
    assert w.getsampwidth()==2
    rate=w.getframerate();channels=w.getnchannels();raw=array.array('h',w.readframes(w.getnframes()))
    metal=[sum(raw[i:i+channels])/(channels*32768) for i in range(0,len(raw),channels)]
reports=[]
for finish in [False,True]:
    rng=random.Random(16092026+finish);sr=48000;duration=.56 if finish else .43
    samples=[];last=0
    for i in range(int(sr*duration)):
        t=i/sr;noise=rng.uniform(-1,1);high=noise-last;last=noise
        # Crisp initial crack, descending electric snap, delayed fractured tinkles, compact low body.
        crack=high*math.exp(-t/(.010 if finish else .006))*.28
        snap=math.sin(2*math.pi*(2100*t-2300*t*t))*math.exp(-t/.044)*(.09 if finish else .16)
        chips=sum(math.sin(2*math.pi*f*(t-delay))*math.exp(-(t-delay)/.030)*.045 for f,delay in [(3700,.025),(2800,.052),(4550,.083)] if t>=delay)
        bass=math.sin(2*math.pi*(115*t-85*t*t))*math.exp(-t/(.11 if finish else .045))*(.36 if finish else .13)
        index=int(t*rate*(.83 if finish else 1.35));body=metal[index] if index<len(metal) else 0
        fade=min(1,t/.001)*min(1,(duration-t)/.025)
        samples.append((crack+snap+chips+bass+body*(.72 if finish else .42))*fade)
    peak=max(abs(x) for x in samples);gain=.89/max(.89,peak)
    data=array.array('h',(round(x*gain*32767) for x in samples))
    name='armor_finish' if finish else 'armor_break'
    with wave.open(str(out/(name+'.wav')),'wb') as w:
        w.setparams((1,2,sr,0,'NONE','not compressed'));w.writeframes(data.tobytes())
    reports.append({'name':name,'duration':duration,'peak':max(abs(x) for x in data)/32768,'clipped':sum(abs(x)>=32767 for x in data),'source':str(source)})
evidence=root/'AuditEvidence/combat-slice-r7'
(evidence/'audio-assets.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print(json.dumps(reports,indent=2))
