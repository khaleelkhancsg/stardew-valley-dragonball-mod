"""
Asset generator for the Saiyan Transformations mod.

Everything the mod draws is produced procedurally here, so the art can be
re-tuned without a pixel editor. Run it from anywhere:

    python tools/generate_assets.py

Outputs into ../assets/ relative to this file:
    saiyanhair.png   6 hairstyles x (down / right / up), vanilla 16x32 cell layout
    aura.png         8-frame flame aura, row 0 = body (tinted), row 1 = white-hot core
    lightning.png    4-frame crackle overlay used by SSJ2 and above
    kamehameha.png   beam body / charge orb / impact burst, 4 frames each
    icons.png        6 buff icons

Hair is described as a "skyline": for each of the 16 columns in a cell, TOP is
the first row containing hair and BOT is the last. That gives direct control of
spike height at this sprite scale, which triangles do not.

Alignment note: hair occupies the same 16x32 space as the farmer body frame. If
a form sits a pixel high or low against your farmer, shift the TOP/BOT numbers
for that form and re-run; nothing else needs to change.
"""

import math
import os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets")
TOOLS = os.path.join(os.path.dirname(os.path.abspath(__file__)))

CELL_W, CELL_H = 16, 32

# palettes: (highlight, light, mid, dark, outline)
PALETTES = {
    "ssj":  ((255, 250, 205), (255, 226, 92), (238, 186, 28), (180, 126, 10), (104, 68, 4)),
    "ssj2": ((255, 253, 220), (255, 234, 112), (246, 198, 34), (188, 134, 12), (106, 70, 4)),
    "ssj3": ((255, 250, 200), (252, 222, 96), (232, 182, 24), (172, 120, 8), (96, 62, 2)),
    "god":  ((255, 228, 234), (255, 140, 170), (228, 62, 102), (152, 26, 60), (82, 10, 32)),
    "blue": ((228, 250, 255), (128, 226, 255), (46, 156, 230), (22, 90, 156), (8, 42, 82)),
    "ui":   ((255, 255, 255), (240, 248, 255), (196, 216, 238), (132, 154, 186), (62, 78, 102)),
    "mui":  ((255, 255, 255), (250, 252, 255), (222, 232, 246), (176, 192, 214), (96, 112, 138)),
}

_ = -1  # empty column

