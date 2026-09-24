from pathlib import Path
import re, subprocess, json, hashlib
import numpy as np
from scipy import signal
from scipy.io import wavfile

ROOT=Path(r'D:\project-mecha-design\MECH ROUGE\handoff\GameplayLoop_V1')
ROUND=Path(r'E:\SteamLibrary\JishenBuildWork\CombatVelocity_R1')
SOURCES=ROUND/'AudioSources'; OUT=ROOT/'Assets/Resources/Audio/Combat/VelocityR1'; OUT.mkdir(parents=True,exist_ok=True)
VGM=Path(r'E:\SteamLibrary\GameStudyExports\MechaRefinement_R04\ChatWorkspace\work\vgmstream\vgmstream-cli.exe')
FF=Path(r'D:\Tools\ffmpeg\bin\ffmpeg.exe'); SR=48000
entries={}
for record in (SOURCES/'all-streams.txt').read_text(encoding='utf-8-sig').split('metadata for ')[1:]:
    index=int(re.search(r'stream index: (\d+)',record)[1]); name=re.search(r'stream name: (.*)',record)
    if name:
        for n in name[1].strip().split('; '): entries.setdefault(n,[]).append(index)
manifest=[]
def peak(a): return float(np.max(np.abs(a)))+1e-9
def filt(a,f,kind='highpass',order=2): return signal.sosfilt(signal.butter(order,f,btype=kind,fs=SR,output='sos'),a).astype(np.float32)
def save(p,a):
    a=np.clip(a,-.97,.97); wavfile.write(p,SR,(a*32767).astype(np.int16))
def source(name,variant):
    ids=entries[name]; index=ids[variant%len(ids)]; p=SOURCES/f'{name}_{index}.wav'
    if not p.exists(): subprocess.run([str(VGM),'-i','-s',str(index),'-o',str(p),str(SOURCES/'mission.acb')],stdout=subprocess.DEVNULL,check=True)
    rate,a=wavfile.read(p); a=a.astype(np.float32)/32768
    if a.ndim==2:a=a.mean(axis=1)
    if rate!=SR:a=signal.resample_poly(a,SR,rate)
    return a,index,p
specs={
 'rifle':('atk000_beam_rifle',.235,False),
 'enemy':('atk006_beam_machinegun',.19,False),
 'beam':('atk003_beam_magnum',.45,True),
 'swing1':('atk014_beam_saber_slash',.20,False),
 'swing2':('atk064_beam_saber_slash02',.18,False),
 'swing3':('atk020_physical_saber_slash',.30,True),
 'hit':('dam000_beam_hit_weak',.16,False),
 'metal':('dam005_physical_bullet_hit_weak',.15,False),
 'heavy':('dam002_beam_hit_large',.30,True),
 'bladehit':('dam012_beam_slash_hit_weak01',.19,False),
 'bladeheavy':('dam016_beam_slash_hit_large01',.33,True),
 'dash':('mov118_boost_start',.28,True),
}
# Banks use several alternate labels; require a named, documented source, never a random stream.
for key,(name,dur,heavy) in list(specs.items()):
    if name not in entries:
        prefix=name.split('_')[0]+'_'; matches=[n for n in entries if n.startswith(prefix)]
        if not matches:raise ValueError((name,'missing named cue'))
        specs[key]=(matches[0],dur,heavy)
