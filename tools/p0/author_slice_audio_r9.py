"""R9 offline sound authoring: CC0 recordings + original synthesis, no device playback.
Keep reference and old banks intact. All outputs have PCM imports without normalization.
"""
from pathlib import Path
import json, hashlib, subprocess, wave, re, uuid
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/Resources/Audio/Combat'
OUT=BASE/'R9'; OUT.mkdir(exist_ok=True)
AUDIT=ROOT/'AuditEvidence/combat-slice-r9'; AUDIT.mkdir(exist_ok=True)
SR=48000; rng=np.random.default_rng(91609); records=[]

def decode(p):
    raw=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(p),'-f','f32le','-ac','1','-ar',str(SR),'-'],capture_output=True,check=True).stdout
    return np.frombuffer(raw,np.float32).astype(float)

def band(x,lo=55,hi=9500):
    return sosfilt(butter(2,[lo,hi],btype='bandpass',fs=SR,output='sos'),x)

def unit(x): return x/max(np.max(abs(x)),1e-9)
def rms(x): return np.sqrt(np.mean(x*x))
def time(d): return np.arange(round(d*SR))/SR
def phase(f): return 2*np.pi*np.cumsum(f)/SR
def noise(n,lo,hi):
    x=band(rng.normal(size=n),lo,hi);return x/max(rms(x),1e-9)

def fades(x,a=.0006,b=.012):
    t=np.arange(len(x))/SR
    return x*np.clip(t/a,0,1)*np.clip((len(x)/SR-t)/b,0,1)

cache={}
def material(name,d,speed=1):
    if name not in cache:
        x=decode(BASE/name);idx=np.flatnonzero(abs(x)>max(abs(x))*.025)
        cache[name]=unit(x[max(0,idx[0]-24):])
    x=cache[name];return np.interp(np.arange(round(d*SR))*speed,np.arange(len(x)),x,right=0)

def place(x,layer,at=0,gain=1):
    o=round(at*SR);n=min(len(layer),len(x)-o)
    if n>0:x[o:o+n]+=layer[:n]*gain
    return x

def metal(d,i=0,speed=1):
    return material(['impactMetal_heavy_000.ogg','impactMetal_heavy_001.ogg','impactMetal_medium_000.ogg'][i%3],d,speed)