# HAIR[form][view] = (top_row_per_column, bottom_row_per_column)
HAIR = {
    "ssj": {
        "down": ([_, _, 6, 2, 1, 4, 1, 0, 0, 1, 4, 1, 2, 6, _, _],
                 [_, _, 12, 12, 11, 9, 8, 8, 8, 8, 9, 11, 12, 12, _, _]),
        "side": ([_, _, 5, 2, 0, 3, 0, 1, 3, 1, 3, 6, _, _, _, _],
                 [_, _, 13, 13, 13, 12, 11, 10, 9, 8, 7, 8, _, _, _, _]),
        "up":   ([_, _, 5, 2, 1, 3, 1, 0, 0, 1, 3, 1, 2, 5, _, _],
                 [_, _, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, _, _]),
    },
    "ssj2": {
        "down": ([_, _, 4, 1, 0, 3, 0, 0, 2, 0, 0, 3, 1, 4, _, _],
                 [_, _, 12, 12, 11, 9, 8, 8, 8, 8, 9, 11, 12, 12, _, _]),
        "side": ([_, _, 3, 0, 0, 2, 0, 0, 2, 0, 2, 5, _, _, _, _],
                 [_, _, 13, 13, 13, 12, 11, 10, 9, 8, 7, 8, _, _, _, _]),
        "up":   ([_, _, 3, 1, 0, 2, 0, 0, 1, 0, 0, 2, 1, 3, _, _],
                 [_, _, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, _, _]),
    },
    "ssj3": {
        "down": ([_, 8, 5, 1, 0, 0, 3, 0, 0, 3, 0, 0, 1, 5, 8, _],
                 [_, 26, 22, 12, 11, 9, 8, 8, 8, 8, 9, 11, 22, 26, _, _]),
        "side": ([_, 8, 4, 1, 0, 0, 2, 0, 1, 3, 5, _, _, _, _, _],
                 [_, 27, 25, 22, 18, 13, 12, 10, 9, 8, 8, _, _, _, _, _]),
        "up":   ([_, 7, 4, 1, 0, 0, 2, 0, 0, 2, 0, 0, 1, 4, 7, _],
                 [_, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, _]),
    },
    "god": {
        "down": ([_, _, 6, 2, 0, 3, 1, 0, 0, 1, 3, 0, 2, 6, _, _],
                 [_, _, 13, 13, 12, 10, 8, 8, 8, 8, 10, 12, 13, 13, _, _]),
        "side": ([_, _, 5, 2, 0, 2, 0, 1, 2, 1, 3, 6, _, _, _, _],
                 [_, _, 14, 14, 13, 12, 11, 10, 9, 8, 7, 8, _, _, _, _]),
        "up":   ([_, _, 5, 2, 0, 2, 0, 0, 0, 0, 2, 0, 2, 5, _, _],
                 [_, _, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, _, _]),
    },
    "blue": {
        "down": ([_, _, 5, 1, 0, 3, 1, 0, 0, 1, 3, 0, 1, 5, _, _],
                 [_, _, 12, 12, 11, 9, 8, 8, 8, 8, 9, 11, 12, 12, _, _]),
        "side": ([_, _, 4, 1, 0, 2, 0, 1, 2, 1, 3, 6, _, _, _, _],
                 [_, _, 13, 13, 13, 12, 11, 10, 9, 8, 7, 8, _, _, _, _]),
        "up":   ([_, _, 4, 1, 0, 2, 1, 0, 0, 1, 2, 0, 1, 4, _, _],
                 [_, _, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, _, _]),
    },
    "ui": {
        "down": ([_, _, 5, 2, 0, 2, 1, 0, 1, 1, 2, 0, 2, 5, _, _],
                 [_, _, 13, 13, 12, 10, 8, 8, 8, 8, 10, 12, 13, 13, _, _]),
        "side": ([_, _, 4, 1, 0, 1, 0, 1, 2, 2, 4, 7, _, _, _, _],
                 [_, _, 14, 14, 13, 12, 11, 10, 9, 8, 7, 8, _, _, _, _]),
        "up":   ([_, _, 4, 1, 0, 1, 0, 0, 0, 0, 1, 1, 2, 4, _, _],
                 [_, _, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, _, _]),
    },
    # Mastered Ultra Instinct: calmer than UI, swept back rather than spiked up
    "mui": {
        "down": ([_, _, 4, 2, 1, 1, 2, 2, 2, 2, 1, 1, 2, 4, _, _],
                 [_, _, 13, 13, 12, 10, 8, 8, 8, 8, 10, 12, 13, 13, _, _]),
        "side": ([_, _, 3, 1, 1, 2, 2, 2, 3, 3, 5, 7, _, _, _, _],
                 [_, _, 14, 14, 13, 12, 11, 10, 9, 8, 7, 8, _, _, _, _]),
        "up":   ([_, _, 3, 1, 1, 1, 2, 2, 2, 2, 1, 1, 2, 3, _, _],
                 [_, _, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, _, _]),
    },
}

FORMS = ["ssj", "ssj2", "ssj3", "god", "blue", "ui", "mui"]

DARKER = {0: 1, 1: 2, 2: 3, 3: 4, 4: 4}


# Columns trimmed from each side of every hair profile. The hand-tuned skylines
# below were a pixel wide on each edge, so the hair overhung the skull; insetting
# here keeps the tuned silhouette but squeezes it onto the head.
HAIR_INSET = 1


def inset_profile(top, bot, inset=HAIR_INSET):
    """Squeeze a hair skyline inward by `inset` columns per side, sampling the
    original profile so spike heights survive rather than being averaged away."""
    cols = [x for x in range(CELL_W) if top[x] >= 0]
    if inset <= 0 or not cols:
        return top, bot

    lo, hi = min(cols), max(cols)
    new_lo, new_hi = lo + inset, hi - inset
    if new_hi - new_lo < 2:
        return top, bot

    span_old = hi - lo
    span_new = new_hi - new_lo
    new_top = [-1] * CELL_W
    new_bot = [-1] * CELL_W

    for x in range(new_lo, new_hi + 1):
        t = (x - new_lo) / span_new
        src = lo + int(round(t * span_old))
        src = max(lo, min(hi, src))
        new_top[x] = top[src]
        new_bot[x] = bot[src]

    return new_top, new_bot