for key,(name,dur,heavy) in specs.items():
    for variant in range(3):
        raw,index,p=source(name,variant); raw=filt(raw,65)
        hot=np.flatnonzero(np.abs(raw)>peak(raw)*.015); raw=raw[max(0,int(hot[0])-100):] if len(hot) else raw
        ratio=[.95,1.015,1.07][variant]; raw=np.interp(np.arange(0,len(raw),ratio),np.arange(len(raw)),raw)
        n=int(SR*dur); body=np.zeros(n,dtype=np.float32); body[:min(n,len(raw))]=raw[:n]
        body=filt(body,11000,'lowpass'); body*=.60/peak(body)
        t=np.arange(n)/SR
        # Original electronic layer: damped FM ignition / chirp; no unchanged source copies.
        freq=950 if key.startswith('swing') else 1700
        phase=2*np.pi*(freq*.025*(1-np.exp(-t/.025))+140*t)
        electronic=np.sin(phase+1.8*np.sin(phase*.507))*np.exp(-t/.052)*(1-np.exp(-t/.0009))*.13
        # Controlled short sub-body, + harmonic for small speakers. Ordinary/large hits retain headroom.
        subfreq=75 if heavy else 92
        sub=np.sin(2*np.pi*(subfreq*t+35*.012*(1-np.exp(-t/.012))))
        sub=(sub+.18*np.sin(4*np.pi*subfreq*t))*np.exp(-t/(.048 if heavy else .028))
        sub*=np.minimum(1,t/.002)*(.30 if heavy else .19)
        if key.startswith('swing'):sub*=.45
        mixed=np.tanh((body+electronic+sub)*1.15)/1.15
        mixed*=np.minimum(1,t/.0007)*np.minimum(1,(dur-t)/.025)
        mixed*=.78/peak(mixed); dest=OUT/f'{key}_{variant+1}.wav';save(dest,mixed)
        manifest.append(dict(file=dest.name,source=name,stream=index,source_sha256=hashlib.sha256(p.read_bytes()).hexdigest(),sha256=hashlib.sha256(dest.read_bytes()).hexdigest(),seconds=dur,peak_db=20*np.log10(peak(mixed)),processing='onset trim, varied pitch, 65 Hz highpass, cabinet lowpass, original FM electronic layer, 75/92 Hz damped low-frequency layer, soft saturation, short release'))
(ROUND/'AudioSources/processed-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
print('Processed',len(manifest),'48 kHz PCM weapon / impact / propulsion variants',flush=True)

# Original local composition, rendered by a deterministic multi-track synthesizer and FFmpeg.
# No GB4/Hades music or melody is sampled. 64 bars @170 BPM; tail is wrapped to the loop start.
rng=np.random.default_rng(9242026); BPM=170; beat=60/BPM; duration=64*4*beat; N=round(duration*SR)
tracks={n:np.zeros((N,2),np.float32) for n in ('drums','guitars','bass','synth')}
cache={}
def note(m):return 440*2**((m-69)/12)
def env(t,d,attack=.003,release=.03):return np.minimum(1,t/attack)*np.minimum(1,np.maximum(0,d-t)/release)
def guitar(m,d,side):
    key=('g',m,round(d,4),side)
    if key in cache:return cache[key]
    t=np.arange(int(d*SR))/SR; freq=note(m)*(1+side*.0009)
    a=np.zeros_like(t)
    for semitone,gain in ((0,1),(7,.62),(12,.36)):
        f=freq*2**(semitone/12)
        for h in range(1,18):a+=gain*np.sin(2*np.pi*f*h*t+side*.09*h)*np.exp(-t*(2+h*.6))/(h**1.15)
    a*=np.exp(-t*7)*env(t,d,.0015,.026)
    a=np.tanh(a*5.8);a=filt(filt(a,100),5200,'lowpass');a*=.65/peak(a)
    cache[key]=a.astype(np.float32);return cache[key]
def bass(m,d):
    t=np.arange(int(d*SR))/SR;f=note(m)
    a=np.sin(2*np.pi*f*t)+.3*signal.sawtooth(2*np.pi*f*t)
    return filt(np.tanh(a*1.5),1800,'lowpass')*env(t,d,.003,.025)*.55
def lead(m,d):
    t=np.arange(int(d*SR))/SR;f=note(m)
    a=(signal.sawtooth(2*np.pi*f*t)+.45*signal.sawtooth(2*np.pi*f*1.004*t)+.2*np.sin(4*np.pi*f*t))
    a=filt(a,2600,'lowpass')*env(t,d,.009,.06)*np.exp(-t*.8)*.22
    return a
def drum(kind):
    if kind in cache:return cache[kind]
    d={'kick':.20,'snare':.20,'hat':.065,'open':.24,'crash':.8,'tom':.19}[kind];t=np.arange(int(d*SR))/SR;noise=rng.normal(size=len(t))
    if kind=='kick':
        a=np.sin(2*np.pi*(45*t+110*.02*(1-np.exp(-t/.02))))*np.exp(-t/ .065)+filt(noise,4800,'lowpass')*np.exp(-t/.005)*.38
    elif kind=='snare':a=filt(noise,1200)*np.exp(-t/.042)*.35+(np.sin(2*np.pi*185*t)+.4*np.sin(2*np.pi*320*t))*np.exp(-t/.032)*.45
    elif kind=='tom':a=np.sin(2*np.pi*(100*t+50*.03*(1-np.exp(-t/.03))))*np.exp(-t/.045)
    else:
        metallic=sum(np.sign(np.sin(2*np.pi*f*t)) for f in (3170,4289,5793,6877,8411,9917))/6
        a=filt(noise*.3+metallic,6200)*np.exp(-t/({'hat':.013,'open':.058,'crash':.22}[kind]))*.35
    a*=env(t,d,.0006,.008);cache[kind]=a.astype(np.float32);return cache[kind]
def put(track,a,b,gain=1,pan=0):
    start=round(b*beat*SR);idx=(start+np.arange(len(a)))%N
    gains=np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)])*gain
    tracks[track][idx]+=a[:,None]*gains
