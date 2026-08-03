"""
Sound effect generator for the Saiyan Transformations mod.

Synthesises every cue the mod plays. Run it from anywhere:

    python tools/generate_sounds.py

Outputs 16-bit mono 44.1kHz WAVs into ../assets/sounds/, plus a
tools/preview_sounds.png showing the waveform and spectrogram of each cue so the
sound design can be checked without launching the game.

These are original synthesised effects built to match the *sound design language*
of the source material - the layered ki roar, the sub-heavy transformation
impact, the rising charge whine and the band-limited beam roar. They are not
samples from the show. If you want the genuine recordings, drop your own files
into assets/sounds/ under the same names and the mod will use them instead.

Design notes, per layer:
  ki roar      resonant band-passed pink noise, slowly modulated, then saturated
  impact       exponential sub sine drop plus a broadband transient
  power surge  exponential pitch sweep through a tracking resonant filter
  crackle      sparse short noise grains, band-passed into the electric range
  divine pad   detuned saws under a slow filter opening (God / Blue only)
  metal ring   inharmonic partials with long decays (Ultra Instinct only)
"""

import os
import wave

import numpy as np
from scipy import signal

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets", "sounds")
TOOLS = os.path.dirname(os.path.abspath(__file__))


# --------------------------------------------------------------- primitives

def n_of(dur):
    return int(round(SR * dur))


def taxis(dur):
    return np.arange(n_of(dur)) / SR


def rng(seed):
    return np.random.default_rng(seed)


def white(dur, seed):
    return rng(seed).standard_normal(n_of(dur))


def pink(dur, seed):
    """Voss-ish pinking filter; gives noise a natural downward tilt."""
    b = [0.049922035, -0.095993537, 0.050612699, -0.004408786]
    a = [1.0, -2.494956002, 2.017265875, -0.522189400]
    return signal.lfilter(b, a, white(dur, seed))


def smooth_noise(dur, rate, seed):
    """Random control signal band-limited to `rate` Hz, normalised to +/-1."""
    x = white(dur, seed)
    sos = signal.butter(2, max(0.5, rate), btype="low", fs=SR, output="sos")
    y = signal.sosfilt(sos, x)
    peak = np.abs(y).max()
    return y / peak if peak > 1e-9 else y


def biquad_bandpass(f0, q):
    w0 = 2 * np.pi * f0 / SR
    alpha = np.sin(w0) / (2 * q)
    b = np.array([q * alpha, 0.0, -q * alpha])
    a = np.array([1 + alpha, -2 * np.cos(w0), 1 - alpha])
    return b / a[0], a / a[0]


def biquad_lowpass(f0, q):
    w0 = 2 * np.pi * f0 / SR
    alpha = np.sin(w0) / (2 * q)
    cw = np.cos(w0)
    b = np.array([(1 - cw) / 2, 1 - cw, (1 - cw) / 2])
    a = np.array([1 + alpha, -2 * cw, 1 - alpha])
    return b / a[0], a / a[0]


def tv_filter(x, f_curve, q, kind="band", block=128):
    """Time-varying resonant filter. The moving resonance is what makes these
    read as energy rather than as plain noise."""
    f_curve = np.clip(np.asarray(f_curve, dtype=float), 20.0, SR * 0.45)
    if f_curve.ndim == 0:
        f_curve = np.full(len(x), float(f_curve))
    out = np.zeros_like(x)
    zi = np.zeros(2)
    design = biquad_bandpass if kind == "band" else biquad_lowpass
    for start in range(0, len(x), block):
        stop = min(start + block, len(x))
        f0 = float(np.mean(f_curve[start:stop]))
        b, a = design(f0, q)
        out[start:stop], zi = signal.lfilter(b, a, x[start:stop], zi=zi)
    return out


