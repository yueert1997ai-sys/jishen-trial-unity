"""Re-author existing licensed R9 material into short layered mecha contacts. No Hades audio copied."""
from pathlib import Path
import wave,json,hashlib,re,uuid
import numpy as np
from scipy.signal import butter,sosfilt
root=Path(__file__).resolve().parents[2]
base=root/'Assets/Resources/Audio/Combat';out=base/'ImpactR2';out.mkdir(exist_ok=True)
sr=48000;rng=np.random.default_rng(91802);records=[]
def read(name,duration,speed=1):
    with wave.open(str(base/'R9'/f'{name}.wav'),'rb') as w:
        assert w.getframerate()==sr and w.getsampwidth()==2
        x=np.frombuffer(w.readframes(w.getnframes()),'<i2').astype(float)/32768
    return np.interp(np.arange(round(duration*sr))*speed,np.arange(len(x)),x,right=0)
def band(x,lo,hi):return sosfilt(butter(2,[lo,hi],btype='bandpass',fs=sr,output='sos'),x)
def noise(t,lo,hi):
    x=band(rng.normal(size=len(t)),lo,hi);return x/max(np.sqrt(np.mean(x*x)),1e-8)
def save(name,x,peak=.74):
    t=np.arange(len(x))/sr;x=band(x,48,9800)
    x*=np.minimum(t/.0004,1)*np.minimum((len(x)/sr-t)/.014,1)
    x=x/max(abs(x))*peak
    p=out/f'{name}.wav'
    with wave.open(str(p),'wb') as w:w.setparams((1,2,sr,0,'NONE','not compressed'));w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    meta=Path(str(p)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'guid: \w+','guid: '+uuid.uuid4().hex,(base/'R9/saber_hit.wav.meta').read_text()),encoding='utf-8')
    records.append({'name':name,'seconds':len(x)/sr,'peak':float(max(abs(x))),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
for i in range(3):
    suffix='' if i==0 else '_'+str(i+1)
    t=np.arange(round(.22*sr))/sr
    hit=read('saber_hit'+suffix,.22)
    x=.85*hit*np.exp(-t*4)+.15*noise(t,110,650)*np.exp(-t*65)+.085*noise(t,2200,7000)*np.exp(-t*110)
    save('saber_hit'+suffix,x,.79)
    t=np.arange(round(.12*sr))/sr
    x=.8*band(read('armor_tick_'+str(i+1),.12),500,7800)*np.exp(-t*16)
    x+=.16*noise(t,130,900)*np.exp(-t*72)
    save('hull_hit_'+str(i+1),x,.60)
    t=np.arange(round(.19*sr))/sr
    x=.75*read('armor_tick_'+str(i+1),.19,.80)+.12*noise(t,1100,5500)*np.exp(-t*90)
    x+=.055*np.sin(2*np.pi*(1700+i*170)*t)*np.exp(-t*24)
    save('armor_clash_'+str(i+1),x,.69)
    t=np.arange(round(.115*sr))/sr
    src=read('m7_attack'+suffix,.115)
    save('m7_attack'+suffix,1.08*band(src,65,1100)+.72*band(src,1100,8200),.73)
for i,(name,d,center) in enumerate([('saber_swing_1',.19,.02),('saber_swing_2',.18,.02),('saber_heavy',.29,.034)]):
    t=np.arange(round(d*sr))/sr
    # Broad air swipe; no impact or delayed hit baked into the empty swing.
    air=noise(t,350,3100)*np.exp(-((t-center)/(.02+i*.004))**2)
    x=.62*read(name,d)*np.exp(-t*5)+.14*air
    save(name,x,.56 if i<2 else .67)
t=np.arange(round(.30*sr))/sr
save('saber_hit_heavy',read('saber_hit_heavy',.30)*np.exp(-t*3)+.14*noise(t,75,520)*np.exp(-t*36)+.08*noise(t,1800,6500)*np.exp(-t*82),.84)
t=np.arange(round(.36*sr))/sr
save('armor_break',read('armor_break',.36)*.78+.21*noise(t,900,7000)*np.exp(-t*80)+.08*read('armor_tick_2',.36,.74)*np.exp(-t*6),.83)
for i in range(2):
    t=np.arange(round(.40*sr))/sr
    x=.8*read('death_'+str(i+1),.40)*np.exp(-t*2)+.17*noise(t,75,800)*np.exp(-t*34)
    tail=read('armor_tick_'+str(i+1),.40,.72);delay=round(.065*sr)
    x[delay:]+=.09*tail[:-delay]*np.exp(-t[:-delay]*8)
    save('death_'+str(i+1),x,.75)
audit=root/'AuditEvidence/impact-r2';audit.mkdir(exist_ok=True)
(audit/'authored-audio.json').write_text(json.dumps(records,indent=2))
print('Authored',len(records),'PCM transients from existing R9 material; no playback')
