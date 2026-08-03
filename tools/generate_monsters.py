"""
Boss sprite generator for the Saiyan Transformations mod.

Produces replacement sprite sheets for the monsters the bosses are built from, so
a Saibaman is an actual Saibaman rather than a green slime with an aura.

    python tools/generate_monsters.py

Sheet dimensions are matched to vanilla exactly. That matters: the game turns a
frame index into a source rect using the *texture width*, so a sheet of the wrong
width scrambles every animation. The sizes below were measured from vanilla
reskin mods, which by definition match the originals:

    Green Slime   64x168   frames 16x24   (4 cols x 7 rows)  - verified by alpha bands
    Shadow Brute  64x256   frames 16x32   (4 cols x 8 rows)
    Skeleton      64x192   frames 16x32   (4 cols x 6 rows)
    Mummy         64x160   frames 16x32   (4 cols x 5 rows)
    Squid Kid     64x96    frames 16x24   (4 cols x 4 rows)

Every cell of every sheet is filled. Monster classes use different frame indices
for walking, attacking and dying, and rather than guess which index means what,
filling the whole grid guarantees there is never a blank frame on screen.

Row convention, repeated to fill the sheet:
    row 0  facing down    (4-frame walk)
    row 1  facing right   (4-frame walk)
    row 2  facing up      (4-frame walk)
    row 3+ repeats, with the last row used for a hurt/attack pose
"""

import math
import os

from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets", "monsters")
TOOLS = os.path.dirname(os.path.abspath(__file__))


def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


class Canvas:
    """Tiny pixel helper with bounds checking, so frame drawing stays readable."""

    def __init__(self, img, ox, oy, w, h):
        self.px = img.load()
        self.ox, self.oy, self.w, self.h = ox, oy, w, h

    def set(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h and c is not None:
            self.px[self.ox + x, self.oy + y] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, c)

    def hline(self, x0, x1, y, c):
        for x in range(int(x0), int(x1) + 1):
            self.set(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c):
        for y in range(int(cy - ry), int(cy + ry) + 1):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                dx = (x + 0.5 - cx) / max(0.5, rx)
                dy = (y + 0.5 - cy) / max(0.5, ry)
                if dx * dx + dy * dy <= 1.0:
                    self.set(x, y, c)

    def outline_pass(self, colour, mask):
        """Darken the silhouette edge, given the set of filled pixels."""
        for (x, y) in list(mask):
            for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
                if (x + dx, y + dy) not in mask:
                    self.set(x, y, colour)
                    break


def outline_cell(img, ox, oy, w, h, colour):
    """Grow a 1px dark border outward from the silhouette. Stardew outlines nearly
    every sprite this way, and its absence is the main thing that made these read
    as flat blocks rather than characters."""
    px = img.load()
    filled = set()
    for y in range(h):
        for x in range(w):
            if px[ox + x, oy + y][3] > 0:
                filled.add((x, y))

    border = set()
    for (x, y) in filled:
        for dx, dy in ((0, -1), (0, 1), (-1, 0), (1, 0)):
            n = (x + dx, y + dy)
            if n not in filled and 0 <= n[0] < w and 0 <= n[1] < h:
                border.add(n)

    for (x, y) in border:
        px[ox + x, oy + y] = colour


# --------------------------------------------------------------- saibaman

SAIBA = {
    "skin": (104, 186, 62, 255),
    "skin_lit": (146, 220, 96, 255),
    "skin_dark": (62, 128, 40, 255),
    "outline": (28, 62, 22, 255),
    "eye": (232, 232, 216, 255),
    "pupil": (28, 28, 28, 255),
    "mouth": (150, 40, 40, 255),
}


