"""Author a 48 kHz Raiken sound bank. Run with Blender's bundled Python (NumPy).

New broadband air, low body, inharmonic metal and energy layers; short Kenney CC0
metal recordings supply physical contact texture. Original source clips are read only.
"""
import hashlib, json, pathlib, subprocess, wave
import numpy as np

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Resources/Audio/Combat/RaikenV7'
AUDIT = ROOT / 'AuditEvidence/power-combo-v7/audio'
SR = 48000
RNG = np.random.default_rng(7026)
manifest = []

def filtered(x, low=45, high=9000):
    f = np.fft.rfftfreq(len(x), 1 / SR)
    hp = (f / max(1, low)) ** 4
    gain = hp / (1 + hp) / (1 + (f / high) ** 6)
    return np.fft.irfft(np.fft.rfft(x) * gain, n=len(x))

def noise(n, low, high):
    x = filtered(RNG.normal(size=n), low, high)
    return x / max(.0001, np.sqrt(np.mean(x*x)))

def chirp(t, begin, end, rate, phase=0):
    # Exponential frequency glide with an analytic phase integral.
    return np.sin(2*np.pi*(end*t+(begin-end)/rate*(1-np.exp(-rate*t)))+phase)

def contact_recording(name, n, stretch=1.0):
    source=ROOT/'Assets/Resources/Audio/Combat'/name
    raw=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(source),'-f','f32le','-ac','1','-ar',str(SR),'-'],capture_output=True,check=True).stdout
    x=np.frombuffer(raw,np.float32).astype(np.float64)
    if stretch!=1:
        x=np.interp(np.arange(int(len(x)*stretch))/stretch,np.arange(len(x)),x)
    y=np.zeros(n); y[:min(n,len(x))]=x[:n]
    return filtered(y,200,7500)

def finish(name,x,width=.12,peak=.84):
    x=filtered(x,42,10000)
    x*=np.minimum(np.arange(len(x))/96,1)*np.minimum(np.arange(len(x),0,-1)/720,1)
    x=np.tanh(x*1.22)/1.22
    # Very short decorrelated side signal; all bass remains mono compatible.
    side=filtered(x,650,9500)
    side=(np.roll(side,173)-np.roll(side,269))*width
    side[:269]=0
    stereo=np.column_stack((x+side,x-side))
    stereo*=peak/max(.001,np.max(np.abs(stereo)))
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR)
        w.writeframes(np.round(stereo*32767).astype('<i2').tobytes())
    manifest.append(dict(name=name,duration=round(len(x)/SR,3),peak_dbfs=round(20*np.log10(np.max(np.abs(stereo))),2),rms_dbfs=round(20*np.log10(np.sqrt(np.mean(stereo*stereo))),2),sha256=hashlib.sha256((OUT/(name+'.wav')).read_bytes()).hexdigest()))
    return stereo

def swing(stage):
    dur=[.47,.51,.68][stage]; t=np.arange(int(SR*dur))/SR
    peak=[.10,.10,.12][stage]
    env=np.where(t<peak,np.exp(-((t-peak)/.065)**2),np.exp(-(t-peak)/[.082,.092,.13][stage]))
    # A dense moving air band, followed by a short tearing edge, without a laser chirp.
    low=noise(len(t),85,680);mid=noise(len(t),480,3600);high=noise(len(t),2200,8200)
    signal=(.23*low+.18*mid+.07*high)*env
    signal+=.14*chirp(t,215-stage*14,68-stage*5,14)*env
    signal+=.035*np.sin(2*np.pi*(880*t+38*np.sin(2*np.pi*17*t)))*env*np.exp(-t*4)
    onset=np.maximum(t-peak,0)
    tear=(t>=peak)*(1-np.exp(-onset*1500))*np.exp(-onset*29)
    signal+=noise(len(t),800,6200)*tear*(.095+stage*.025)
    if stage==2:
        signal+=.11*chirp(t,130,55,18)*np.exp(-np.maximum(t-.12,0)*11)*np.minimum(t/.10,1)
    return finish(['cut_reverse','cut_return','cut_heavy'][stage],signal,.13+stage*.035,.82 if stage<2 else .88)

def impact(index,heavy=False):
    dur=.90 if heavy else .59;t=np.arange(int(SR*dur))/SR
    body=chirp(t,170 if heavy else 215,58 if heavy else 83,31)*np.exp(-t*(13 if heavy else 21))
    transient=noise(len(t),950,8500)*np.exp(-t*105)
    crush=noise(len(t),220,2900)*np.exp(-t*(19 if heavy else 28))
    metal=np.zeros(len(t))
    freqs=[273,467,821,1319,2263,3597] if heavy else [341,583,987,1583,2731,4129]
    for j,f in enumerate(freqs):
        metal+=np.sin(2*np.pi*f*(1+index*.018)*t+RNG.random()*6.28)*np.exp(-t*(9+j*5))/(j+2)
    plate=contact_recording('impactMetal_heavy_00'+str(index%2)+'.ogg',len(t),1.45 if heavy else 1.10)
    signal=.45*body+.22*transient+.16*crush+.10*metal+.32*plate
    signal+=(.045 if heavy else .025)*noise(len(t),1700,7200)*np.exp(-t*9)*(1-np.exp(-t*90))
    # Restrained early room reflections: mass without a long wash covering the next cut.
    direct=signal.copy()
    for delay,gain in [(0.031,.16),(.057,.08),(.083,.035)]:
        shift=int(SR*delay);signal[shift:]+=direct[:-shift]*gain
    return finish('armor_break_heavy' if heavy else 'armor_cut_0'+str(index+1),signal,.085,.88)

def mechanisms():
    t=np.arange(int(SR*.20))/SR
    env=np.sin(np.pi*np.minimum(t/.2,1))**1.2
    servo=(.07*chirp(t,95,165,9)+.04*noise(len(t),180,1400))*env
    servo+=.022*np.sin(2*np.pi*610*t)*np.exp(-t*25)
    load=finish('servo_load',servo,.045,.57)
    t=np.arange(int(SR*.17))/SR; x=np.zeros(len(t))
    for delay,gain in [(0,.10),(.049,.18)]:
        p=np.maximum(0,t-delay);e=(t>=delay)*np.exp(-p*65)
        x+=gain*(noise(len(t),850,5900)+.55*np.sin(2*np.pi*390*p))*e
    lock=finish('grip_lock',x,.04,.63)
    return load,lock

def main():
    OUT.mkdir(parents=True,exist_ok=True);AUDIT.mkdir(parents=True,exist_ok=True)
    load,lock=mechanisms();cuts=[swing(i) for i in range(3)]
    hits=[impact(0),impact(1),impact(0,True)]
    audition=np.zeros((SR*7,2))
    for i in range(3):
        base=i*2.25
        for at,clip,gain in [(base,load,.26),(base+.14,cuts[i],.63),(base+.27,hits[i],.62)]:
            n=int(at*SR);audition[n:n+len(clip)]+=clip*gain
    with wave.open(str(AUDIT/'sound_bank_preview.wav'),'wb') as w:
        w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR)
        w.writeframes(np.round(np.clip(audition,-.97,.97)*32767).astype('<i2').tobytes())
    (AUDIT/'bank-manifest.json').write_text(json.dumps(dict(sample_rate=SR,clips=manifest,method='Project-authored synthesis and physical Kenney CC0 metal contact layers; final Player mix is separately captured.'),indent=2),encoding='utf-8')
    print(json.dumps(manifest,indent=2))

if __name__=='__main__':main()