def expsweep(dur, f0, f1, curve=1.0, wave_kind="sine", seed=0):
    t = taxis(dur)
    if len(t) == 0:
        return t
    x = (t / max(t[-1], 1e-9)) ** curve
    f = f0 * (f1 / f0) ** x
    phase = 2 * np.pi * np.cumsum(f) / SR
    if wave_kind == "sine":
        return np.sin(phase)
    if wave_kind == "saw":
        return signal.sawtooth(phase)
    if wave_kind == "square":
        return signal.square(phase, 0.45)
    raise ValueError(wave_kind)


def env(dur, attack, hold, release, curve=2.0):
    t = taxis(dur)
    e = np.ones(len(t))
    na, nh = n_of(attack), n_of(hold)
    if na > 0:
        e[:na] = np.linspace(0, 1, na) ** 0.6
    tail = len(t) - na - nh
    if tail > 0:
        e[na + nh:] = np.linspace(1, 0, tail) ** curve
    return e


def decay(dur, tau, attack=0.002):
    t = taxis(dur)
    e = np.exp(-t / max(tau, 1e-5))
    na = n_of(attack)
    if na > 1:
        e[:na] *= np.linspace(0, 1, na)
    return e


def saturate(x, drive):
    return np.tanh(x * drive) / np.tanh(drive)


def add(base, sig, at=0.0, gain=1.0):
    start = n_of(at)
    if start >= len(base):
        return base
    stop = min(len(base), start + len(sig))
    base[start:stop] += sig[:stop - start] * gain
    return base


def finalize(x, drive=1.8, hp=30.0):
    """Trim inaudible sub rumble, then soft-clip so the body of the sound comes
    up instead of the transient hogging all the headroom."""
    sos = signal.butter(2, hp, btype="high", fs=SR, output="sos")
    x = signal.sosfilt(sos, x)
    m = np.abs(x).max()
    if m > 1e-9:
        x = x / m
    return saturate(x * 1.7, drive)


def reverb(x, tail=0.30, mix=0.25, seed=99):
    L = n_of(tail)
    ir = rng(seed).standard_normal(L) * np.exp(-np.arange(L) / (SR * tail * 0.25))
    ir[0] += 1.0
    wet = signal.fftconvolve(x, ir)[:len(x)]
    peak = np.abs(wet).max()
    if peak > 1e-9:
        wet = wet / peak * (np.abs(x).max() + 1e-9)
    return (1 - mix) * x + mix * wet


# ------------------------------------------------------------------ layers

def ki_roar(dur, centre, q, seed, brightness=1.0, drive=2.4, mod_rate=6.0, mod_depth=0.22):
    """The signature aura roar: a moving resonance over pink noise, saturated."""
    src = pink(dur, seed)
    lfo = smooth_noise(dur, mod_rate, seed + 11)
    body = tv_filter(src, centre * (1.0 + mod_depth * lfo), q, "band")

    air_lfo = smooth_noise(dur, mod_rate * 1.7, seed + 23)
    air = tv_filter(pink(dur, seed + 31),
                    centre * 4.2 * brightness * (1.0 + 0.15 * air_lfo), 1.1, "band")

    low = tv_filter(pink(dur, seed + 47), centre * 0.42, 0.9, "band")

    x = body + 0.38 * brightness * air + 0.55 * low
    return saturate(x, drive)


def crackle(dur, density, seed, lo=1800.0, hi=7500.0, grain=0.0045, level=1.0):
    n = n_of(dur)
    out = np.zeros(n)
    r = rng(seed)
    for _ in range(int(dur * density)):
        pos = int(r.random() * n)
        length = int(SR * grain * (0.5 + 1.5 * r.random()))
        if length < 2 or pos + length >= n:
            continue
        e = np.exp(-np.arange(length) / (SR * grain * 0.35))
        out[pos:pos + length] += r.standard_normal(length) * e * (0.35 + 0.65 * r.random())
    sos = signal.butter(2, [lo, hi], btype="band", fs=SR, output="sos")
    return signal.sosfilt(sos, out) * level