def _draw_hair_cell(img, ox, oy, top, bot, pal):
    """Shade a hair cell, then add the detail that makes it read as hair rather than
    a coloured mass: clump separation, a specular cluster, and a darker root band."""
    px = img.load()
    cols = [x for x in range(CELL_W) if top[x] >= 0]
    if not cols:
        return
    lo, hi_col = min(cols), max(cols)
    gtop = min(top[x] for x in cols)
    gbot = max(bot[x] for x in cols)

    # local peaks read as separate spikes, so clumps are split beside them
    peaks = {x for x in cols
             if all(top[x] <= top[n] for n in (x - 1, x + 1) if n in cols)}

    for x in cols:
        col_span = max(1, bot[x] - top[x])
        for y in range(top[x], bot[x] + 1):
            if not (0 <= y < CELL_H):
                continue
            depth = (y - top[x]) / col_span
            k = 0.6 * depth + 0.4 * ((y - gtop) / max(1, gbot - gtop))
            if k < 0.13:
                idx = 0
            elif k < 0.34:
                idx = 1
            elif k < 0.62:
                idx = 2
            else:
                idx = 3

            # clump separation: the valley between two spikes gets a shadow seam
            if x not in peaks and (x - 1 in peaks or x + 1 in peaks) and depth < 0.55:
                idx = DARKER[idx]

            # a darker band where the hair meets the head
            if 0.62 <= depth <= 0.78 and bot[x] < 16:
                idx = DARKER[idx]

            if y == bot[x] and bot[x] >= 7:
                idx = 4                       # grounded bottom edge
            elif x in (lo, hi_col):
                idx = DARKER[idx]             # silhouette edge

            if y >= 15 and x % 3 == 1:
                idx = DARKER[idx]             # strand separation in long hair

            px[ox + x, oy + y] = pal[idx] + (255,)

    # specular cluster, up and to the left, the way vanilla lights its hair
    lit_cols = [x for x in cols if x <= (lo + hi_col) // 2]
    for i, x in enumerate(lit_cols[:3]):
        y = top[x] + 1 + i
        if top[x] <= y <= bot[x] and 0 <= y < CELL_H:
            px[ox + x, oy + y] = pal[0] + (255,)

    # tip highlights: the very top pixel of each spike catches the light
    for x in peaks:
        y = top[x]
        if 0 <= y < CELL_H:
            px[ox + x, oy + y] = pal[0] + (255,)


# Forms that use the hand-authored, resized hair from tools/imported_hair.png
# (a 48x96 sheet with columns: ssj, ssg, ssb) instead of the procedural hair.
# ssj art covers both Super Saiyan and Super Saiyan 2; the rest keep procedural.
IMPORTED_HAIR = {
    0: 0,   # Super Saiyan       <- imported ssj  column
    1: 0,   # Super Saiyan 2     <- imported ssj  column
    2: 3,   # Super Saiyan 3     <- imported ssj3 column
    3: 1,   # Super Saiyan God   <- imported ssg  column
    4: 2,   # Super Saiyan Blue  <- imported ssb  column
    5: 4,   # Ultra Instinct     <- imported ui   column
    6: 5,   # Mastered Ultra Instinct <- imported mui column
}


def make_hair():
    img = Image.new("RGBA", (len(FORMS) * CELL_W, 96), (0, 0, 0, 0))
    for i, form in enumerate(FORMS):
        for j, view in enumerate(("down", "side", "up")):
            top, bot = inset_profile(*HAIR[form][view])
            _draw_hair_cell(img, i * CELL_W, j * CELL_H, top, bot, PALETTES[form])

    imported = os.path.join(TOOLS, "imported_hair.png")
    if os.path.exists(imported):
        src = Image.open(imported).convert("RGBA")
        for dst_col, src_col in IMPORTED_HAIR.items():
            art = src.crop((src_col * CELL_W, 0, src_col * CELL_W + CELL_W, 96))
            # clear the procedural cell first, then lay the imported art over it
            blank_col = Image.new("RGBA", (CELL_W, 96), (0, 0, 0, 0))
            img.paste(blank_col, (dst_col * CELL_W, 0))
            img.alpha_composite(art, (dst_col * CELL_W, 0))

    img.save(os.path.join(OUT, "saiyanhair.png"))


# ------------------------------------------------------------------ noise

def _hash(x, y, s):
    h = (int(x) * 374761393 + int(y) * 668265263 + s * 1442695041) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0


def _vnoise(x, y, period_y, s):
    """Value noise that wraps in y so the animation loops seamlessly."""
    x0, y0 = math.floor(x), math.floor(y)
    fx, fy = x - x0, y - y0
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)

    def at(ix, iy):
        return _hash(ix, iy % period_y, s)

    a = at(x0, y0) * (1 - fx) + at(x0 + 1, y0) * fx
    b = at(x0, y0 + 1) * (1 - fx) + at(x0 + 1, y0 + 1) * fx
    return a * (1 - fy) + b * fy


