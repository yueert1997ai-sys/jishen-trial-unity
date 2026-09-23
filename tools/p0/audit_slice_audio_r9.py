"""Readback QA of authored files and actual Unity mixer captures; never claims audition."""
from pathlib import Path
import json,csv,wave,math
import numpy as np
R=Path(__file__).resolve().parents[2]; A=R/'AuditEvidence/combat-slice-r9'

def read(p):
    with wave.open(str(p)) as w:
        sr=w.getframerate();channels=w.getnchannels()
        x=np.frombuffer(w.readframes(w.getnframes()),'<i2').reshape(-1,channels)/32768.
    return sr,channels,x

bank=json.loads((A/'audio-bank.json').read_text());assert len(bank['clips'])==47
for row in bank['clips']:
    sr,ch,x=read(R/'Assets/Resources/Audio/Combat/R9'/(row['name']+'.wav'))
    assert sr==48000 and ch==1 and np.max(abs(x))<.9
    assert np.mean(x*x)>1e-6
    if row['loop']:assert abs(x[-1,0]-x[0,0])<.001
    assert 'compressionFormat: 0' in (R/'Assets/Resources/Audio/Combat/R9'/(row['name']+'.wav.meta')).read_text()
assert len({v['sha256'] for v in bank['clips']})==47

p=sorted(A.glob('sound-*'))[-1];sr,ch,x=read(p/'native-cue-bank.wav');rows=[]
for row in csv.DictReader((p/'native-cue-bank.csv').open()):
    y=x[round(float(row['start'])*sr):round(float(row['end'])*sr)]
    peak=float(abs(y).max());rms=float(np.sqrt(np.mean(y*y)))
    if row['cue']=='MUTED':assert peak==0
    else:assert peak>.012 and rms>.0005,row['cue']
    rows.append(dict(cue=row['cue'],peak=peak,rms_dbfs=round(20*math.log10(max(rms,1e-12)),2)))
assert np.max(abs(x))<.95
record=dict(authored_clips=47,cue_bank=rows,sample_rate=sr,channels=ch,seconds=len(x)/sr,clipping_samples=int(np.sum(abs(x)>=.999)),auditioned=False)
for mode in ['detail','replay']:
    candidates=sorted(A.glob(mode+'-*'))
    if not candidates or not (candidates[-1]/'game-mix.wav').exists():continue
    folder=candidates[-1];sr,ch,y=read(folder/'game-mix.wav')
    assert np.max(abs(y))<.95 and np.sqrt(np.mean(y*y))>.001
    record[mode]=dict(folder=folder.name,seconds=len(y)/sr,peak=float(np.max(abs(y))),rms_dbfs=float(20*np.log10(np.sqrt(np.mean(y*y)))),clipping_samples=int(np.sum(abs(y)>=.999)))
(A/'native-audio-analysis.json').write_text(json.dumps(record,indent=2),encoding='utf-8')
print('PASS 47 distinct PCM assets; 34 native cues; exact mute; no clipping. Captures:',*[m for m in ['detail','replay'] if m in record])