def sub_hit(dur, f_start, f_end, tau, seed=0):
    t = taxis(dur)
    f = f_end + (f_start - f_end) * np.exp(-t / (tau * 0.45))
    phase = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(phase) * decay(dur, tau, 0.001)


def transient(dur, tau, seed, hp=250.0):
    x = white(dur, seed) * decay(dur, tau, 0.0008)
    sos = signal.butter(2, hp, btype="high", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def divine_pad(dur, f0, cutoff, seed):
    t = taxis(dur)
    x = np.zeros(len(t))
    for det in (0.995, 1.0, 1.006, 1.494, 2.001):
        x += signal.sawtooth(2 * np.pi * f0 * det * t + rng(seed + int(det * 100)).random())
    x /= 5.0
    sweep = np.linspace(cutoff * 0.35, cutoff, len(t))
    x = tv_filter(x, sweep, 0.9, "low")
    return x * env(dur, dur * 0.45, 0.0, dur * 0.55, 1.4)


def metal_ring(dur, f0, seed):
    t = taxis(dur)
    x = np.zeros(len(t))
    for ratio, amp, tau in ((1.0, 1.0, 0.9), (2.76, 0.55, 0.7), (5.40, 0.32, 0.5),
                            (8.93, 0.18, 0.35), (13.34, 0.10, 0.25)):
        x += amp * np.sin(2 * np.pi * f0 * ratio * t + rng(seed + int(ratio * 10)).random()) \
             * np.exp(-t / tau)
    return x / 2.2


# ------------------------------------------------------------- transformations

TRANSFORMS = {
    # name: roar centre, Q, brightness, drive, duration, pre-suck, crackle density,
    #       sub start/end, surge top, extras
    "transform_ssj": dict(
        centre=265, q=2.2, bright=1.00, drive=2.6, dur=1.90, pre=0.26, crack=0,
        sub=(78, 38), tau=0.26, surge=820, pad=None, ring=None, seed=101),
    "transform_ssj2": dict(
        centre=305, q=2.6, bright=1.20, drive=3.1, dur=2.00, pre=0.22, crack=95,
        sub=(84, 40), tau=0.25, surge=980, pad=None, ring=None, seed=202),
    "transform_ssj3": dict(
        centre=185, q=2.0, bright=0.95, drive=3.6, dur=2.90, pre=0.55, crack=130,
        sub=(70, 30), tau=0.42, surge=700, pad=None, ring=None, seed=303),
    "transform_god": dict(
        centre=225, q=1.8, bright=0.85, drive=2.2, dur=2.30, pre=0.34, crack=25,
        sub=(74, 34), tau=0.32, surge=640, pad=146.8, ring=None, seed=404),
    "transform_blue": dict(
        centre=345, q=2.4, bright=1.35, drive=2.5, dur=2.30, pre=0.30, crack=80,
        sub=(80, 36), tau=0.30, surge=1150, pad=196.0, ring=None, seed=505),
    "transform_ui": dict(
        centre=400, q=2.8, bright=1.55, drive=1.9, dur=2.50, pre=0.42, crack=45,
        sub=(66, 30), tau=0.34, surge=1400, pad=None, ring=880.0, seed=606),
}


def make_transform(p):
    dur, pre, seed = p["dur"], p["pre"], p["seed"]
    out = np.zeros(n_of(dur))

    # 1. inward suck before the burst
    suck = white(pre, seed + 1)
    suck = tv_filter(suck, np.geomspace(260, 3200, n_of(pre)), 1.4, "band")
    suck *= np.linspace(0, 1, n_of(pre)) ** 3.0
    add(out, suck, 0.0, 0.42)

    # 2. impact
    body = dur - pre
    add(out, sub_hit(min(1.2, body), p["sub"][0], p["sub"][1], p["tau"], seed + 2), pre, 0.55)
    add(out, transient(0.22, 0.035, seed + 3), pre, 0.55)

    # 3. ki roar for the rest of the cue - this is the layer that should carry it
    roar = ki_roar(body, p["centre"], p["q"], seed + 4,
                   brightness=p["bright"], drive=p["drive"])
    roar *= env(body, 0.035, body * 0.30, body * 0.65, 1.7)
    add(out, roar, pre, 1.10)

    # 4. power surge sweeping up through a tracking resonance
    sdur = min(0.72, body * 0.55)
    surge = 0.6 * expsweep(sdur, p["centre"] * 0.45, p["surge"], 0.8, "saw") \
        + 0.4 * expsweep(sdur, p["centre"] * 0.45, p["surge"], 0.8, "sine")
    surge = tv_filter(surge, np.geomspace(p["centre"] * 0.9, p["surge"] * 1.6, n_of(sdur)),
                      1.6, "low")
    surge *= env(sdur, 0.02, sdur * 0.3, sdur * 0.68, 1.5)
    add(out, surge, max(0.0, pre - 0.06), 0.45)

    # 5. electric crackle
    if p["crack"]:
        add(out, crackle(body, p["crack"], seed + 5, level=1.0)
            * env(body, 0.02, body * 0.35, body * 0.63, 1.2), pre, 0.55)

    # 6. per-form character
    if p["pad"]:
        add(out, divine_pad(body, p["pad"], 2400, seed + 6), pre, 0.30)
    if p["ring"]:
        add(out, metal_ring(min(1.6, body), p["ring"], seed + 7), pre, 0.32)

    return finalize(reverb(out, 0.32, 0.20, seed + 8), 1.9)


# --------------------------------------------------------------- other cues

def make_aura_loop(dur=2.0, seed=777):
    """Seamless bed played while transformed."""
    x = ki_roar(dur, 240, 1.9, seed, brightness=1.0, drive=2.0, mod_rate=4.0, mod_depth=0.18)
    x += 0.25 * crackle(dur, 40, seed + 1, level=0.8)
    return make_seamless(x, 0.045)


def make_kame_charge(dur=0.78, seed=808):
    out = np.zeros(n_of(dur))
    t = taxis(dur)

    # rising whine, sine plus a touch of saw for bite
    tone = 0.65 * expsweep(dur, 130, 1500, 1.35, "sine") \
        + 0.35 * expsweep(dur, 130, 1500, 1.35, "saw")
    trem_rate = np.linspace(7, 22, len(t))
    trem = 1.0 - 0.20 * (0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(trem_rate) / SR))
    tone *= trem * (np.linspace(0, 1, len(t)) ** 1.4)
    add(out, tone, 0.0, 0.55)

    # gathering air
    air = tv_filter(pink(dur, seed + 1), np.geomspace(380, 5200, len(t)), 1.8, "band")
    air *= np.linspace(0, 1, len(t)) ** 2.0
    add(out, air, 0.0, 0.55)

    # low throb accelerating as the ki condenses
    throb_rate = np.linspace(6, 18, len(t))
    throb = np.sin(2 * np.pi * 52 * t) * (0.68 + 0.32 * signal.square(
        2 * np.pi * np.cumsum(throb_rate) / SR, 0.40))
    add(out, throb * np.linspace(0, 1, len(t)), 0.0, 0.28)

    return finalize(out, 1.7)