for bar in range(64):
    section=bar//16; pos=bar%16; base=bar*4
    root=[40,40,43,42,40,40,36,38][(bar//2)%8]
    pattern=[0,.5,.75,1.5,2,2.5,3,3.75] if bar%2==0 else [0,.25,1,1.5,2,2.75,3.25,3.5]
    if section==2:pattern=[0,.5,1.5,2,2.5,3,3.5,3.75]
    for j,b in enumerate(pattern):
        pitch=root+(7 if j==len(pattern)-1 and bar%4==3 else 0);d=beat*(.38 if b%1 else .48)
        for side in (-1,1):put('guitars',guitar(pitch,d,side),base+b+(0.021 if side==1 else 0),.80,side*.78)
        put('bass',bass(pitch-12,d*1.08),base+b,.66)
        put('drums',drum('kick'),base+b,.83)
    for b in (1,3):put('drums',drum('snare'),base+b,.9, -.04)
    for j in range(8):put('drums',drum('open' if j==7 and bar%2 else 'hat'),base+j*.5,.8 if j%2==0 else .55,.36)
    if bar%4==0:put('drums',drum('crash'),base,.8,-.4)
    if bar%8==7:
        for j in range(4):put('drums',drum('tom' if j%2 else 'snare'),base+3+j*.25,.45+j*.10,(j-1.5)*.2)
    # Rhythmic arpeggio reinforces drive; melodic motif is developed in B / finale.
    chord=[root+24,root+31,root+36,root+34]
    for j in range(8):put('synth',lead(chord[(j+bar)%4],beat*.32),base+j*.5,.32,(j%2*2-1)*.22)
    if section in (1,3):
        melody=[76,79,83,81,79,76,74,78,79,83,86,83,81,79,78,74]
        for j in range(2):
            m=melody[(pos*2+j)%len(melody)];a=lead(m,beat*.78)
            put('synth',a,base+j*2+.5,.82, -.12)
            put('synth',a,base+j*2+1.25,.18,.38)
musicdir=ROUND/'Music';musicdir.mkdir(exist_ok=True)
mix=sum(tracks.values());mix=filt(mix.T,30).T if False else mix
for ch in range(2):mix[:,ch]=filt(mix[:,ch],32)
mix=np.tanh(mix*.8);mix*=.75/peak(mix)
save(musicdir/'Overdrive_170_master.wav',mix)
for n,a in tracks.items():save(musicdir/(n+'.wav'),a*.7/peak(a))
musicout=ROOT/'Assets/Resources/Audio/Music/Velocity_Overdrive.ogg'
subprocess.run([str(FF),'-hide_banner','-loglevel','error','-y','-i',str(musicdir/'Overdrive_170_master.wav'),'-af','loudnorm=I=-17:TP=-2:LRA=7','-ar','48000','-c:a','libvorbis','-q:a','7',str(musicout)],check=True)
metadata=dict(title='OVERDRIVE / 推进临界',bpm=BPM,bars=64,seconds=duration,method='Original locally composed electronic-metal score: deterministic multi-track synthesis, distorted double-tracked power chords, bass, drums and synth; FFmpeg mastering. Online ElevenLabs/fal attempt rejected before generation due to provider balance.',seed=9242026,sha256=hashlib.sha256(musicout.read_bytes()).hexdigest())
(musicdir/'composition.json').write_text(json.dumps(metadata,indent=2,ensure_ascii=False),encoding='utf8')
print('Original 64-bar electronic-metal loop rendered:',round(duration,3),'seconds',flush=True)