def _fbm(x, y, period_y, s):
    return (_vnoise(x, y, period_y, s) * 0.6
            + _vnoise(x * 2.1, y * 2.1, period_y * 2, s + 7) * 0.3
            + _vnoise(x * 4.3, y * 4.3, period_y * 4, s + 19) * 0.1)


# 32x56 so the aura draws at a clean 4x (128x224 on screen) against the
# farmer's 64x128, keeping every aura pixel aligned to the game's pixel grid.
AURA_W, AURA_H, AURA_FRAMES = 32, 56, 8
PERIOD = 16


def make_aura():
    """Edge-weighted flame shell with licking tongues, so the farmer stays visible."""
    img = Image.new("RGBA", (AURA_W * AURA_FRAMES, AURA_H * 2), (0, 0, 0, 0))
    px = img.load()
    cx = AURA_W / 2.0
    for f in range(AURA_FRAMES):
        scroll = f * (PERIOD / AURA_FRAMES)
        for x in range(AURA_W):
            # ragged upper limit -> separated tongues of flame
            tongue = 0.52 + 0.46 * _fbm(x * 0.30, -scroll * 1.4, PERIOD, 41)
            for y in range(AURA_H):
                t = (AURA_H - 1 - y) / (AURA_H - 1)
                if t > tongue:
                    continue
                half = 13.0 * (1.0 - t) ** 0.60 + 2.0
                d = abs(x + 0.5 - cx) / half
                if d > 1.15:
                    continue
                shell = math.exp(-((d - 0.80) ** 2) / 0.085)
                core = max(0.0, 1.0 - d * d) * 0.22
                n = _fbm(x * 0.22, y * 0.17 - scroll, PERIOD, 3)
                field = (shell * 0.95 + core) * (0.35 + 1.30 * n)
                field *= (1.0 - (t / max(0.05, tongue)) ** 2.0) ** 0.55
                field *= min(1.0, 0.30 + (AURA_H - 1 - y) / 4.0)   # soften at the feet
                if field <= 0.06:
                    continue
                a = min(1.0, field)
                alpha = int(min(210, (a ** 1.25) * 215))
                if alpha < 8:
                    continue
                v = int(200 + 55 * min(1.0, a * 1.3))
                px[f * AURA_W + x, y] = (255, v, max(150, v - 40), alpha)
                if a > 0.78:
                    px[f * AURA_W + x, AURA_H + y] = (
                        255, 255, 255, int(min(235, (a - 0.78) / 0.22 * 235)))
    img.save(os.path.join(OUT, "aura.png"))


LIT_W, LIT_H, LIT_FRAMES = AURA_W, AURA_H, 4