def make_kame_fire(dur=1.10, seed=909):
    out = np.zeros(n_of(dur))

    add(out, transient(0.35, 0.075, seed + 1, hp=160), 0.0, 1.00)
    add(out, sub_hit(0.85, 72, 33, 0.20, seed + 2), 0.0, 0.50)

    body = dur - 0.02
    beam = ki_roar(body, 470, 1.7, seed + 3, brightness=1.25, drive=3.4,
                   mod_rate=9.0, mod_depth=0.12)
    beam = tv_filter(beam, np.geomspace(900, 420, n_of(body)), 1.0, "low")
    beam *= env(body, 0.012, body * 0.45, body * 0.54, 1.3)
    add(out, beam, 0.02, 1.15)

    return finalize(reverb(saturate(out, 2.0), 0.30, 0.18, seed + 4), 2.0)


def make_kame_beam_loop(dur=0.50, seed=1010):
    """Sustained beam bed. Modulation rates divide evenly into the loop length."""
    n = n_of(dur)
    t = taxis(dur)
    core = tv_filter(pink(dur, seed), 500, 2.1, "band")
    air = tv_filter(pink(dur, seed + 1), 3400, 1.2, "band") * 0.45
    low = tv_filter(pink(dur, seed + 2), 150, 1.0, "band") * 0.65
    am = 1.0 - 0.22 * np.sin(2 * np.pi * 40 * t)     # 20 whole cycles in 0.5s
    x = saturate((core + air + low) * am, 2.6)
    return make_seamless(x, 0.03)


