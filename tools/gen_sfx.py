#!/usr/bin/env python3
"""
Procedurally synthesize the game's SFX set as 16-bit 44.1 kHz mono WAVs into
Assets/Resources/Audio/SFX, matching the canonical keys in SfxManager.cs.
Pure stdlib (no numpy): additive partials, filtered noise bursts, pitch sweeps.
Deterministic (seeded) so regeneration is stable. Rerun any time:
  python3 tools/gen_sfx.py
"""
import math
import os
import random
import struct
import wave

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Audio", "SFX")
rng = random.Random(1337)


def write_wav(name, samples, gain=0.9):
    peak = max(1e-6, max(abs(s) for s in samples))
    scale = gain * 32767 / peak
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-32767, min(32767, s * scale)))) for s in samples))
    print(f"  {name}.wav  {len(samples) / SR:.2f}s")


def env_exp(n, k):
    return [math.exp(-k * i / SR) for i in range(n)]


def noise(n):
    return [rng.uniform(-1, 1) for _ in range(n)]


def lowpass(xs, cutoff_start, cutoff_end=None):
    """One-pole LPF with optionally sweeping cutoff."""
    cutoff_end = cutoff_end if cutoff_end is not None else cutoff_start
    y, out, n = 0.0, [], len(xs)
    for i, x in enumerate(xs):
        c = cutoff_start + (cutoff_end - cutoff_start) * i / n
        a = min(0.99, 2 * math.pi * c / SR)
        y += a * (x - y)
        out.append(y)
    return out


def highpass(xs, cutoff):
    lp = lowpass(xs, cutoff)
    return [x - l for x, l in zip(xs, lp)]


def sine_sweep(n, f0, f1, k=0.0):
    out, phase = [], 0.0
    for i in range(n):
        f = f0 + (f1 - f0) * i / n
        phase += 2 * math.pi * f / SR
        out.append(math.sin(phase) * (math.exp(-k * i / SR) if k else 1.0))
    return out


def mix(*tracks):
    n = max(len(t) for t in tracks)
    return [sum(t[i] if i < len(t) else 0.0 for t in tracks) for i in range(n)]


def mul(xs, ys):
    return [x * y for x, y in zip(xs, ys)]


def secs(s):
    return int(s * SR)


def partial_hit(freqs, dur, k=18.0, detune=0.003):
    n = secs(dur)
    tracks = []
    for f in freqs:
        fd = f * (1 + rng.uniform(-detune, detune))
        tracks.append(mul(sine_sweep(n, fd, fd * 0.98), env_exp(n, k)))
    return mix(*tracks)


def whoosh(dur, c0, c1, hp=300, k=9.0):
    n = secs(dur)
    body = lowpass(highpass(noise(n), hp), c0, c1)
    # swell in then out so it reads as a swing, not a click
    envA = [math.sin(math.pi * min(1.0, i / n)) ** 1.5 for i in range(n)]
    return mul(mul(body, envA), env_exp(n, k * 0.3))


os.makedirs(OUT, exist_ok=True)
print("Synthesizing SFX ->", os.path.relpath(OUT))

# --- Attacks ---
write_wav("sfx_punch_whiff", whoosh(0.16, 1800, 700))
write_wav("sfx_sword_swing", whoosh(0.26, 2600, 500, hp=500))
write_wav("sfx_dodge", whoosh(0.14, 3000, 1200, hp=800))

thump = mul(sine_sweep(secs(0.14), 95, 42), env_exp(secs(0.14), 26))
crack = mul(lowpass(noise(secs(0.05)), 4000), env_exp(secs(0.05), 90))
write_wav("sfx_punch_hit", mix(thump, crack))

metal = partial_hit([720, 1080, 1460, 2210], 0.28, k=22)
slice_ = mul(highpass(noise(secs(0.08)), 2500), env_exp(secs(0.08), 60))
write_wav("sfx_sword_hit", mix(metal, slice_, mul(sine_sweep(secs(0.12), 130, 60), env_exp(secs(0.12), 30))))

