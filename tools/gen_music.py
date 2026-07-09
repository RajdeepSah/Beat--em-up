#!/usr/bin/env python3
"""
Procedurally synthesize the adaptive music layers as loopable 44.1 kHz WAVs into
Assets/Resources/Audio/Music (loaded by MusicManager):
  music_menu.wav            16 s  low war-camp drone (integer cycle counts -> seamless loop)
  music_combat_base.wav     9.6 s siege drums at 100 BPM (16 beats)
  music_combat_intense.wav  9.6 s double-time toms + metal accents (sample-locked layer)
Deterministic; pure stdlib. Rerun any time:  python3 tools/gen_music.py
Replace any file with licensed music of the same name whenever you like.
"""
import math
import os
import random
import struct
import wave

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Audio", "Music")
rng = random.Random(7)

BPM = 100.0
BEAT = 60.0 / BPM                 # 0.6 s
LOOP_BEATS = 16
LOOP_SECS = BEAT * LOOP_BEATS     # 9.6 s
LOOP_N = int(LOOP_SECS * SR)


def write_wav(name, samples, gain=0.9):
    peak = max(1e-6, max(abs(s) for s in samples))
    scale = gain * 32767 / peak
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-32767, min(32767, s * scale)))) for s in samples))
    print(f"  {name}.wav  {len(samples) / SR:.2f}s")


def add_at(buf, t, samples, vol=1.0):
    i0 = int(t * SR)
    for j, s in enumerate(samples):
        k = (i0 + j) % len(buf)  # wrap: hits near the loop end ring into the start
        buf[k] += s * vol


def drum(freq0, freq1, dur, k, punch=0.4):
    """Pitch-dropping sine 'skin' + a short noise attack."""
    n = int(dur * SR)
    out, phase = [], 0.0
    for i in range(n):
        f = freq0 + (freq1 - freq0) * (i / n)
        phase += 2 * math.pi * f / SR
        env = math.exp(-k * i / SR)
        s = math.sin(phase) * env
        if i < int(0.012 * SR):
            s += rng.uniform(-1, 1) * punch * (1 - i / (0.012 * SR))
        out.append(s)
    return out


def metal(freqs, dur, k):
    n = int(dur * SR)
    out = [0.0] * n
    for f in freqs:
        phase = 0.0
        fd = f * (1 + rng.uniform(-0.002, 0.002))
        for i in range(n):
            phase += 2 * math.pi * fd / SR
            out[i] += math.sin(phase) * math.exp(-k * i / SR)
    return out


def click(dur=0.03):
    n = int(dur * SR)
    return [rng.uniform(-1, 1) * math.exp(-90 * i / SR) for i in range(n)]


os.makedirs(OUT, exist_ok=True)
print("Synthesizing music ->", os.path.relpath(OUT))

# ---- Menu drone: slow-beating low fifth + wind. 16 s, all integer-cycle -> perfect loop. ----
N = 16 * SR
menu = [0.0] * N
for f, vol in ((55.0, 0.5), (82.5, 0.35), (110.25, 0.18)):  # integer cycles in 16 s
    phase = 0.0
    for i in range(N):
        phase += 2 * math.pi * f / SR
        swell = 0.75 + 0.25 * math.sin(2 * math.pi * (i / N) * 2)  # 2 full swells per loop
        menu[i] += math.sin(phase) * vol * swell
lp = 0.0
for i in range(N):  # filtered wind bed
    lp += 0.008 * (rng.uniform(-1, 1) - lp)
    menu[i] += lp * 2.2
write_wav("music_menu", menu, gain=0.6)

# ---- Combat base: taiko pattern.  X..x X..x X.xx X..x  (per 4 beats) ----
base = [0.0] * LOOP_N
big = drum(70, 38, 0.5, 7, punch=0.5)
mid = drum(120, 70, 0.25, 14, punch=0.3)
for bar in range(4):
    t0 = bar * 4 * BEAT
    add_at(base, t0 + 0.0 * BEAT, big, 1.0)
    add_at(base, t0 + 1.5 * BEAT, mid, 0.5)
    add_at(base, t0 + 2.0 * BEAT, big, 0.85 if bar % 2 == 0 else 0.6)
    add_at(base, t0 + 3.5 * BEAT, mid, 0.55)
    if bar == 2:
        add_at(base, t0 + 2.5 * BEAT, mid, 0.7)
for beat in range(LOOP_BEATS):  # dry pulse keeps time between hits
    add_at(base, beat * BEAT, click(), 0.12)
write_wav("music_combat_base", base, gain=0.75)

# ---- Intensity layer: double-time toms + metal accents (fades in with the crowd). ----
intense = [0.0] * LOOP_N
tom = drum(160, 95, 0.16, 20, punch=0.25)
for eighth in range(LOOP_BEATS * 2):
    t = eighth * BEAT / 2
    if eighth % 8 in (2, 5, 7):
        add_at(intense, t, tom, 0.55 + 0.1 * rng.uniform(-1, 1))
anvil = metal([620, 930, 1470], 0.4, 10)
add_at(intense, 7.5 * BEAT, anvil, 0.5)
add_at(intense, 15.5 * BEAT, anvil, 0.65)
write_wav("music_combat_intense", intense, gain=0.7)

print("done.")
