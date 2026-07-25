"""
Generate original, procedural Black Knight sound effects (no external assets).

Outputs 16-bit mono WAVs at 44.1 kHz to NinjaMod/audio/:
  bk_swing.wav     - axe whoosh (slash / horizontal windup)
  bk_hit.wav       - heavy blade impact (slash / horizontal hit)
  bk_cleave.wav    - massive downward slam (vertical slash+)
  bk_curse.wav     - deep, muffled ghostly incantation (curse cast)
  bk_trueform.wav  - rising power-up roar (empower - true form)
  bk_armor.wav     - metallic armour clank (dark armor)
  bk_hurt.wav      - short low grunt through a helmet (hurt)

Overall tone: low, knightly, muffled "through-a-visor" voice + weighty metal.
Re-runnable. Pure numpy; writes WAV headers manually (no extra deps).
"""
from __future__ import annotations

import os
import struct
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "NinjaMod", "audio")
SR = 44100
rng = np.random.default_rng(1207)


def t(dur):
    return np.linspace(0, dur, int(SR * dur), endpoint=False)


def env(n, a=0.01, d=0.2, s=0.6, r=0.3, sus=0.5):
    """Simple ADSR over n samples (times are fractions of total)."""
    a_n = max(1, int(n * a)); d_n = max(1, int(n * d)); r_n = max(1, int(n * r))
    s_n = max(1, n - a_n - d_n - r_n)
    e = np.concatenate([
        np.linspace(0, 1, a_n),
        np.linspace(1, sus, d_n),
        np.full(s_n, sus),
        np.linspace(sus, 0, r_n),
    ])
    return e[:n] if len(e) >= n else np.pad(e, (0, n - len(e)))


def lowpass(sig, cutoff):
    f = np.fft.rfft(sig)
    freqs = np.fft.rfftfreq(len(sig), 1 / SR)
    roll = 1 / (1 + (freqs / cutoff) ** 4)  # gentle 4th-order-ish
    return np.fft.irfft(f * roll, n=len(sig))


def bandpass(sig, lo, hi):
    f = np.fft.rfft(sig)
    freqs = np.fft.rfftfreq(len(sig), 1 / SR)
    mask = (freqs > lo) & (freqs < hi)
    smooth = np.convolve(mask.astype(float), np.ones(9) / 9, mode="same")
    return np.fft.irfft(f * smooth, n=len(sig))


def norm(sig, peak=0.9):
    m = np.max(np.abs(sig)) or 1.0
    return sig / m * peak


def reverb_tail(sig, delays_ms=(37, 53, 71, 97), decay=0.5):
    out = sig.copy()
    for dm in delays_ms:
        d = int(SR * dm / 1000)
        if d < len(out):
            echo = np.zeros_like(out)
            echo[d:] = out[:-d] * decay
            out = out + echo
    return out


def save(name, sig):
    os.makedirs(OUT_DIR, exist_ok=True)
    sig = norm(sig, 0.92)
    data = (sig * 32767).astype("<i2").tobytes()
    path = os.path.join(OUT_DIR, name)
    with open(path, "wb") as f:
        f.write(b"RIFF")
        f.write(struct.pack("<I", 36 + len(data)))
        f.write(b"WAVEfmt ")
        f.write(struct.pack("<IHHIIHH", 16, 1, 1, SR, SR * 2, 2, 16))
        f.write(b"data")
        f.write(struct.pack("<I", len(data)))
        f.write(data)
    print(f"  {name}  ({len(sig)/SR:.2f}s)")


def swing():
    n = int(SR * 0.34); x = t(0.34)
    noise = rng.standard_normal(n)
    sweep = bandpass(noise, 350, 2200)
    # amplitude swells up then fast fade = whoosh past
    e = np.exp(-((x - 0.17) ** 2) / (2 * 0.06 ** 2))
    chirp = np.sin(2 * np.pi * (900 - 1400 * x) * x) * 0.15
    return norm(sweep * e + chirp * e)


def hit():
    n = int(SR * 0.4); x = t(0.4)
    thud = np.sin(2 * np.pi * 92 * x) * np.exp(-22 * x)
    sub = np.sin(2 * np.pi * 55 * x) * np.exp(-16 * x) * 0.7
    transient = rng.standard_normal(n) * np.exp(-90 * x)
    metal = sum(np.sin(2 * np.pi * fr * x) * np.exp(-14 * x)
                for fr in (1180, 1870, 2610, 3320)) * 0.12
    return norm(thud + sub + transient * 0.6 + metal)