def make_kame_impact(dur=0.95, seed=1111):
    out = np.zeros(n_of(dur))
    add(out, transient(0.45, 0.09, seed + 1, hp=120), 0.0, 1.00)
    add(out, sub_hit(0.80, 60, 28, 0.18, seed + 2), 0.0, 0.55)
    add(out, crackle(0.7, 220, seed + 3, lo=900, hi=6000, grain=0.006)
        * decay(0.7, 0.22, 0.004), 0.02, 0.55)
    rumble = tv_filter(pink(0.9, seed + 4), np.geomspace(320, 90, n_of(0.9)), 1.2, "band")
    add(out, rumble * decay(0.9, 0.30, 0.01), 0.03, 0.90)
    return finalize(reverb(out, 0.45, 0.30, seed + 5), 1.9)


def make_powerdown(dur=1.00, seed=1212):
    out = np.zeros(n_of(dur))
    fall = 0.6 * expsweep(dur, 780, 85, 0.85, "saw") + 0.4 * expsweep(dur, 780, 85, 0.85, "sine")
    fall = tv_filter(fall, np.geomspace(2400, 190, n_of(dur)), 1.3, "low")
    add(out, fall * env(dur, 0.01, dur * 0.15, dur * 0.84, 1.8), 0.0, 0.55)

    roar = ki_roar(dur, 230, 1.8, seed + 1, brightness=0.8, drive=1.8)
    roar = tv_filter(roar, np.geomspace(1800, 220, n_of(dur)), 0.9, "low")
    add(out, roar * env(dur, 0.005, dur * 0.10, dur * 0.89, 2.2), 0.0, 0.55)

    add(out, sub_hit(0.5, 55, 26, 0.14, seed + 2), dur * 0.55, 0.20)
    return finalize(reverb(out, 0.28, 0.18, seed + 3), 1.8)


def make_boss_roar(dur=1.90, seed=1414):
    """Low, menacing bellow for a boss entrance."""
    out = np.zeros(n_of(dur))

    growl = ki_roar(dur, 115, 2.4, seed, brightness=0.55, drive=4.2,
                    mod_rate=3.2, mod_depth=0.30)
    growl *= env(dur, 0.10, dur * 0.35, dur * 0.55, 1.5)
    add(out, growl, 0.0, 1.10)

    # descending menace under it
    fall = expsweep(dur * 0.8, 190, 62, 1.1, "saw")
    fall = tv_filter(fall, np.geomspace(700, 130, n_of(dur * 0.8)), 1.4, "low")
    add(out, fall * env(dur * 0.8, 0.06, dur * 0.3, dur * 0.45, 1.6), 0.05, 0.55)

    add(out, sub_hit(0.9, 64, 30, 0.26, seed + 1), 0.0, 0.60)
    # dissonant low partials for unease
    t = taxis(dur)
    for f, amp in ((58.0, 0.5), (87.0, 0.32), (139.0, 0.18)):
        out[:len(t)] += amp * 0.25 * np.sin(2 * np.pi * f * t) * np.exp(-t / 1.1)

    return finalize(reverb(out, 0.60, 0.32, seed + 2), 2.1)


