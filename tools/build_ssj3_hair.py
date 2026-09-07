"""Rebuild the Super Saiyan 3 column of assets/saiyanhair.png from the hand-drawn source.

The source sheet holds four pieces in a 2x2 grid: front, back, and two side profiles.
Player hair cells are a fixed 16x32, three stacked rows (front / right / back); the game
mirrors the right row for the left facing, so only one side piece is needed.

Each piece is fitted inside a box rather than scaled to a fixed width, so a tall piece
cannot overflow the cell, and each is centred so nothing is ever clipped at an edge.
"""

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

SRC = r"C:/Users/khale/Downloads/stardew images/ssj3 hair.png"
SHEET = r"C:/Users/khale/Documents/Claude projects/SaiyanTransformations/assets/saiyanhair.png"
COL = 2                     # Super Saiyan 3 is the third hairstyle in the sheet
CW, CH = 16, 32             # one hair cell

# (max width, max height) each piece is fitted into, in cell pixels
FIT_FRONT = (16, 20)
FIT_SIDE = (16, 26)
FIT_BACK = (16, 24)
TOP = 1                     # top margin inside the cell


def extract(path):
    """Key the light background and return the four pieces: front, back, sideL, sideR."""
    im = Image.open(path).convert("RGBA")
    a = np.array(im)
    r, g, b = a[:, :, 0].astype(int), a[:, :, 1].astype(int), a[:, :, 2].astype(int)
    # background is near-white/grey; the hair is yellow, so its blue channel stays low
    whiteish = np.minimum(np.minimum(r, g), b) > 205
    lbl, _ = ndimage.label(whiteish)
    border = set(lbl[0, :]) | set(lbl[-1, :]) | set(lbl[:, 0]) | set(lbl[:, -1])
    border.discard(0)
    a[np.isin(lbl, list(border)), 3] = 0          # only clear background joined to the edge
    keyed = Image.fromarray(a, "RGBA")

    lbl2, n = ndimage.label(a[:, :, 3] > 40)
    boxes = []
    for i in range(1, n + 1):
        ys, xs = np.where(lbl2 == i)
        if len(xs) < 800:
            continue
        boxes.append((xs.min(), ys.min(), xs.max(), ys.max()))
    boxes.sort(key=lambda c: (round(c[1] / 200), c[0]))   # top row first, then left to right
    return [keyed.crop((x0, y0, x1 + 1, y1 + 1)) for x0, y0, x1, y1 in boxes[:4]]


def place(piece, fit, flip=False):
    """Fit inside the box, sharpen back the detail the downscale costs, centre in the cell."""
    max_w, max_h = fit
    scale = min(max_w / piece.width, max_h / piece.height)
    w = max(1, round(piece.width * scale))
    h = max(1, round(piece.height * scale))
    img = piece.resize((w, h), Image.LANCZOS)
    img = img.filter(ImageFilter.UnsharpMask(radius=1.0, percent=70, threshold=2))
    if flip:
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
    cell = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    cell.alpha_composite(img, ((CW - w) // 2, TOP))     # centred: never clipped
    return cell


def clipped(cell):
    """Opaque pixels sitting on a cell edge, which would read as a cut-off silhouette."""
    left = sum(1 for y in range(CH) if cell.getpixel((0, y))[3] > 0)
    right = sum(1 for y in range(CH) if cell.getpixel((CW - 1, y))[3] > 0)
    return left, right


def main():
    front, back, _side_l, side_r = extract(SRC)

    cells = {
        0: place(front, FIT_FRONT),
        1: place(side_r, FIT_SIDE, flip=True),   # flipped so the profile faces right
        2: place(back, FIT_BACK),
    }

    sheet = Image.open(SHEET).convert("RGBA")
    for row, cell in cells.items():
        sheet.paste(cell, (COL * CW, row * CH))
        l, r = clipped(cell)
        name = {0: "front", 1: "side ", 2: "back "}[row]
        flag = "  <-- TOUCHING EDGE" if (l or r) else ""
        print(f"{name}: edge pixels left={l} right={r}{flag}")
    sheet.save(SHEET)
    print("wrote", SHEET)


if __name__ == "__main__":
    main()