def cleave():
    n = int(SR * 0.75); x = t(0.75)
    sub = (np.sin(2 * np.pi * 44 * x) + np.sin(2 * np.pi * 68 * x)) * np.exp(-9 * x)
    thud = np.sin(2 * np.pi * 96 * x) * np.exp(-13 * x)
    crack = rng.standard_normal(n) * np.exp(-70 * x)
    rumble = lowpass(rng.standard_normal(n), 180) * np.exp(-4.5 * x) * 0.8
    metal = sum(np.sin(2 * np.pi * fr * x) * np.exp(-9 * x)
                for fr in (760, 1490, 2230)) * 0.1
    body = sub + thud + crack * 0.7 + rumble + metal
    return norm(reverb_tail(body, decay=0.35))


def voice(dur, base, formants, growl=0.0):
    """Muffled 'through-a-visor' voiced tone: low fundamental + formants + low-pass."""
    n = int(SR * dur); x = t(dur)
    sig = np.sin(2 * np.pi * base * x)
    sig += 0.5 * np.sin(2 * np.pi * base * 1.5 * x)
    sig += 0.3 * np.sin(2 * np.pi * base * 2.0 * x)
    if growl:
        sig = np.tanh(sig * (1 + growl * 3))  # waveshape distortion = growl
    # formant resonances via bandpassed noise mixed in
    for fc in formants:
        sig += 0.25 * bandpass(rng.standard_normal(n), fc * 0.85, fc * 1.15)
    sig *= (0.85 + 0.15 * np.sin(2 * np.pi * 5.5 * x))  # slow tremolo
    return lowpass(sig, 1300)  # muffled / masked


def curse():
    dur = 1.15; x = t(dur)
    v = voice(dur, base=112, formants=(300, 620, 900))
    rise = np.sin(2 * np.pi * (140 + 60 * x) * x) * np.exp(-2.0 * x) * 0.25
    whisper = bandpass(rng.standard_normal(len(x)), 1500, 4500) * np.exp(-1.8 * x) * 0.2
    e = env(len(x), a=0.08, d=0.2, s=0.6, r=0.4, sus=0.7)
    return norm(reverb_tail((v + rise + whisper) * e, decay=0.5))


def trueform():
    dur = 1.25; x = t(dur)
    subrise = np.sin(2 * np.pi * (38 + 46 * np.clip(x / 0.9, 0, 1)) * x)
    growl = voice(dur, base=84, formants=(260, 520), growl=0.8)
    build = np.clip(x / 0.85, 0, 1) ** 1.5
    boom = np.sin(2 * np.pi * 70 * x) * np.exp(-8 * np.clip(x - 0.82, 0, None)) * (x > 0.8)
    crack = rng.standard_normal(len(x)) * np.exp(-60 * np.clip(x - 0.82, 0, None)) * (x > 0.8)
    body = subrise * build * 0.8 + growl * build + boom * 1.2 + crack * 0.5
    return norm(reverb_tail(lowpass(body, 2000), decay=0.4))


def armor():
    dur = 0.5; x = t(dur)
    thunk = np.sin(2 * np.pi * 120 * x) * np.exp(-20 * x)
    metal = sum(np.sin(2 * np.pi * fr * x) * np.exp(-11 * x)
                for fr in (820, 1370, 2190, 3510, 4630)) * 0.14
    shimmer = bandpass(rng.standard_normal(len(x)), 3000, 8000) * np.exp(-18 * x) * 0.15
    return norm(thunk + metal + shimmer)


def hurt():
    dur = 0.32
    v = voice(dur, base=132, formants=(400, 780), growl=0.4)
    e = env(int(SR * dur), a=0.03, d=0.25, s=0.4, r=0.5, sus=0.5)
    return norm(v * e)


def main():
    print("generating Black Knight SFX ->", OUT_DIR)
    save("bk_swing.wav", swing())
    save("bk_hit.wav", hit())
    save("bk_cleave.wav", cleave())
    save("bk_curse.wav", curse())
    save("bk_trueform.wav", trueform())
    save("bk_armor.wav", armor())
    save("bk_hurt.wav", hurt())


if __name__ == "__main__":
    main()
