"""Author layered metal-impact PCM for enemy bullet hits: sharp transient + inharmonic ring + body thud.
Pure synthesis from filtered noise and sine partials; no third-party recordings.
Files land in Assets/Resources/Audio/Combat/ImpactR2 and a manifest in AuditEvidence/metal-impact-r1."""
from pathlib import Path
import wave, hashlib, json, datetime
import numpy as np
from scipy.signal import butter, sosfilt
root = Path(__file__).resolve().parents[2]
out = root / 'Assets/Resources/Audio/Combat/ImpactR2'
out.mkdir(parents=True, exist_ok=True)
sr = 48000
rng = np.random.default_rng(772019)
records = []

def band(x, lo, hi):
    return sosfilt(butter(2, [lo, hi], btype='bandpass', fs=sr, output='sos'), x)

def noise(n, lo, hi):
    x = band(rng.normal(size=n), lo, hi)
    return x / max(np.sqrt(np.mean(x * x)), 1e-8)

def save(name, x, peak):
    t = np.arange(len(x)) / sr
    x = band(x, 42, 10500)
    x *= np.minimum(t / .0003, 1) * np.minimum((len(x) / sr - t) / .012, 1)
    x = x / max(abs(x)) * peak
    p = out / f'{name}.wav'
    with wave.open(str(p), 'wb') as w:
        w.setparams((1, 2, sr, 0, 'NONE', 'not compressed'))
        w.writeframes(np.round(x * 32767).astype('<i2').tobytes())
    records.append({'name': name, 'seconds': round(len(x) / sr, 4), 'peak': float(peak),
                    'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})

def clang(t, partials, decay, gain):
    # Inharmonic metal partials with slight pairwise beating.
    x = np.zeros(len(t))
    for i, f in enumerate(partials):
        x += np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * np.exp(-t * decay * (1 + .16 * i))
        x += .35 * np.sin(2 * np.pi * f * 1.013 * t) * np.exp(-t * decay * (1 + .2 * i))
    return x / max(np.sqrt(np.mean(x * x)), 1e-8) * gain

for v in range(3):
    # Light: bullet on plate ~0.15 s. Snap transient, short ring, small thud.
    n = round(.15 * sr); t = np.arange(n) / sr
    transient = noise(n, 1700, 7800) * np.exp(-t * 340) * .52
    ring = clang(t, [1520 + v * 130, 2410 + v * 90, 3350], 46, .40) * np.exp(-t * 23)
    thud = (noise(n, 90, 340) * np.exp(-t * 72) + .8 * np.sin(2 * np.pi * (132 + v * 8) * t) * np.exp(-t * 58)) * .17
    save(f'metal_hit_light_{v+1}', transient + ring + thud, .58)

for v in range(2):
    # Heavy: cannon-round connect ~0.26 s. Deeper thud, longer ring, brighter snap.
    n = round(.26 * sr); t = np.arange(n) / sr
    transient = noise(n, 1200, 8200) * np.exp(-t * 300) * .5
    ring = clang(t, [880 + v * 60, 1660 + v * 110, 2680, 3890], 26, .36) * np.exp(-t * 15)
    thud = (noise(n, 70, 300) * np.exp(-t * 46) + np.sin(2 * np.pi * (88 + v * 6) * t) * np.exp(-t * 34)) * .34
    save(f'metal_hit_heavy_{v+1}', transient + ring + thud, .72)

audit = root / 'AuditEvidence/metal-impact-r1'
audit.mkdir(parents=True, exist_ok=True)
(audit / 'audio-manifest.json').write_text(json.dumps(
    {'authored': datetime.datetime.now().isoformat(timespec='seconds'), 'sampleRate': sr,
     'design': 'transient(band noise)+inharmonic ring(sine partials, beating)+thud(low band+sine), pure synthesis',
     'clips': records}, indent=2), encoding='utf-8')
print('Authored', len(records), 'PCM clips into', out)
