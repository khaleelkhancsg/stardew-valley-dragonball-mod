"""
Placeholder boss portraits for the dialogue boxes.

Each is a 64x64 tinted bust silhouette, one per speaking boss, so the NPC-style
dialogue boxes have a face immediately. They are deliberately plain - drop a
hand-drawn 64x64 portrait in over assets/portraits/<BossId>.png to replace one.

    python tools/generate_portraits.py
"""

import os
from PIL import Image, ImageDraw

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets", "portraits")

# BossId -> a representative colour (its aura), used to tint the placeholder.
COLOURS = {
    "_default": (150, 150, 160),
    "Saibamen": (120, 230, 90), "Guldo": (140, 210, 130), "Nappa": (230, 210, 130),
    "Jeice": (255, 110, 90), "EliteWarrior": (180, 120, 255), "Burter": (120, 180, 255),
    "Recoome": (255, 140, 90), "CaptainGinyu": (180, 120, 255),
    "FriezaFirst": (200, 150, 235), "CoolerFirst": (150, 230, 220),
    "FriezaFinal": (235, 205, 245), "CoolerFinal": (110, 210, 200),
    "CellImperfect": (120, 200, 120), "CellSemiPerfect": (140, 220, 140),
    "CellJuniors": (150, 210, 255), "CellPerfect": (140, 240, 140),
    "Bojack": (120, 230, 170), "Broly": (150, 240, 120), "Dabura": (200, 60, 60),
    "BuuFat": (255, 180, 220), "SuperBuu": (255, 130, 210), "BuuSuperGohan": (255, 120, 200),
    "MetalCoolerLegion": (170, 220, 255), "KidBuu": (255, 150, 210),
    "FriezaGolden": (255, 215, 90), "FriezaBlack": (150, 80, 190),
    "Destroyer": (190, 120, 255),
}


def darken(c, f):
    return (int(c[0] * f), int(c[1] * f), int(c[2] * f), 255)


def lighten(c, f):
    return (min(255, int(c[0] + (255 - c[0]) * f)),
            min(255, int(c[1] + (255 - c[1]) * f)),
            min(255, int(c[2] + (255 - c[2]) * f)), 255)


def portrait(colour):
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    base = (colour[0], colour[1], colour[2], 255)
    dark = darken(colour, 0.55)
    lite = lighten(colour, 0.35)
    outline = darken(colour, 0.3)

    # shoulders
    d.polygon([(10, 63), (18, 44), (46, 44), (54, 63)], fill=dark, outline=outline)
    # neck
    d.rectangle([28, 40, 36, 48], fill=base)
    # head
    d.ellipse([18, 10, 46, 42], fill=base, outline=outline)
    # cheek light
    d.ellipse([22, 15, 34, 30], fill=lite)
    # eyes
    d.rectangle([25, 24, 28, 27], fill=(30, 30, 34, 255))
    d.rectangle([36, 24, 39, 27], fill=(30, 30, 34, 255))
    # brow
    d.line([(23, 22), (30, 21)], fill=outline)
    d.line([(34, 21), (41, 22)], fill=outline)
    return img


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for name, colour in COLOURS.items():
        portrait(colour).save(os.path.join(OUT, name + ".png"))
    print(f"wrote {len(COLOURS)} portraits to {OUT}")
