"""Offline authoring only. Existing Kenney CC0 recordings plus original synthesis; no device playback."""
from pathlib import Path
import wave, subprocess, json, hashlib
import numpy as np
root=Path(__file__).resolve().parents[2]
out=root/'Assets/Resources/Audio/Combat/P0Kinetic';out.mkdir(parents=True,exist_ok=True)
audit=root/'AuditEvidence/p0-ballistics';audit.mkdir(parents=True,exist_ok=True)
sr=48000;rng=np.random.default_rng(90902);manifest=[]
def band(x,lo,hi):
 f=np.fft.rfftfreq(len(x),1/sr);h=(f/max(lo,1))**4
 return np.fft.irfft(np.fft.rfft(x)*h/(1+h)/(1+(f/hi)**6),n=len(x))
def noise(n,lo,hi):
 x=band(rng.normal(size=n),lo,hi);return x/max(np.sqrt(np.mean(x*x)),1e-6)
def recording(name,n):
 raw=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(root/'Assets/Resources/Audio/Combat'/name),'-f','f32le','-ac','1','-ar',str(sr),'-'],capture_output=True,check=True).stdout
 x=np.frombuffer(raw,np.float32).astype(float);indices=np.flatnonzero(np.abs(x)>.015)
 if len(indices):x=x[max(0,indices[0]-48):]
 y=np.zeros(n);y[:min(n,len(x))]=x[:n];return y
def save(name,x,peak):
 x=band(x,50,10000);n=len(x)
 x*=np.minimum(np.arange(n)/24,1)*np.minimum(np.arange(n,0,-1)/480,1)
 x=np.tanh(x*1.25);x*=peak/max(np.max(np.abs(x)),1e-6)
 path=out/(name+'.wav')
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(np.round(x*32767).astype('<i2').tobytes())
 manifest.append(dict(name=name,seconds=round(n/sr,3),peak_dbfs=round(20*np.log10(max(abs(x))),2),rms_dbfs=round(20*np.log10(np.sqrt(np.mean(x*x))),2),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
for i,dur in enumerate([.16,.13,.24]):
 t=np.arange(round(dur*sr))/sr;n=len(t);at=.018
 air=np.exp(-((t-at)/(.014 if i<2 else .023))**2)
 tail=np.exp(-np.maximum(0,t-at)*(48 if i<2 else 27))*(t>=at)
 x=.25*noise(n,260,3600)*air+.065*noise(n,1700,7600)*tail
 x+=.07*noise(n,75,500)*tail*(1.6 if i==2 else 1)
 save('cut_'+str(i+1),x,.72 if i<2 else .82)
for i in range(2):
 t=np.arange(round(.15*sr))/sr;n=len(t)
 crack=noise(n,1600,10000)*np.exp(-t*180)*.4
 body=noise(n,75,1000)*np.exp(-t*45)*.25
 transient=recording('explosionCrunch_000.ogg',n)*np.exp(-t*32)*.45
 bolt=recording('impactMetal_medium_000.ogg',n)*np.exp(-t*65)*.18
 save('rifle_shot_'+str(i+1),crack+body+transient+bolt,.79)
for i,heavy in enumerate([False,False,True]):
 dur=.27 if heavy else .16;t=np.arange(round(dur*sr))/sr;n=len(t)
 plate=recording('impactMetal_heavy_00'+str(i%2)+'.ogg',n)
 x=.7*plate*np.exp(-t*(13 if heavy else 24))
 x+=.12*noise(n,1000,7000)*np.exp(-t*130)
 x+=(.28 if heavy else .12)*noise(n,65,550)*np.exp(-t*(21 if heavy else 40))
 save('hit_heavy' if heavy else 'hit_'+str(i+1),x,.85 if heavy else .76)
t=np.arange(round(.13*sr))/sr;n=len(t)
save('heavy_load',noise(n,150,1600)*np.sin(np.pi*t/.13)**2*.10,.39)
(audit/'audio-manifest.json').write_text(json.dumps({'sample_rate':sr,'clips':manifest,'auditioned':False,'source':'Existing Kenney CC0 recordings plus authored broadband synthesis; no device playback.'},indent=2),encoding='utf8')
print(json.dumps(manifest,indent=2))