# --- Block / guard ---
write_wav("sfx_block_raise", partial_hit([340, 510], 0.12, k=40))
clank = partial_hit([420, 830, 1240], 0.22, k=26)
write_wav("sfx_block_impact", mix(clank, mul(sine_sweep(secs(0.1), 110, 60), env_exp(secs(0.1), 35))))
shatter = mul(highpass(noise(secs(0.4)), 900), env_exp(secs(0.4), 9))
write_wav("sfx_guard_break", mix(shatter, mul(sine_sweep(secs(0.45), 520, 90), env_exp(secs(0.45), 7))))

# --- Player ---
grunt = mul(lowpass(noise(secs(0.18)), 900, 300), env_exp(secs(0.18), 18))
write_wav("sfx_player_hurt", mix(grunt, mul(sine_sweep(secs(0.15), 220, 130), env_exp(secs(0.15), 20))))
write_wav("sfx_player_die", mix(
    mul(sine_sweep(secs(0.7), 260, 55), env_exp(secs(0.7), 4.5)),
    mul(lowpass(noise(secs(0.6)), 700, 150), env_exp(secs(0.6), 6))))

# --- Enemies ---
write_wav("sfx_orc_die", mix(
    mul(lowpass(noise(secs(0.35)), 500, 120), env_exp(secs(0.35), 8)),
    mul(sine_sweep(secs(0.35), 160, 60), env_exp(secs(0.35), 9))))

rattle = []
for i in range(7):  # bone clatter: a burst of short dry clicks
    click = mul(highpass(noise(secs(0.03)), 1500), env_exp(secs(0.03), 120))
    rattle += click + [0.0] * secs(0.02 + 0.012 * i)
write_wav("sfx_skeleton_die", rattle)

write_wav("sfx_brute_die", mix(
    mul(sine_sweep(secs(0.8), 120, 34), env_exp(secs(0.8), 4)),
    mul(lowpass(noise(secs(0.5)), 400, 90), env_exp(secs(0.5), 6))))

inhale = mul(lowpass(highpass(noise(secs(0.3)), 400), 500, 2400), [i / secs(0.3) for i in range(secs(0.3))])
write_wav("sfx_enemy_windup", inhale, gain=0.5)

# --- Waves / UI ---
horn = mix(*[mul(sine_sweep(secs(0.8), f, f * 0.995), [min(1.0, i / secs(0.18)) for i in range(secs(0.8))])
             for f in (196, 294, 392)])
write_wav("sfx_wave_start", mul(horn, env_exp(secs(0.8), 2.2)), gain=0.7)

chime = []
for j, f in enumerate((523, 659, 784)):  # rising victory arpeggio
    tone = mul(sine_sweep(secs(0.35), f, f), env_exp(secs(0.35), 8))
    chime = mix(chime + [0.0] * (secs(0.11 * j) + len(tone) - len(chime)) if chime else tone,
                [0.0] * secs(0.11 * j) + tone)
write_wav("sfx_wave_clear", chime, gain=0.7)

write_wav("sfx_ui_button", mul(sine_sweep(secs(0.05), 1050, 900), env_exp(secs(0.05), 45)), gain=0.5)
write_wav("sfx_combo_up", mix(
    mul(sine_sweep(secs(0.16), 880, 880), env_exp(secs(0.16), 14)),
    mul(sine_sweep(secs(0.16), 1318, 1318), env_exp(secs(0.16), 16))), gain=0.6)

step = mul(lowpass(noise(secs(0.07)), 900, 250), env_exp(secs(0.07), 45))
write_wav("sfx_footstep", step, gain=0.45)

# Parry: bright metallic ping — instantly readable as "you nailed the timing".
ping = partial_hit([1180, 1770, 2650, 3540], 0.35, k=14, detune=0.001)
write_wav("sfx_parry", mix(ping, mul(highpass(noise(secs(0.05)), 3000), env_exp(secs(0.05), 80))), gain=0.8)

print("done.")