def make_boss_defeat(dur=1.70, seed=1515):
    """Death blast plus a falling wail."""
    out = np.zeros(n_of(dur))
    add(out, transient(0.50, 0.10, seed + 1, hp=140), 0.0, 1.00)
    add(out, sub_hit(0.9, 78, 30, 0.22, seed + 2), 0.0, 0.60)

    wail = expsweep(dur * 0.85, 620, 70, 0.9, "saw")
    wail = tv_filter(wail, np.geomspace(2100, 150, n_of(dur * 0.85)), 1.5, "low")
    add(out, wail * env(dur * 0.85, 0.03, dur * 0.2, dur * 0.72, 1.7), 0.02, 0.60)

    add(out, crackle(1.1, 260, seed + 3, lo=800, hi=6500, grain=0.007)
        * decay(1.1, 0.34, 0.005), 0.04, 0.55)
    return finalize(reverb(out, 0.70, 0.36, seed + 4), 1.9)


def make_dodge(dur=0.34, seed=1616):
    """Short, sharp afterimage whoosh."""
    n = n_of(dur)
    out = np.zeros(n)
    air = tv_filter(white(dur, seed), np.geomspace(5200, 700, n), 1.9, "band")
    air *= decay(dur, 0.075, 0.003)
    add(out, air, 0.0, 1.00)

    shimmer = metal_ring(dur, 1650.0, seed + 1) * decay(dur, 0.055, 0.002)
    add(out, shimmer, 0.0, 0.40)
    return finalize(out, 1.6)


def make_shenron(dur=3.20, seed=1717):
    """Sky-darkening rumble for the summoning. Deep, slow, and building."""
    out = np.zeros(n_of(dur))

    rumble = ki_roar(dur, 78, 2.0, seed, brightness=0.4, drive=3.4,
                     mod_rate=2.0, mod_depth=0.35)
    rumble *= env(dur, dur * 0.45, dur * 0.25, dur * 0.30, 1.3)
    add(out, rumble, 0.0, 1.15)

    # slow rise underneath, like something enormous waking up
    rise = expsweep(dur, 42, 240, 1.9, "saw")
    rise = tv_filter(rise, np.geomspace(120, 900, n_of(dur)), 1.6, "low")
    add(out, rise * env(dur, dur * 0.6, 0.0, dur * 0.4, 1.2), 0.0, 0.55)

    # thunder cracks
    for at, amp in ((0.55, 0.7), (1.45, 0.85), (2.35, 1.0)):
        add(out, transient(0.55, 0.10, seed + int(at * 100), hp=180), at, amp * 0.8)
        add(out, sub_hit(0.7, 70, 30, 0.20, seed + int(at * 200)), at, amp * 0.5)

    t = taxis(dur)
    out[:len(t)] += 0.18 * np.sin(2 * np.pi * 47 * t) * np.linspace(0, 1, len(t))
    return finalize(reverb(out, 0.85, 0.40, seed + 9), 2.0)


def make_dragonball(dur=0.85, seed=1818):
    """Bright chime for picking up a ball."""
    out = np.zeros(n_of(dur))
    for i, f in enumerate((784.0, 1046.5, 1318.5)):
        add(out, metal_ring(dur - i * 0.06, f, seed + i), i * 0.06, 0.7)
    shimmer = tv_filter(pink(dur, seed + 5), np.geomspace(2200, 6000, n_of(dur)), 1.6, "band")
    add(out, shimmer * decay(dur, 0.16, 0.004), 0.0, 0.35)
    return finalize(reverb(out, 0.35, 0.28, seed + 6), 1.5)


