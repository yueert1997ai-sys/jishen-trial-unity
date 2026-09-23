"""P0.3 offline audio authoring. No audio device is opened.

References are analyzed only, never sampled into delivered assets.
Rifle basis: mnslugger20, Freesound 259758, CC0.
Music: Heavy Battle 2, MintoDog, CC0. See docs/licenses/P0_Audio_V3.md.
"""
from pathlib import Path
import hashlib,json,subprocess,wave,uuid
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
AUDIT=ROOT/'AuditEvidence/p0-audio-v3'
OUT=ROOT/'Assets/Resources/Audio/Combat/P0EnergyKinetic'
OUT.mkdir(parents=True,exist_ok=True)
SR=48000
FF='D:/Tools/ffmpeg/bin/ffmpeg.exe'
rng=np.random.default_rng(9303)
manifest=[]

def decode(path,channels=1):
    p=subprocess.run([FF,'-v','error','-i',str(path),'-f','f32le','-ac',str(channels),'-ar',str(SR),'-'],capture_output=True,check=True)
    x=np.frombuffer(p.stdout,np.float32).astype(float)
    return x if channels==1 else x.reshape(-1,channels)

def band(x,lo,hi):
    f=np.fft.rfftfreq(len(x),1/SR)
    h=1/(1+(lo/np.maximum(f,1))**6)/(1+(f/hi)**8)
    return np.fft.irfft(np.fft.rfft(x)*h,n=len(x))

def noise(n,lo,hi):
    x=band(rng.normal(size=n),lo,hi)
    return x/max(np.sqrt(np.mean(x*x)),1e-9)

def phase(freq):
    return 2*np.pi*np.cumsum(freq)/SR

def fade(x,attack=.001,release=.014):
    t=np.arange(len(x))/SR
    return x*np.minimum(t/attack,1)*np.minimum((len(x)/SR-t)/release,1)

def write_meta(path,music=False):
    target=Path(str(path)+'.meta')
    if target.exists():return
    template=(ROOT/'Assets/Resources/Audio/Combat/P0Kinetic/cut_1.wav.meta').read_text()
    import re
    template=re.sub(r'guid: \w+', 'guid: '+uuid.uuid4().hex,template)
    template=template.replace('normalize: 1','normalize: 0')
    # PCM for transients; streaming Vorbis for long-form music. Disable hidden re-normalization.
    template=template.replace('compressionFormat: 1','compressionFormat: '+('1' if music else '0'))
    if music:
        template=template.replace('loadType: 0','loadType: 2').replace('forceToMono: 1','forceToMono: 0').replace('preloadAudioData: 1','preloadAudioData: 0').replace('loadInBackground: 0','loadInBackground: 1')
    target.write_text(template)