def save(name,x,peak=.68,loop=False):
    x=band(x,42,10200)
    if loop:
        # Cyclic FFT filtering avoids startup state on the loop boundary.
        f=np.fft.rfftfreq(len(x),1/SR);h=1/(1+(45/np.maximum(f,1))**4)/(1+(f/8000)**4)
        x=np.fft.irfft(np.fft.rfft(x)*h,n=len(x));x-=np.mean(x)
        # Last 20 ms joins the actual start; no zero-volume notch in a sustained engine.
        n=960;u=np.linspace(0,1,n);x[-n:]=x[-n:]*(1-u)+np.interp(np.linspace(0,n-1,n),np.arange(n),x[:n])*u
        # Make the end meet sample zero, using a short slope-preserving correction.
        x[-96:]-=np.linspace(0,x[-1]-x[0],96)
    else:x=fades(x)
    x*=peak/max(abs(x));assert np.isfinite(x).all() and max(abs(x))<.9
    p=OUT/(name+'.wav')
    with wave.open(str(p),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    meta=Path(str(p)+'.meta')
    if not meta.exists():
        text=(BASE/'P0Cadence/m7_attack.wav.meta').read_text()
        text=re.sub(r'guid: \w+','guid: '+uuid.uuid4().hex,text).replace('compressionFormat: 1','compressionFormat: 0')
        meta.write_text(text,encoding='utf-8')
    energy=np.cumsum(x*x);cross=lambda q:float(np.searchsorted(energy,energy[-1]*q)/SR)
    records.append(dict(name=name,seconds=len(x)/SR,peak_dbfs=20*np.log10(max(abs(x))),rms_dbfs=20*np.log10(rms(x)),energy_10_ms=cross(.1)*1000,energy_90_ms=cross(.9)*1000,loop=loop,boundary_delta=float(abs(x[-1]-x[0])),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
    return x

# Gun: only first shot of each recorded burst, shorter than its source fire interval.
# Every output is one firing transient plus a deliberately quiet, delayed bolt mechanism.
raw=decode(ROOT/'AuditEvidence/p0-audio-v3/sources/M4_recorded.mp3')
hop=240;env=np.sqrt(np.mean(raw[:len(raw)//hop*hop].reshape(-1,hop)**2,axis=1))
idx=np.where((env[1:-1]>env[:-2])&(env[1:-1]>=env[2:])&(env[1:-1]>.35*max(env)))[0]+1
peaks=[]
for i in sorted(idx,key=lambda v:-env[v]):
    if all(abs(i-j)*hop/SR>.07 for j in peaks):peaks.append(i)
peaks.sort();first=[i for k,i in enumerate(peaks) if k==0 or (i-peaks[k-1])*hop/SR>.32]
assert len(first)>=3
shots=[]
for i in range(3):
    p=first[i]*hop;s=max(0,p-round(.028*SR));v=abs(raw[s:p+hop]);s+=max(0,np.flatnonzero(v>max(v)*.12)[0]-24)
    dry=fades(raw[s:s+round(.072*SR)],.0003,.010)
    x=np.zeros(round(.115*SR));place(x,dry)
    x=1.5*band(x,65,350)+band(x,350,1700)+.72*band(x,1700,8000)
    place(x,band(metal(.06,2,1.23+i*.025),650,5200),.048,.065)
    shots.append(save('m7_attack'+('' if i==0 else '_'+str(i+1)),x,.73))
t=time(.27);impulse=noise(len(t),180,3900)*np.exp(-t*23);impulse[:480]=0
x=fftconvolve(shots[0][:3456],impulse)[:len(t)];x=unit(x)*np.clip(t/.045,0,1)**2*np.exp(-t*8)
save('m7_tail',x,.16)

# Blade passes: air displacement and a short electromechanical edge; no collision baked in.
for i,(d,center,width,f0,f1) in enumerate([(.23,.027,.026,590,190),(.205,.020,.020,430,260),(.34,.037,.039,350,105)]):
    t=time(d);air=np.exp(-((t-center)/width)**2);decay=np.exp(-np.maximum(t-center,0)*(26 if i<2 else 17))
    motor=np.sin(phase(f1+(f0-f1)*np.exp(-t*20))+.35*np.sin(2*np.pi*93*t))
    x=.14*motor*decay+.27*noise(len(t),240,2400)*air+.025*noise(len(t),2600,6200)*decay
    x+=.08*material('forceField_001.ogg',d,1.3-i*.16)*decay
    save(['saber_swing_1','saber_swing_2','saber_heavy'][i],x,.57 if i<2 else .68)
t=time(.145)
save('saber_load',.15*metal(.145,2,.68)*np.exp(-t*34)+.11*np.sin(phase(180+480*t/.145))*np.sin(np.pi*t/.145)**2,.30)

# Material events: bullet ticks, blade gouges, chassis damage and debris are different designs.
for i in range(3):
    t=time(.145)
    x=.30*np.tanh(3*metal(.145,i,1.15+i*.08))*np.exp(-t*13)+.055*noise(len(t),1900,7300)*np.exp(-t*135)
    x+=.075*(np.sin(2*np.pi*(1310+i*137)*t)+.35*np.sin(2*np.pi*(2231+i*99)*t))*np.exp(-t*48)
    save('armor_tick_'+str(i+1),x,.57)
    t=time(.24)
    x=.62*metal(.24,i,.77+i*.03)*np.exp(-t*8)
    x+=.07*noise(len(t),450,3100)*np.exp(-t*21)*(.65+.35*np.sin(2*np.pi*87*t))
    x+=.13*np.sin(phase(95+90*np.exp(-t*35)))*np.exp(-t*32)
    save('saber_hit'+('' if i==0 else '_'+str(i+1)),x,.73)
    t=time(.19);x=.6*metal(.19,i,.73)*np.exp(-t*10)+.08*noise(len(t),100,750)*np.exp(-t*30)
    save('foot_'+str(i+1),x,.43)
t=time(.33)
save('saber_hit_heavy',.75*metal(.33,1,.61)*np.exp(-t*6)+.11*noise(len(t),450,2600)*np.exp(-t*16)+.3*np.sin(phase(70+90*np.exp(-t*24)))*np.exp(-t*22),.81)
for i in range(2):
    t=time(.30)
    x=.60*band(metal(.30,i,.64),70,1900)+.21*np.sin(phase(80+65*np.exp(-t*40)))*np.exp(-t*24)
    place(x,metal(.12,2,.88),.062,.10)
    save('player_hit_'+str(i+1),x,.72)

# Armor break is an immediate dry fracture then falling electrical pressure, distinct from kill.
t=time(.35);x=.30*np.tanh(4*metal(.35,1,1.25))*np.exp(-t*12)+.07*noise(len(t),1800,7200)*np.exp(-t*115)
x+=.32*np.sin(phase(580+1650*np.exp(-t*31))+.8*np.sin(2*np.pi*170*t))*np.exp(-t*16)
place(x,np.tanh(3*metal(.2,0,.9)),.029,.13);save('armor_break',x,.82)
t=time(.46);x=.7*metal(.46,1,.56)*np.exp(-t*6)+.14*noise(len(t),800,5100)*np.exp(-t*50)
place(x,np.sin(phase(62+70*np.exp(-t*26)))*np.exp(-t*17),.018,.36)
place(x,metal(.19,2,.8),.12,.17);save('armor_finish',x,.86)
for i in range(2):
    t=time(.46);x=.55*metal(.46,i,.62)*np.exp(-t*6)+.13*material('explosionCrunch_000.ogg',.46,.8)*np.exp(-t*9)
    place(x,metal(.20,2,1.3),.11,.19);place(x,metal(.15,0,1.7),.21,.1)
    save('death_'+str(i+1),x,.68)

# Propulsion: separate valve opening, turbine load, exhaust cut-off, and quick boost punch.
t=time(2.0)
engine=material('spaceEngineLow_000.ogg',2,.86)
engine=.24*band(engine,60,850)+.03*noise(len(t),100,1850)
engine+=.10*np.sin(2*np.pi*96*t)+.038*np.sin(2*np.pi*192*t+.25*np.sin(2*np.pi*3*t))
save('boost_loop',engine,.46,loop=True)
for name,d,k in [('boost_start',.15,1),('dash',.21,1.4),('boost_stop',.18,.7)]:
    t=time(d);x=.3*material('thrusterFire_000.ogg',d,1.65)*np.exp(-t*13)
    x+=.19*noise(len(t),65,1550)*np.exp(-t*27)*k+.07*noise(len(t),1700,5000)*np.exp(-t*21)
    if name=='boost_stop':x=band(x,400,4300)*np.exp(-t*8)
    save(name,x,.65 if name=='dash' else .42)

# Short mechanical transaction: catch, double lock, charge ready. No generic notification jingle.
for name,d,speed in [('weapon_draw',.085,1.5),('weapon_stow',.095,.86),('saber_regrip',.105,1.16),('salvage_catch',.13,.73),('salvage_lock',.17,.88),('salvage_cancel',.105,1.1)]:
    t=time(d);x=.5*metal(d,2,speed)*np.exp(-t*18)
    if name=='salvage_lock':place(x,metal(.08,0,1.4),.062,.34)
    if name=='salvage_catch':x+=.16*np.sin(2*np.pi*165*t)*np.exp(-t*32)
    save(name,x,.55 if name.startswith('salvage') else .35)
t=time(.50)
x=.05*material('spaceEngineLow_000.ogg',.50,1.25)+.13*np.sin(phase(160+370*(t/.5)**1.3))
x*=np.sin(np.pi*np.clip(t/.5,0,1))**.7;save('salvage_pull',x,.40)
t=time(.24);x=.12*np.sin(phase(290+650*(1-np.exp(-t*22))))*np.exp(-t*17)+.08*metal(.24,2,1.4)*np.exp(-t*26)
save('salvage_ready',x,.45)

# Hostile fire has less bass and a different signature; danger cues stay short and sparse.
for i in range(2):
    t=time(.16);x=.45*material('laserLarge_001.ogg',.16,1.5+i*.16)*np.exp(-t*24)
    x+=.13*metal(.16,2,1.3)*np.exp(-t*36)
    save('enemy_shot_'+str(i+1),band(x,370,5400),.52)
t=time(.16);x=np.zeros(len(t))
for at,f in [(0,1740),(.078,2320)]:
    v=time(.04);place(x,np.sin(2*np.pi*f*v)*np.sin(np.pi*v/.04)**2,at,.25)
save('warning',x,.46)
for i in range(2):
    t=time(.13);save('wall_'+str(i+1),band(metal(.13,i,1.8),1200,8000)*np.exp(-t*23),.42)
for name,freqs,d in [('wave',[480,640],.19),('reward',[540,810],.20),('victory',[260,390,520],.58),('defeat',[420,290,180],.52)]:
    t=time(d);x=np.zeros(len(t))
    for i,f in enumerate(freqs):
        v=time(d/len(freqs)*.85);place(x,.10*np.sin(2*np.pi*f*v)*np.sin(np.pi*v/(len(v)/SR))**2,i*d/len(freqs))
    place(x,metal(.11,2,.9),0,.08);save(name,x,.40)
# Other existing equipped weapons retain their own event; new timbres share the same mix language.
for name,src,speed in [('beam','laserLarge_002.ogg',.8),('missile','thrusterFire_002.ogg',1.15)]:
    t=time(.26);save(name,material(src,.26,speed)*np.exp(-t*12)+.13*metal(.26,0,.8)*np.exp(-t*25),.61)

old=[]
for p in sorted((BASE/'P0Cadence').glob('*.wav')):
    x=decode(p);e=np.cumsum(x*x)
    old.append(dict(name=p.stem,seconds=len(x)/SR,energy90_ms=float(np.searchsorted(e,e[-1]*.9)/SR*1000),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
(AUDIT/'audio-bank.json').write_text(json.dumps(dict(sample_rate=SR,clips=records,old_bank=old,source='CC0 mnslugger20 M4 recording + Kenney impact/sci-fi + original synthesis',auditioned=False),indent=2),encoding='utf-8')
print('Authored',len(records),'PCM clips; all peaks < -0.9 dBFS; old banks untouched')