def make_unlock(dur=1.80, seed=1313):
    """Triumphant shimmer for the moment a new form becomes available."""
    out = np.zeros(n_of(dur))
    for i, f in enumerate((392.0, 523.25, 659.25, 784.0)):
        seg = min(1.5, dur - 0.12 * i)
        out = add(out, metal_ring(seg, f, seed + i) * 0.9, 0.12 * i, 0.55)

    swell = tv_filter(pink(dur * 0.7, seed + 9),
                      np.geomspace(300, 4200, n_of(dur * 0.7)), 1.5, "band")
    swell *= np.linspace(0, 1, n_of(dur * 0.7)) ** 2.2
    add(out, swell, 0.0, 0.35)
    add(out, sub_hit(0.7, 90, 42, 0.30, seed + 10), 0.30, 0.45)
    return finalize(reverb(out, 0.55, 0.32, seed + 11), 1.6)


# ------------------------------------------------------------------- output

def make_seamless(x, fade):
    """Crossfade the tail into the head so the file loops without a click."""
    nf = n_of(fade)
    if nf * 2 >= len(x):
        return x
    head = x[:nf].copy()
    tail = x[-nf:].copy()
    ramp = np.linspace(0, 1, nf)
    x = x[:-nf]
    x[:nf] = head * ramp + tail * (1 - ramp)
    return x


def write_wav(path, x, peak=0.89):
    x = np.asarray(x, dtype=float)
    m = np.abs(x).max()
    if m > 1e-9:
        x = x / m * peak
    data = np.clip(x * 32767.0, -32768, 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    return len(data) / SR


def build_all():
    os.makedirs(OUT, exist_ok=True)
    cues = {}
    for name, params in TRANSFORMS.items():
        cues[name] = make_transform(params)
    cues["aura_loop"] = make_aura_loop()
    cues["kame_charge"] = make_kame_charge()
    cues["kame_fire"] = make_kame_fire()
    cues["kame_beam_loop"] = make_kame_beam_loop()
    cues["kame_impact"] = make_kame_impact()
    cues["powerdown"] = make_powerdown()
    cues["unlock"] = make_unlock()
    cues["boss_roar"] = make_boss_roar()
    cues["boss_defeat"] = make_boss_defeat()
    cues["dodge"] = make_dodge()
    cues["shenron"] = make_shenron()
    cues["dragonball"] = make_dragonball()

    # loops sit under the action, so they are mastered quieter than one-shots
    peaks = {"aura_loop": 0.46, "kame_beam_loop": 0.70}
    for name, x in cues.items():
        dur = write_wav(os.path.join(OUT, name + ".wav"), x, peaks.get(name, 0.89))
        print(f"{name + '.wav':24s} {dur:5.2f}s")
    return cues


def make_preview(cues):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt

    names = list(cues)
    fig, axes = plt.subplots(len(names), 2, figsize=(13, 1.5 * len(names)))
    fig.patch.set_facecolor("#1a1c24")
    for i, name in enumerate(names):
        x = cues[name]
        x = x / (np.abs(x).max() + 1e-9)

        ax = axes[i][0]
        ax.plot(np.arange(len(x)) / SR, x, lw=0.4, color="#ffd24e")
        ax.set_ylim(-1.05, 1.05)
        ax.set_ylabel(name, rotation=0, ha="right", va="center",
                      fontsize=8, color="#e8ecf4")

        ax2 = axes[i][1]
        ax2.specgram(x, NFFT=1024, Fs=SR, noverlap=512, cmap="magma", vmin=-110, vmax=-20)
        ax2.set_yscale("symlog", linthresh=200)
        ax2.set_ylim(30, 16000)

        for a in (ax, ax2):
            a.set_facecolor("#1a1c24")
            a.tick_params(labelsize=6, colors="#8892a4")
            for s in a.spines.values():
                s.set_color("#333845")
            if i != len(names) - 1:
                a.set_xticklabels([])
    fig.tight_layout()
    path = os.path.join(TOOLS, "preview_sounds.png")
    fig.savefig(path, dpi=110, facecolor=fig.get_facecolor())
    print("preview ->", path)


if __name__ == "__main__":
    make_preview(build_all())