def saibaman_frame(img, ox, oy, facing, step, pal=SAIBA):
    """16x24 cell. Squat humanoid with a bulbous head and a scowl. The palette is
    swappable so the same body doubles as Saibamen, Cell Juniors, ambushers, etc."""
    c = Canvas(img, ox, oy, 16, 24)
    p = pal
    bob = (0, -1, 0, 1)[step % 4]
    swing = (0, 1, 0, -1)[step % 4]

    # legs
    leg_y0, leg_y1 = 18, 22
    if facing == 1:      # side view: legs stride
        c.rect(6 - swing, leg_y0, 7 - swing, leg_y1, p["skin_dark"])
        c.rect(9 + swing, leg_y0, 10 + swing, leg_y1, p["skin"])
    else:
        c.rect(5, leg_y0, 6, leg_y1, p["skin_dark"])
        c.rect(9, leg_y0, 10, leg_y1, p["skin"])

    # torso
    c.rect(5, 12 + bob, 10, 18 + bob, p["skin"])
    c.hline(5, 10, 12 + bob, p["skin_lit"])
    c.rect(5, 17 + bob, 10, 18 + bob, p["skin_dark"])

    # arms
    if facing == 1:
        c.rect(4 + swing, 13 + bob, 5 + swing, 17 + bob, p["skin_dark"])
        c.rect(10 - swing, 13 + bob, 11 - swing, 17 + bob, p["skin"])
    else:
        c.rect(3, 13 + bob + abs(swing), 4, 17 + bob, p["skin_dark"])
        c.rect(11, 13 + bob - abs(swing), 12, 17 + bob, p["skin"])

    # head: wide, flat-topped, slightly domed
    c.ellipse(8, 7 + bob, 5.2, 4.6, p["skin"])
    c.ellipse(7, 6 + bob, 3.4, 2.8, p["skin_lit"])
    c.hline(4, 11, 11 + bob, p["skin_dark"])

    # face
    if facing == 2 or facing == 0:      # down / up
        if facing == 2:
            c.rect(5, 6 + bob, 6, 7 + bob, p["eye"])
            c.rect(9, 6 + bob, 10, 7 + bob, p["eye"])
            c.set(6, 7 + bob, p["pupil"])
            c.set(9, 7 + bob, p["pupil"])
            c.hline(6, 9, 9 + bob, p["mouth"])
    else:                                # side
        c.rect(9, 6 + bob, 11, 7 + bob, p["eye"])
        c.set(11, 7 + bob, p["pupil"])
        c.hline(9, 11, 9 + bob, p["mouth"])

    # crown ridges, the detail that separates it from a slime
    for rx in (5, 8, 11):
        c.set(rx, 2 + bob, p["skin_dark"])
        c.set(rx, 3 + bob, p["skin"])

    # brow ridge and a seam down the centre of the skull
    if facing == 2:
        c.hline(4, 11, 5 + bob, p["skin_dark"])
        c.set(8, 4 + bob, p["skin_dark"])
    elif facing == 1:
        c.hline(8, 12, 5 + bob, p["skin_dark"])

    # belly plating, so the torso is not one flat block
    c.hline(6, 9, 14 + bob, p["skin_lit"])
    c.set(8, 16 + bob, p["skin_dark"])
    c.set(5, 13 + bob, p["skin_dark"])
    c.set(10, 13 + bob, p["skin_dark"])

    # splayed toes
    if facing != 1:
        for fx in (5, 9):
            c.set(fx, 22, p["skin_dark"])
            c.set(fx + 1, 22, p["skin_dark"])


# --------------------------------------------------------------- humanoids

