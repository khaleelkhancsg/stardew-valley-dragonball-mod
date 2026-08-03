"""Builds a single labelled contact sheet of every asset the mod ships.

    python tools/make_artboard.py   ->  tools/artboard.png
"""

import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.join(HERE, "..", "assets")

BG = (30, 33, 42, 255)
PANEL = (40, 44, 56, 255)
TEXT = (232, 238, 248, 255)
DIM = (150, 160, 178, 255)
ACCENT = (255, 214, 78, 255)


def font(size, bold=False):
    for name in (("arialbd.ttf", "arial.ttf") if bold else ("arial.ttf",)):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


F_TITLE = font(34, True)
F_HEAD = font(21, True)
F_BODY = font(15)


def load(rel):
    return Image.open(os.path.join(ASSETS, rel)).convert("RGBA")


def up(img, scale):
    return img.resize((img.width * scale, img.height * scale), Image.NEAREST)


def main():
    monsters = [
        ("Saibaman", "monsters/saibaman.png", 24),
        ("Elite Saiyan Warrior", "monsters/elite.png", 32),
        ("Blade Adept", "monsters/blade.png", 32),
        ("Perfect Android", "monsters/android.png", 32),
        ("Ki Adept", "monsters/adept.png", 24),
    ]

    W = 1820
    canvas = Image.new("RGBA", (W, 2100), BG)
    d = ImageDraw.Draw(canvas)

    d.text((40, 30), "Saiyan Transformations - asset sheet", font=F_TITLE, fill=TEXT)
    d.text((42, 76), "every pixel generated procedurally by tools/*.py", font=F_BODY, fill=DIM)

    y = 120

    # ---- boss sprites: show the three facing rows of each sheet
    d.text((40, y), "Boss sprites   (per-instance sheets, vanilla dimensions)",
           font=F_HEAD, fill=ACCENT)
    y += 34
    panel_h = 32 * 3 * 4 + 46            # tallest frame set plus the label strip
    d.rectangle([32, y, W - 32, y + panel_h], fill=PANEL)
    x = 60
    for label, rel, fh in monsters:
        sheet = load(rel)
        rows = sheet.crop((0, 0, sheet.width, fh * 3))   # down / right / up
        scaled = up(rows, 4)
        canvas.alpha_composite(scaled, (x, y + 34))
        d.text((x, y + 10), label, font=F_BODY, fill=TEXT)
        x += scaled.width + 60
    y += panel_h + 26

    # ---- transformation hair
    d.text((40, y), "Transformation hair   (6 forms x down / side / up, 16x32 cells)",
           font=F_HEAD, fill=ACCENT)
    y += 34
    hair = load("saiyanhair.png")
    body = Image.new("RGBA", hair.size, (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    for i in range(6):
        for j in range(3):
            ox, oy = i * 16, j * 32
            bd.ellipse([ox + 3, oy + 3, ox + 12, oy + 14], fill=(245, 205, 175, 255))
            bd.rectangle([ox + 4, oy + 15, ox + 11, oy + 24], fill=(90, 120, 180, 255))
            bd.rectangle([ox + 5, oy + 25, ox + 10, oy + 31], fill=(70, 80, 110, 255))
    combined = up(Image.alpha_composite(body, hair), 4)
    d.rectangle([32, y, W - 32, y + combined.height + 40], fill=PANEL)
    canvas.alpha_composite(combined, (60, y + 20))
    names = ["SSJ", "SSJ2", "SSJ3", "God", "Blue", "Ultra"]
    for i, n in enumerate(names):
        d.text((60 + i * 64, y + combined.height + 22), n, font=F_BODY, fill=DIM)
    y += combined.height + 62

    # ---- dragon balls + ki bar + icons
    d.text((40, y), "Items and HUD", font=F_HEAD, fill=ACCENT)
    y += 34
    d.rectangle([32, y, W - 32, y + 250], fill=PANEL)

    balls = up(load("dragonballs.png"), 6)
    canvas.alpha_composite(balls, (60, y + 40))
    d.text((60, y + 14), "Dragon Balls (1-7 star)", font=F_BODY, fill=TEXT)

    bar = up(load("kibar.png"), 4)
    canvas.alpha_composite(bar, (60 + balls.width + 70, y + 40))
    d.text((60 + balls.width + 70, y + 14), "Ki bar chrome", font=F_BODY, fill=TEXT)

    icons = up(load("technique_icons.png"), 5)
    ix = 60 + balls.width + 70 + bar.width + 70
    canvas.alpha_composite(icons, (ix, y + 44))
    d.text((ix, y + 14), "Technique icons", font=F_BODY, fill=TEXT)

    buffs = up(load("icons.png"), 5)
    canvas.alpha_composite(buffs, (ix, y + 130))
    d.text((ix, y + 104), "Form buff icons", font=F_BODY, fill=TEXT)

    disk = up(load("disk.png"), 4)
    dx = ix + max(icons.width, buffs.width) + 70
    canvas.alpha_composite(disk, (dx, y + 44))
    d.text((dx, y + 14), "Destructo Disk", font=F_BODY, fill=TEXT)
    y += 272

    # ---- effects
    d.text((40, y), "Effects", font=F_HEAD, fill=ACCENT)
    y += 34
    aura = load("aura.png")
    aura_body = up(aura.crop((0, 0, aura.width, aura.height // 2)), 3)
    kame = up(load("kamehameha.png"), 3)
    panel_h = max(aura_body.height, kame.height) + 60
    d.rectangle([32, y, W - 32, y + panel_h], fill=(18, 20, 26, 255))
    canvas.alpha_composite(aura_body, (60, y + 40))
    d.text((60, y + 14), "Aura, 8 frames (tinted per form)", font=F_BODY, fill=TEXT)
    kx = 60 + aura_body.width + 60
    canvas.alpha_composite(kame, (kx, y + 40))
    d.text((kx, y + 14), "Beam / charge orb / impact", font=F_BODY, fill=TEXT)
    y += panel_h + 30

    canvas.crop((0, 0, W, min(y + 20, canvas.height))).save(
        os.path.join(HERE, "artboard.png"))
    print("wrote", os.path.join(HERE, "artboard.png"))


if __name__ == "__main__":
    main()