def make_lightning():
    img = Image.new("RGBA", (LIT_W * LIT_FRAMES, LIT_H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for f in range(LIT_FRAMES):
        ox = f * LIT_W
        for b in range(3):
            seed = f * 31 + b * 7
            x = 4 + _hash(seed, 1, 5) * (LIT_W - 8)
            y = 4 + _hash(seed, 2, 5) * (LIT_H - 26)
            pts = [(ox + x, y)]
            for step in range(5):
                x += (_hash(seed, step + 3, 11) - 0.5) * 11
                y += 3 + _hash(seed, step + 3, 13) * 5
                x = max(2, min(LIT_W - 3, x))
                pts.append((ox + x, y))
            d.line([(px_ + 1, py_) for px_, py_ in pts], fill=(190, 225, 255, 80), width=1)
            d.line(pts, fill=(255, 255, 255, 240), width=1)
    img.save(os.path.join(OUT, "lightning.png"))


def make_kamehameha():
    """128x96: row0 beam body, row1 charge orb, row2 impact burst (4 frames each)."""
    img = Image.new("RGBA", (128, 96), (0, 0, 0, 0))
    px = img.load()
    for f in range(4):
        ox = f * 32
        # beam body - stretched along the beam in game, so it must tile in x
        for y in range(32):
            dy = abs(y + 0.5 - 16.0)
            for x in range(32):
                ripple = 1.6 * math.sin((x / 32.0) * 2 * math.pi * 2 + f * math.pi / 2)
                edge = 13.0 + ripple
                if dy > edge:
                    continue
                k = 1.0 - (dy / edge)
                if k > 0.55:
                    px[ox + x, y] = (255, 255, 255, 255)
                else:
                    a = int(min(255, (k / 0.55) ** 1.1 * 235))
                    if a > 5:
                        px[ox + x, y] = (255, 255, 255, a)
        # charge orb
        pulse = 8.5 + 1.6 * math.sin(f * math.pi / 2)
        for y in range(32):
            for x in range(32):
                dist = math.hypot(x + 0.5 - 16, y + 0.5 - 16)
                if dist > pulse + 4:
                    continue
                k = max(0.0, 1.0 - dist / (pulse + 4))
                if dist < pulse * 0.55:
                    px[ox + x, 32 + y] = (255, 255, 255, 255)
                else:
                    a = int(min(255, (k ** 1.6) * 255))
                    if a > 6:
                        px[ox + x, 32 + y] = (255, 255, 255, a)
        # impact burst: expanding ring + short outward dashes + centre flash
        r = 3.5 + f * 3.2
        fade = 1.0 - f * 0.18
        for y in range(32):
            for x in range(32):
                dist = math.hypot(x + 0.5 - 16, y + 0.5 - 16)
                ring = abs(dist - r)
                if ring < 3.2:
                    a = int((1.0 - ring / 3.2) ** 1.2 * 255 * fade)
                    if a > 6:
                        px[ox + x, 64 + y] = (255, 255, 255, a)
                if f < 2 and dist < 5.5 - f * 2.0:
                    px[ox + x, 64 + y] = (255, 255, 255, 255)
        d = ImageDraw.Draw(img)
        for s in range(6):
            ang = s * math.pi / 3 + f * 0.25
            r0, r1 = r + 1.5, r + 4.0 + f
            d.line([(ox + 16 + math.cos(ang) * r0, 64 + 16 + math.sin(ang) * r0),
                    (ox + 16 + math.cos(ang) * r1, 64 + 16 + math.sin(ang) * r1)],
                   fill=(255, 255, 255, int(200 * fade)), width=1)
    img.save(os.path.join(OUT, "kamehameha.png"))


def make_disk():
    """4-frame energy disk for Destructo Disk. Spin comes from rotation at draw
    time, so the frames only need to carry the energy shimmer."""
    img = Image.new("RGBA", (128, 32), (0, 0, 0, 0))
    px = img.load()
    for f in range(4):
        ox = f * 32
        for y in range(32):
            for x in range(32):
                dx, dy = x + 0.5 - 16, y + 0.5 - 16
                dist = math.hypot(dx, dy * 2.6)          # squashed = seen edge-on
                ring = abs(dist - 12.5)
                if ring > 4.0:
                    continue
                k = 1.0 - (ring / 4.0)
                wobble = 0.75 + 0.25 * math.sin(math.atan2(dy, dx) * 6 + f * 1.6)
                a = int(min(255, (k ** 1.3) * 255 * wobble))
                if a < 8:
                    continue
                px[ox + x, y] = (255, 255, 255, a) if k > 0.62 else (210, 240, 255, a)
        # bright leading edge
        for s_ in range(2):
            ang = (f * 0.5) + s_ * math.pi
            ex = int(16 + math.cos(ang) * 12.5)
            ey = int(16 + math.sin(ang) * 12.5 / 2.6)
            if 0 <= ex < 32 and 0 <= ey < 32:
                px[ox + ex, ey] = (255, 255, 255, 255)
    img.save(os.path.join(OUT, "disk.png"))


def make_technique_icons():
    """16x16 glyphs for the equipped-technique chip, in technique registration order:
    beam, disc, solar flare, spirit bomb, instant transmission, kaioken."""
    img = Image.new("RGBA", (96, 16), (0, 0, 0, 0))
    px = img.load()

    def dot(ox, x, y, c):
        if 0 <= x < 16 and 0 <= y < 16:
            px[ox + x, y] = c

    W = (255, 255, 255, 255)
    S = (196, 226, 255, 220)
    R = (255, 96, 78, 255)
    RD = (188, 44, 36, 230)

    # 0: kamehameha beam
    for y in range(6, 10):
        for x in range(3, 15):
            dot(0, x, y, W if y in (7, 8) else S)
    for y in range(5, 11):
        for x in range(1, 5):
            if (x - 3) ** 2 + (y - 8) ** 2 <= 5:
                dot(0, x, y, W)

    # 1: destructo disc
    for y in range(16):
        for x in range(16):
            ring = abs(math.hypot(x + 0.5 - 8, (y + 0.5 - 8) * 2.2) - 6.0)
            if ring < 1.8:
                dot(16, x, y, W if ring < 0.9 else S)

    # 2: solar flare starburst
    for r in range(2, 8):
        for k in range(8):
            a = k * math.pi / 4
            dot(32, int(8 + math.cos(a) * r), int(8 + math.sin(a) * r), S if r > 4 else W)
    for y in range(6, 11):
        for x in range(6, 11):
            if (x - 8) ** 2 + (y - 8) ** 2 <= 4:
                dot(32, x, y, W)

    # 3: spirit bomb - big orb with sparks
    for y in range(16):
        for x in range(16):
            d = math.hypot(x + 0.5 - 8, y + 0.5 - 9)
            if d < 5.6:
                dot(48, x, y, W if d < 3.4 else S)
    for (x, y) in ((2, 3), (13, 4), (4, 1), (11, 1), (14, 9)):
        dot(48, x, y, W)

    # 4: instant transmission - two fingers and a swirl
    for y in range(2, 9):
        dot(64, 6, y, W)
        dot(64, 8, y, W)
    for k in range(10):
        a = k * 0.62
        r = 2.0 + k * 0.42
        dot(64, int(8 + math.cos(a) * r), int(11 + math.sin(a) * r * 0.6), S)

    # 5: kaioken - a red flame silhouette
    for y in range(16):
        t = (15 - y) / 15.0
        half = 5.6 * (1 - t) ** 0.55 + 0.8
        for x in range(16):
            d = abs(x + 0.5 - 8) / half
            if d > 1:
                continue
            lick = 0.72 + 0.28 * math.sin(x * 1.7 + y * 0.4)
            if t > lick:
                continue
            dot(80, x, y, R if d < 0.55 else RD)
    for (x, y) in ((8, 2), (6, 4), (10, 4)):
        dot(80, x, y, (255, 226, 180, 255))

    img.save(os.path.join(OUT, "technique_icons.png"))


STAR_LAYOUTS = {
    1: [(8, 8)],
    2: [(6, 6), (10, 10)],
    3: [(8, 5), (5, 10), (11, 10)],
    4: [(6, 6), (10, 6), (6, 10), (10, 10)],
    5: [(6, 5), (10, 5), (8, 8), (6, 11), (10, 11)],
    6: [(6, 4), (10, 4), (4, 8), (12, 8), (6, 12), (10, 12)],
    7: [(8, 3), (4, 7), (8, 7), (12, 7), (5, 11), (8, 14), (11, 11)],
}


def make_senzu():
    """16x16 senzu bean: a pale green pod with a highlight."""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = img.load()
    for y in range(16):
        for x in range(16):
            dx = (x + 0.5 - 8) / 4.6
            dy = (y + 0.5 - 8) / 6.2
            d = dx * dx + dy * dy
            if d > 1.0:
                continue
            lit = 1.0 - math.hypot(x + 0.5 - 6, y + 0.5 - 5) / 11.0
            if d > 0.82:
                c = (74, 104, 44)
            elif lit > 0.72:
                c = (226, 246, 176)
            elif lit > 0.52:
                c = (176, 220, 118)
            elif lit > 0.32:
                c = (132, 186, 82)
            else:
                c = (96, 142, 58)
            px[x, y] = c + (255,)
    # seam down the middle and a leaf nub on top
    for y in range(4, 12):
        px[8, y] = (86, 128, 52, 255)
    px[8, 1] = (96, 150, 60, 255)
    px[8, 2] = (120, 178, 70, 255)
    px[9, 2] = (120, 178, 70, 255)
    img.save(os.path.join(OUT, "senzu.png"))


def make_kibar():
    """Vanilla-style vertical bar chrome, sliced top / middle / bottom like the
    energy and health meters so it reads as part of the same HUD.
    Layout: 12x16 top cap, 12x12 repeating middle, 12x12 bottom cap."""
    img = Image.new("RGBA", (12, 40), (0, 0, 0, 0))
    px = img.load()

    OUT_L = (86, 51, 27, 255)      # outer dark edge
    WOOD = (196, 130, 68, 255)     # frame body
    LIGHT = (238, 190, 126, 255)   # top-left highlight
    SHADE = (140, 86, 42, 255)     # bottom-right shade
    HOLLOW = (54, 40, 30, 255)     # empty track

    def frame_block(y0, height, cap_top, cap_bottom):
        for y in range(y0, y0 + height):
            for x in range(12):
                edge_x = x in (0, 11)
                edge_y = (cap_top and y == y0) or (cap_bottom and y == y0 + height - 1)
                if edge_x or edge_y:
                    px[x, y] = OUT_L
                elif x == 1 or (cap_top and y == y0 + 1):
                    px[x, y] = LIGHT
                elif x == 10 or (cap_bottom and y == y0 + height - 2):
                    px[x, y] = SHADE
                elif x in (2, 9):
                    px[x, y] = WOOD
                else:
                    px[x, y] = HOLLOW

    frame_block(0, 16, True, False)    # top cap
    frame_block(16, 12, False, False)  # repeating middle
    frame_block(28, 12, False, True)   # bottom cap
    img.save(os.path.join(OUT, "kibar.png"))


def make_dragonballs():
    """7 balls, 16x16 each: orange sphere with 1-7 red stars."""
    img = Image.new("RGBA", (112, 16), (0, 0, 0, 0))
    px = img.load()
    for n in range(1, 8):
        ox = (n - 1) * 16
        for y in range(16):
            for x in range(16):
                dx, dy = x + 0.5 - 8, y + 0.5 - 8
                dist = math.hypot(dx, dy)
                if dist > 7.2:
                    continue
                # lit from upper-left
                lit = 1.0 - math.hypot(dx + 2.2, dy + 2.2) / 10.0
                lit = max(0.0, min(1.0, lit))
                if dist > 6.4:
                    c = (150, 78, 8)
                elif lit > 0.72:
                    c = (255, 238, 170)
                elif lit > 0.5:
                    c = (255, 200, 82)
                elif lit > 0.28:
                    c = (243, 158, 30)
                else:
                    c = (198, 108, 14)
                px[ox + x, y] = c + (255,)
        for (sx, sy) in STAR_LAYOUTS[n]:
            for (dx, dy) in ((0, 0), (0, -1), (0, 1), (-1, 0), (1, 0)):
                x, y = sx + dx, sy + dy
                if 0 <= x < 16 and 0 <= y < 16:
                    if math.hypot(x + 0.5 - 8, y + 0.5 - 8) <= 6.6:
                        px[ox + x, y] = (206, 32, 42, 255)
    img.save(os.path.join(OUT, "dragonballs.png"))


def make_icons():
    img = Image.new("RGBA", ((len(FORMS) + 1) * 16, 16), (0, 0, 0, 0))
    px = img.load()
    for i, form in enumerate(FORMS):
        pal = PALETTES[form]
        ox = i * 16
        for y in range(16):
            t = (15 - y) / 15.0
            half = 6.2 * (1 - t) ** 0.5 + 1.1
            for x in range(16):
                d = abs(x + 0.5 - 8) / half
                if d > 1:
                    continue
                k = 1 - d * d
                idx = 0 if k > 0.72 else 1 if k > 0.45 else 2 if k > 0.2 else 3
                px[ox + x, y] = pal[idx] + (int(min(255, 150 + 105 * k)),)
        for y in range(5, 11):
            for x in range(6, 10):
                if (x - 8) ** 2 + (y - 8) ** 2 <= 4:
                    px[ox + x, y] = (255, 255, 255, 255)
    # one extra cell on the end: the ki-exhaustion status icon, a guttering
    # grey flame with a crack through it
    ox = len(FORMS) * 16
    for y in range(16):
        t = (15 - y) / 15.0
        half = 5.0 * (1 - t) ** 0.6 + 1.0
        for x in range(16):
            d = abs(x + 0.5 - 8) / half
            if d > 1 or t > 0.62:
                continue
            k = 1 - d * d
            c = (150, 152, 158) if k > 0.6 else (96, 98, 106)
            px[ox + x, y] = c + (int(min(255, 150 + 90 * k)),)
    for (x, y) in ((8, 6), (7, 8), (9, 9), (8, 11), (7, 13)):
        px[ox + x, y] = (214, 72, 66, 255)

    img.save(os.path.join(OUT, "icons.png"))


def make_preview():
    """Upscaled sheets with a stand-in farmer so alignment can be eyeballed."""
    S = 8
    hair = Image.open(os.path.join(OUT, "saiyanhair.png")).convert("RGBA")
    body = Image.new("RGBA", hair.size, (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    for i in range(len(FORMS)):
        for j in range(3):
            ox, oy = i * 16, j * 32
            bd.ellipse([ox + 3, oy + 3, ox + 12, oy + 14], fill=(245, 205, 175, 255))
            bd.rectangle([ox + 4, oy + 15, ox + 11, oy + 24], fill=(90, 120, 180, 255))
            bd.rectangle([ox + 5, oy + 25, ox + 10, oy + 31], fill=(70, 80, 110, 255))
    pv = Image.new("RGBA", (hair.width * S, 96 * S), (40, 44, 56, 255))
    pv.alpha_composite(Image.alpha_composite(body, hair)
                       .resize((hair.width * S, 96 * S), Image.NEAREST))
    d = ImageDraw.Draw(pv)
    for i in range(1, len(FORMS)):
        d.line([(i * 16 * S, 0), (i * 16 * S, 96 * S)], fill=(255, 0, 0, 110))
    for j in range(1, 3):
        d.line([(0, j * 32 * S), (hair.width * S, j * 32 * S)], fill=(255, 0, 0, 110))
    pv.save(os.path.join(TOOLS, "preview_hair.png"))

    aura = Image.open(os.path.join(OUT, "aura.png"))
    ap = Image.new("RGBA", (aura.width * 3, aura.height * 3), (20, 22, 30, 255))
    ap.alpha_composite(aura.resize((aura.width * 3, aura.height * 3), Image.NEAREST))
    ap.save(os.path.join(TOOLS, "preview_aura.png"))

    kame = Image.open(os.path.join(OUT, "kamehameha.png"))
    kp = Image.new("RGBA", (kame.width * 4, kame.height * 4), (20, 22, 30, 255))
    kp.alpha_composite(kame.resize((kame.width * 4, kame.height * 4), Image.NEAREST))
    kp.save(os.path.join(TOOLS, "preview_kamehameha.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    make_hair()
    make_aura()
    make_lightning()
    # kamehameha.png and disk.png are hand-authored assets; do NOT regenerate them
    # (make_kamehameha / make_disk would clobber the hand-drawn art).
    # make_kamehameha()
    make_icons()
    # make_disk()  # hand-authored, see note above
    make_technique_icons()
    make_dragonballs()
    # NOTE: kibar.png is a hand-authored asset (assets/kibar.png). Do NOT regenerate
    # it here - make_kibar() would clobber the hand-drawn bar. Left callable for
    # reference only.
    # make_kibar()
    make_senzu()
    make_preview()
    for name in sorted(os.listdir(OUT)):
        p = os.path.join(OUT, name)
        print(f"{name:20s} {os.path.getsize(p):>7d} bytes")