def humanoid_frame(img, ox, oy, facing, step, pal, opts):
    """16x32 cell. Parameterised warrior: armour colours, hair, cape, visor."""
    c = Canvas(img, ox, oy, 16, 32)
    bob = (0, -1, 0, 1)[step % 4]
    swing = (0, 1, 0, -1)[step % 4]

    skin = pal["skin"]
    suit = pal["suit"]
    suit_lit = pal["suit_lit"]
    suit_dark = pal["suit_dark"]
    trim = pal["trim"]
    hair = pal.get("hair")
    hair_lit = pal.get("hair_lit", hair)

    # boots and legs
    if facing == 1:
        c.rect(6 - swing, 24, 7 - swing, 30, suit_dark)
        c.rect(9 + swing, 24, 10 + swing, 30, suit)
        c.hline(5 - swing, 7 - swing, 31, trim)
        c.hline(9 + swing, 11 + swing, 31, trim)
    else:
        c.rect(5, 24, 7, 30, suit_dark)
        c.rect(8, 24, 10, 30, suit)
        c.hline(5, 7, 31, trim)
        c.hline(8, 10, 31, trim)

    # torso armour
    c.rect(4, 15 + bob, 11, 24, suit)
    c.hline(4, 11, 15 + bob, suit_lit)
    c.rect(4, 22, 11, 24, suit_dark)
    if opts.get("chest_plate"):
        c.rect(6, 17 + bob, 9, 20 + bob, trim)

    # shoulders
    c.rect(3, 15 + bob, 4, 18 + bob, suit_dark)
    c.rect(11, 15 + bob, 12, 18 + bob, suit_lit)

    # arms
    if facing == 1:
        c.rect(4 + swing, 17 + bob, 5 + swing, 23 + bob, suit_dark)
        c.rect(10 - swing, 17 + bob, 11 - swing, 23 + bob, suit)
        c.rect(4 + swing, 22 + bob, 5 + swing, 23 + bob, skin)
    else:
        c.rect(2, 17 + bob + abs(swing), 3, 22 + bob, suit_dark)
        c.rect(12, 17 + bob - abs(swing), 13, 22 + bob, suit)
        c.rect(2, 21 + bob + abs(swing), 3, 22 + bob, skin)
        c.rect(12, 21 + bob - abs(swing), 13, 22 + bob, skin)

    # head
    c.ellipse(8, 10 + bob, 4.0, 4.4, skin)
    if opts.get("visor"):
        c.rect(4, 9 + bob, 12, 10 + bob, pal["visor"])
        c.set(11, 9 + bob, (255, 255, 255, 255))
    elif facing == 2:
        c.set(6, 10 + bob, (30, 26, 30, 255))
        c.set(10, 10 + bob, (30, 26, 30, 255))
    elif facing == 1:
        c.set(10, 10 + bob, (30, 26, 30, 255))

    # hair: flat cap plus upward spikes, same skyline idea as the player hair
    if hair:
        c.ellipse(8, 8 + bob, 4.4, 3.0, hair)
        c.ellipse(7, 7 + bob, 3.0, 1.8, hair_lit)
        for (sx, top) in opts.get("spikes", [(4, 3), (6, 1), (8, 0), (10, 1), (12, 3)]):
            for y in range(top + bob, 8 + bob):
                c.set(sx, y, hair if y > top + bob else hair_lit)

    if opts.get("cape"):
        cape = pal.get("cape", suit_dark)
        c.rect(3, 15 + bob, 3, 26, cape)
        c.rect(12, 15 + bob, 12, 26, cape)

    # ---- detail pass: what stops it reading as coloured rectangles ----

    c.hline(4, 11, 23, suit_dark)          # belt
    c.set(7, 23, trim)                     # buckle
    c.set(8, 23, trim)

    if facing == 1:                        # boot cuffs
        c.hline(6 - swing, 7 - swing, 27, suit_dark)
        c.hline(9 + swing, 10 + swing, 27, suit_dark)
    else:
        c.hline(5, 7, 27, suit_dark)
        c.hline(8, 10, 27, suit_dark)

    if facing != 1:                        # glove cuffs
        c.hline(2, 3, 20 + bob, trim)
        c.hline(12, 13, 20 + bob, trim)

    c.hline(4, 11, 16 + bob, suit_dark)    # chest seam
    c.set(7, 15 + bob, trim)               # collar studs
    c.set(8, 15 + bob, trim)

    # eyes with whites and pupils rather than two dark dots
    if not opts.get("visor"):
        if facing == 2:
            c.rect(5, 9 + bob, 6, 10 + bob, (240, 240, 236, 255))
            c.rect(9, 9 + bob, 10, 10 + bob, (240, 240, 236, 255))
            c.set(6, 10 + bob, (32, 28, 34, 255))
            c.set(9, 10 + bob, (32, 28, 34, 255))
            c.hline(6, 9, 12 + bob, pal.get("mouth", (150, 92, 84, 255)))
        elif facing == 1:
            c.rect(9, 9 + bob, 11, 10 + bob, (240, 240, 236, 255))
            c.set(11, 10 + bob, (32, 28, 34, 255))