def save(name,x,peak):
    x=fade(band(x,45,8500));x-=np.mean(x)
    x*=peak/max(abs(x))
    x=fade(x,.0003,.006)
    path=OUT/(name+'.wav')
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR)
        w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    write_meta(path)
    manifest.append(dict(name=name,seconds=len(x)/SR,peak_dbfs=float(20*np.log10(max(abs(x)))),rms_dbfs=float(20*np.log10(np.sqrt(np.mean(x*x)))),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    return x

# Electrical beam sweep: moving resonant harmonics, rough modulation and a short plasma tail.
# The existing combo timing triggers this 20 ms before the contact window.
for i,dur in enumerate([.22,.19,.38]):
    t=np.arange(round(dur*SR))/SR;u=t/dur;n=len(t)
    center=[.038,.029,.057][i]
    freq=720+2200*np.exp(-((t-center)/(.052 if i<2 else .085))**2)
    if i==1:freq=850+1900*np.exp(-((t-center)/.04)**2)
    if i==2:freq*=.78
    carrier=phase(freq);mod=phase(freq*.491)
    electric=np.sin(carrier+1.6*np.sin(mod))+.32*np.sin(carrier*2.013+.4*np.sin(2*np.pi*71*t))
    electric*=.8+.2*np.sin(2*np.pi*93*t)
    env=np.minimum(t/.012,1)*np.exp(-np.maximum(0,t-center)/(.044 if i<2 else .09))
    arc=noise(n,1400,6500)*np.exp(-((t-center)/(.025 if i<2 else .042))**2)
    body=np.sin(phase(230+430*np.exp(-t*22)))*np.exp(-t*18)
    save('cut_'+str(i+1),(.55*electric+.23*arc+.07*body)*env,.76 if i<2 else .84)

# Rising electrical tension only during the heavy windup, no persistent hum or grip clanks.
t=np.arange(round(.16*SR))/SR
save('heavy_load',np.sin(phase(520+700*(t/.16)**1.7)+1.2*np.sin(2*np.pi*145*t))*np.sin(np.pi*t/.16)**1.4,.38)

# Plasma-on-armor contact: electrical tear above a short physical impact.
for i,heavy in enumerate([False,False,True]):
    dur=.30 if heavy else .18;t=np.arange(round(dur*SR))/SR;n=len(t)
    raw=decode(ROOT/('Assets/Resources/Audio/Combat/impactMetal_heavy_00'+str(i%2)+'.ogg'))[:n]
    raw=np.pad(raw,(0,max(0,n-len(raw))))
    tear=np.sin(phase(1800+2100*np.exp(-t*35))+2.2*np.sin(phase(430+100*np.exp(-t*20))))
    x=.35*raw*np.exp(-t*20)+.26*tear*np.exp(-t*(21 if heavy else 34))
    x+=.13*noise(n,1400,6500)*np.exp(-t*42)+(.18 if heavy else .07)*noise(n,65,300)*np.exp(-t*25)
    save('hit_heavy' if heavy else 'hit_'+str(i+1),x,.82 if heavy else .73)

# Isolate the first transient after each burst gap; keep less than one shot interval.
recorded=decode(AUDIT/'sources/M4_recorded.mp3')
hop=240
envelope=np.sqrt(np.mean(recorded[:len(recorded)//hop*hop].reshape(-1,hop)**2,axis=1))
indices=np.where((envelope[1:-1]>envelope[:-2])&(envelope[1:-1]>=envelope[2:])&(envelope[1:-1]>.35*max(envelope)))[0]+1
peaks=[]
for i in sorted(indices,key=lambda v:-envelope[v]):
    if all(abs(i-j)*hop/SR>.07 for j in peaks):peaks.append(i)
peaks=sorted(peaks)
first=[i for k,i in enumerate(peaks) if k==0 or (i-peaks[k-1])*hop/SR>.32]
assert len(first)>=3,'Need three distinct recorded burst onsets'
shots=[]
for j in range(3):
    peak=first[j]*hop
    search_start=max(0,peak-round(.028*SR))
    start=search_start
    # Walk back to the quiet leading edge of the transient, within its shot.
    local=np.abs(recorded[search_start:peak+hop])
    crossing=np.flatnonzero(local>max(local)*.12)
    if len(crossing):start=search_start+max(0,crossing[0]-24)
    n=round(.28*SR);t=np.arange(n)/SR
    dry=recorded[start:start+round(.072*SR)].copy()
    dry=fade(dry,.0003,.009)
    x=np.interp(np.arange(n)*(1+(j-1)*.012),np.arange(len(dry)),dry,right=0)
    # Preserve recorded crack; low-mid body is deliberately stronger than the previous noise burst.
    low=band(x,50,250);mid=band(x,250,1400);high=band(x,1400,8000)
    x=1.7*low+mid+.9*high
    # Diffuse the same recorded transient into a quiet tail, never replay a second full crack.
    impulse=noise(round(.17*SR),140,4200)*np.exp(-np.arange(round(.17*SR))/SR*33)
    impulse[:round(.007*SR)]=0
    tail=np.convolve(x[:round(.072*SR)],impulse)[:n]
    tail=np.pad(tail,(0,n-len(tail)))
    x+=.085*max(abs(x))*tail/max(abs(tail))
    x*=np.exp(-np.maximum(0,t-.035)*16)
    bolt=decode(ROOT/'Assets/Resources/Audio/Combat/impactMetal_medium_000.ogg')[:round(.065*SR)]
    offset=round(.037*SR);bolt=band(bolt,800,4600)*np.exp(-np.arange(len(bolt))/SR*50)
    x[offset:offset+len(bolt)]+=.025*bolt
    shots.append(save('rifle_shot_'+str(j+1),x,.82))

# Keep the composer's complete, beat-aligned 64-bar loop. No arbitrary 2.5-second crossfade.
music=ROOT/'Assets/Resources/Audio/Music/P0_HeavyBattle.ogg'
music_length=len(decode(AUDIT/'sources/Heavy_Battle_2.ogg'))/SR
filters=f'highpass=f=38,equalizer=f=2400:t=q:w=0.8:g=-2,loudnorm=I=-18:TP=-2:LRA=9,afade=t=in:d=0.004,afade=t=out:st={music_length-.008}:d=0.008'
subprocess.run([FF,'-v','error','-y','-i',str(AUDIT/'sources/Heavy_Battle_2.ogg'),'-af',filters,'-ar','48000','-c:a','libvorbis','-q:a','6',str(music)],check=True)
write_meta(music,True)
manifest.append(dict(name='P0_HeavyBattle',sha256=hashlib.sha256(music.read_bytes()).hexdigest(),source='Heavy Battle 2 by MintoDog, CC0',processing='38 Hz highpass, -2 dB presence reduction, -18 LUFS normalization'))
(AUDIT/'audio-manifest.json').write_text(json.dumps(dict(clips=manifest,recorded_shot_times=[i*hop/SR for i in first[:3]],acoustic_audition=False,reference_samples_in_build=False),indent=2),encoding='utf8')
print(json.dumps(manifest,indent=2))