PALETTES = {
    "elite": {                      # Elite Saiyan Warrior (Shadow Brute)
        "skin": (226, 178, 132, 255),
        "suit": (58, 62, 104, 255),
        "suit_lit": (92, 98, 150, 255),
        "suit_dark": (32, 34, 62, 255),
        "trim": (206, 172, 76, 255),
        "hair": (34, 30, 34, 255),
        "hair_lit": (72, 66, 72, 255),
        "visor": (86, 214, 126, 255),
        "outline": (18, 18, 34, 255),
        "mouth": (150, 92, 84, 255),
    },
    "blade": {                      # Blade Adepts (Skeleton)
        "skin": (214, 216, 226, 255),
        "suit": (92, 108, 140, 255),
        "suit_lit": (140, 162, 198, 255),
        "suit_dark": (52, 62, 88, 255),
        "trim": (120, 226, 232, 255),
        "hair": (206, 216, 232, 255),
        "hair_lit": (240, 248, 255, 255),
        "outline": (34, 42, 62, 255),
        "mouth": (120, 130, 150, 255),
    },
    "android": {                    # Perfect Android (Mummy)
        "skin": (150, 220, 150, 255),
        "suit": (58, 120, 72, 255),
        "suit_lit": (96, 176, 108, 255),
        "suit_dark": (30, 68, 44, 255),
        "trim": (32, 34, 38, 255),
        "hair": (30, 34, 30, 255),
        "hair_lit": (64, 78, 64, 255),
        "outline": (14, 32, 20, 255),
        "mouth": (60, 96, 66, 255),
    },
    "adept": {                      # Ki Adepts (Squid Kid)
        "skin": (238, 214, 172, 255),
        "suit": (206, 148, 58, 255),
        "suit_lit": (244, 196, 104, 255),
        "suit_dark": (140, 92, 30, 255),
        "trim": (250, 240, 200, 255),
        "hair": (60, 44, 30, 255),
        "hair_lit": (104, 78, 52, 255),
        "cape": (168, 108, 40, 255),
        "outline": (54, 30, 12, 255),
        "mouth": (150, 92, 60, 255),
    },

    # ---- side bosses: placeholder palettes, distinct at a glance -------------
    "nappa": {                      # bald tan bruiser, dark saiyan armour
        "skin": (214, 168, 120, 255), "suit": (44, 46, 58, 255),
        "suit_lit": (78, 82, 100, 255), "suit_dark": (24, 26, 36, 255),
        "trim": (198, 172, 96, 255), "outline": (14, 14, 22, 255),
        "mouth": (140, 84, 72, 255),
    },
    "cooler": {                     # icy purple-white tyrant
        "skin": (208, 210, 232, 255), "suit": (120, 96, 168, 255),
        "suit_lit": (170, 150, 214, 255), "suit_dark": (72, 54, 110, 255),
        "trim": (206, 236, 244, 255), "outline": (40, 28, 62, 255),
        "mouth": (120, 100, 150, 255),
    },
    "recoome": {                    # orange hair, blue-purple suit
        "skin": (232, 190, 150, 255), "suit": (86, 78, 150, 255),
        "suit_lit": (128, 120, 200, 255), "suit_dark": (48, 42, 92, 255),
        "trim": (238, 150, 60, 255), "hair": (238, 128, 44, 255),
        "hair_lit": (255, 176, 90, 255), "outline": (34, 26, 60, 255),
        "mouth": (150, 92, 84, 255),
    },
    "dabura": {                     # red demon king, pale armour
        "skin": (196, 66, 60, 255), "suit": (208, 200, 188, 255),
        "suit_lit": (238, 232, 222, 255), "suit_dark": (140, 132, 120, 255),
        "trim": (150, 40, 40, 255), "hair": (30, 26, 30, 255),
        "hair_lit": (66, 58, 66, 255), "outline": (54, 16, 16, 255),
        "mouth": (90, 24, 24, 255),
    },
    "bojack": {                     # teal skin, red-orange hair
        "skin": (96, 176, 168, 255), "suit": (60, 70, 84, 255),
        "suit_lit": (96, 112, 132, 255), "suit_dark": (34, 42, 52, 255),
        "trim": (222, 96, 60, 255), "hair": (226, 92, 56, 255),
        "hair_lit": (255, 140, 96, 255), "outline": (20, 44, 42, 255),
        "mouth": (60, 110, 104, 255),
    },
    "broly": {                      # legendary green aura, dark green hair
        "skin": (224, 188, 150, 255), "suit": (70, 120, 74, 255),
        "suit_lit": (110, 176, 112, 255), "suit_dark": (38, 70, 42, 255),
        "trim": (230, 224, 140, 255), "hair": (44, 84, 46, 255),
        "hair_lit": (96, 160, 96, 255), "outline": (18, 40, 22, 255),
        "mouth": (150, 92, 84, 255),
    },
    "destroyer": {                  # violet god of destruction
        "skin": (150, 118, 196, 255), "suit": (58, 44, 90, 255),
        "suit_lit": (100, 80, 148, 255), "suit_dark": (34, 24, 56, 255),
        "trim": (236, 208, 120, 255), "hair": (28, 22, 40, 255),
        "hair_lit": (64, 52, 88, 255), "outline": (24, 16, 40, 255),
        "mouth": (110, 84, 150, 255),
    },
    "superbuu": {                   # tall pink majin (Mummy 16x32)
        "skin": (236, 150, 200, 255), "suit": (222, 224, 232, 255),
        "suit_lit": (250, 252, 255, 255), "suit_dark": (150, 152, 164, 255),
        "trim": (196, 60, 130, 255), "hair": (236, 150, 200, 255),
        "hair_lit": (255, 190, 226, 255), "outline": (110, 34, 78, 255),
        "mouth": (150, 40, 90, 255),
    },
    "metalcooler": {                # silver metal legion (Mummy 16x32)
        "skin": (206, 212, 224, 255), "suit": (120, 132, 150, 255),
        "suit_lit": (176, 188, 206, 255), "suit_dark": (72, 82, 98, 255),
        "trim": (150, 220, 232, 255), "hair": (90, 100, 116, 255),
        "hair_lit": (140, 152, 170, 255), "visor": (150, 226, 236, 255),
        "outline": (36, 44, 56, 255), "mouth": (110, 122, 140, 255),
    },
    "kidbuu": {                     # feral pink majin (Mummy 16x32)
        "skin": (240, 130, 190, 255), "suit": (210, 96, 150, 255),
        "suit_lit": (246, 150, 196, 255), "suit_dark": (150, 52, 104, 255),
        "trim": (255, 214, 236, 255), "hair": (240, 130, 190, 255),
        "hair_lit": (255, 180, 220, 255), "outline": (110, 30, 74, 255),
        "mouth": (150, 36, 92, 255),
    },
    "invader": {                    # multiversal invader: void violet, gold trim
        "skin": (188, 154, 224, 255), "suit": (40, 30, 66, 255),
        "suit_lit": (86, 66, 132, 255), "suit_dark": (20, 14, 38, 255),
        "trim": (240, 206, 96, 255), "hair": (150, 92, 230, 255),
        "hair_lit": (200, 150, 255, 255), "visor": (176, 120, 255, 255),
        "outline": (16, 10, 30, 255), "mouth": (120, 80, 160, 255),
    },
    "friezalord": {                 # Frieza's final form: white body, purple biogems
        "skin": (238, 238, 244, 255), "suit": (146, 82, 168, 255),
        "suit_lit": (190, 130, 208, 255), "suit_dark": (96, 48, 116, 255),
        "trim": (214, 96, 150, 255), "hair": None,
        "outline": (60, 30, 74, 255), "mouth": (150, 70, 110, 255),
    },
    "frieza": {                     # small pale-purple soldier (Squid Kid short)
        "skin": (224, 216, 232, 255), "suit": (128, 70, 150, 255),
        "suit_lit": (176, 116, 198, 255), "suit_dark": (78, 40, 96, 255),
        "trim": (232, 214, 244, 255), "outline": (44, 22, 56, 255),
        "mouth": (120, 70, 130, 255),
    },
}

OPTS = {
    "elite": {"chest_plate": True, "visor": True,
              "spikes": [(4, 4), (6, 2), (8, 1), (10, 2), (12, 4)]},
    "blade": {"chest_plate": False,
              "spikes": [(5, 3), (8, 2), (11, 3)]},
    "android": {"chest_plate": True,
                "spikes": [(5, 4), (8, 3), (11, 4)]},
    "adept": {"chest_plate": False, "cape": True,
              "spikes": [(6, 4), (8, 3), (10, 4)]},

    # ---- side bosses -------------------------------------------------------
    "nappa": {"chest_plate": True},                         # bald
    "cooler": {"chest_plate": True,
               "spikes": [(4, 3), (8, 0), (12, 3)]},
    "recoome": {"chest_plate": False,
                "spikes": [(4, 3), (6, 1), (8, 0), (10, 1), (12, 3)]},
    "dabura": {"chest_plate": True, "cape": True,
               "spikes": [(6, 3), (8, 2), (10, 3)]},
    "bojack": {"chest_plate": False,
               "spikes": [(4, 3), (6, 1), (8, 0), (10, 1), (12, 3)]},
    "broly": {"chest_plate": False,
              "spikes": [(3, 3), (6, 0), (8, -1), (10, 0), (13, 3)]},
    "destroyer": {"chest_plate": True, "cape": True,
                  "spikes": [(6, 3), (8, 2), (10, 3)]},
    "superbuu": {"chest_plate": False,
                 "spikes": [(8, 0), (7, 2), (9, 2)]},
    "metalcooler": {"chest_plate": True, "visor": True,
                    "spikes": [(4, 3), (8, 0), (12, 3)]},
    "kidbuu": {"chest_plate": False,
               "spikes": [(8, 0)]},
    "friezalord": {"chest_plate": True,
                   "spikes": [(5, 3), (8, 1), (11, 3)]},
    "invader": {"chest_plate": True, "visor": True, "cape": True,
                "spikes": [(3, 3), (5, 1), (8, -1), (11, 1), (13, 3)]},
    "frieza": {"chest_plate": True},
}


def humanoid_short_frame(img, ox, oy, facing, step, pal, opts):
    """16x24 cell. A hooded, hovering adept - deliberately not a shrunken warrior,
    since squashing the 16x32 body just mangles it."""
    c = Canvas(img, ox, oy, 16, 24)
    hover = (0, -1, -2, -1)[step % 4]

    suit = pal["suit"]
    suit_lit = pal["suit_lit"]
    suit_dark = pal["suit_dark"]
    trim = pal["trim"]
    skin = pal["skin"]

    # robe flares out at the bottom instead of legs
    for i, y in enumerate(range(14 + hover, 22 + hover)):
        spread = 3 + (i // 2)
        c.hline(8 - spread, 7 + spread, y, suit if i % 2 else suit_dark)
    c.hline(4, 11, 22 + hover, suit_dark)

    # torso and shoulders
    c.rect(5, 10 + hover, 10, 15 + hover, suit)
    c.hline(5, 10, 10 + hover, suit_lit)
    if opts.get("chest_plate"):
        c.rect(7, 12 + hover, 8, 14 + hover, trim)

    # sleeves
    if facing == 1:
        c.rect(4, 11 + hover, 5, 15 + hover, suit_dark)
        c.rect(10, 11 + hover, 11, 15 + hover, suit)
    else:
        c.rect(3, 11 + hover, 4, 15 + hover, suit_dark)
        c.rect(11, 11 + hover, 12, 15 + hover, suit)
        c.rect(3, 14 + hover, 4, 15 + hover, skin)
        c.rect(11, 14 + hover, 12, 15 + hover, skin)

    # hood with a shadowed face
    c.ellipse(8, 6 + hover, 4.2, 4.0, suit)
    c.ellipse(7, 5 + hover, 2.8, 2.4, suit_lit)
    c.ellipse(8, 7 + hover, 2.8, 2.4, (26, 22, 26, 255))
    if facing == 2:
        c.set(7, 7 + hover, trim)
        c.set(9, 7 + hover, trim)
    elif facing == 1:
        c.set(10, 7 + hover, trim)

    # ki motes orbiting the hands
    phase = step % 4
    if facing != 0:
        c.set(2 + (phase % 2), 13 + hover, trim)
        c.set(13 - (phase % 2), 12 + hover, trim)


# --------------------------------------------------------------- sheets

# swappable slime-body palettes for saibaman-kind sheets
SLIME_PALETTES = {
    "ambush": {                     # murky ambush green
        "skin": (120, 168, 84, 255), "skin_lit": (162, 208, 112, 255),
        "skin_dark": (74, 112, 52, 255), "outline": (30, 54, 24, 255),
        "eye": (232, 232, 216, 255), "pupil": (28, 28, 28, 255),
        "mouth": (150, 40, 40, 255),
    },
    "celljr": {                     # Cell Junior blue
        "skin": (86, 158, 232, 255), "skin_lit": (140, 198, 250, 255),
        "skin_dark": (48, 96, 168, 255), "outline": (22, 44, 86, 255),
        "eye": (30, 30, 30, 255), "pupil": (236, 120, 60, 255),
        "mouth": (30, 40, 70, 255),
    },
}

SHEETS = {
    "saibaman":  dict(size=(64, 168), frame=(16, 24), kind="saibaman"),
    "elite":     dict(size=(64, 256), frame=(16, 32), kind="humanoid"),
    "blade":     dict(size=(64, 192), frame=(16, 32), kind="humanoid"),
    "android":   dict(size=(64, 160), frame=(16, 32), kind="humanoid"),
    "adept":     dict(size=(64, 96),  frame=(16, 24), kind="humanoid_short"),

    # ---- placeholder sheets still generated (no hand art yet) --------------
    # Bosses that now use hand-drawn sheets (nappa, recoome, dabura, bojack,
    # broly, destroyer, superbuu, metalcooler, celljr, and the whole Frieza/
    # Cooler/Cell/Buu/Ginyu roster) were removed here so re-running the
    # generator can never overwrite that art. Only true placeholders remain.
    "invader":   dict(size=(64, 256), frame=(16, 32), kind="humanoid"),
    "kidbuu":    dict(size=(64, 160), frame=(16, 32), kind="humanoid"),
}

# which facing each row represents, cycling so every row of every sheet is filled
ROW_FACING = [2, 1, 0, 2, 1, 0, 2, 1]


def build_sheet(name, spec):
    w, h = spec["size"]
    fw, fh = spec["frame"]
    cols, rows = w // fw, h // fh
    img = blank(w, h)

    for row in range(rows):
        facing = ROW_FACING[row % len(ROW_FACING)]
        for col in range(cols):
            ox, oy = col * fw, row * fh
            if spec["kind"] == "saibaman":
                pal = spec.get("pal", SAIBA)
                saibaman_frame(img, ox, oy, facing, col, pal)
                outline_cell(img, ox, oy, fw, fh, pal["outline"])
            elif spec["kind"] == "humanoid_short":
                humanoid_short_frame(img, ox, oy, facing, col,
                                     PALETTES[name], OPTS[name])
                outline_cell(img, ox, oy, fw, fh, PALETTES[name]["outline"])
            else:
                humanoid_frame(img, ox, oy, facing, col,
                               PALETTES[name], OPTS[name])
                outline_cell(img, ox, oy, fw, fh, PALETTES[name]["outline"])

    img.save(os.path.join(OUT, name + ".png"))
    return img


def make_preview(sheets):
    scale = 5
    pad = 8
    total_w = sum(im.width for im in sheets.values()) + pad * (len(sheets) + 1)
    total_h = max(im.height for im in sheets.values()) + pad * 2
    pv = Image.new("RGBA", (total_w * scale, total_h * scale), (38, 42, 52, 255))
    x = pad
    for name, im in sheets.items():
        up = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        pv.alpha_composite(up, (x * scale, pad * scale))
        x += im.width + pad
    pv.save(os.path.join(TOOLS, "preview_monsters.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    built = {}
    for name, spec in SHEETS.items():
        built[name] = build_sheet(name, spec)
        print(f"{name + '.png':16s} {spec['size']}  frames {spec['frame']}")
    make_preview(built)
    print("preview ->", os.path.join(TOOLS, "preview_monsters.png"))
