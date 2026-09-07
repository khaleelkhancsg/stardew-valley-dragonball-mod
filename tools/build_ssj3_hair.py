"""Rebuild the Super Saiyan 3 side cell of assets/saiyanhair.png from the hand-drawn sources.

Each source sheet holds four pieces in a 2x2 grid: front, back, and two side profiles.
Player hair cells are a fixed 16x32, three stacked rows (front / right / back); the game
mirrors the right row for the left facing, so only one side piece is needed.

Only the side row is written; the front and back cells come from SRC and are left alone.

The side cell is built from both drawings. SRC's mane hangs long and is coloured better,
but it sweeps up and back with no forward fringe, so the brow stayed bare at any placement.
SRC_SIDE is the redraw that adds a fringe. So the cell takes SRC below SPLIT_ROW - roughly
the middle of the skull - and SRC_SIDE above it, after remapping SRC_SIDE onto SRC's palette
so the join does not change colour halfway up the head.

A piece is fitted inside a box rather than scaled to a fixed width, so a tall piece cannot
overflow the cell. The two drawings differ in proportion, hence a fit box each.
"""

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

SRC = r"C:/Users/khale/Downloads/stardew images/ssj3 hair.png"           # front, back, lower side
SRC_SIDE = r"C:/Users/khale/Downloads/stardew images/ssj3  updated.png"  # side crown, with a fringe
SHEET = r"C:/Users/khale/Documents/Claude projects/SaiyanTransformations/assets/saiyanhair.png"
COL = 2                     # Super Saiyan 3 is the third hairstyle in the sheet
CW, CH = 16, 32             # one hair cell

# (max width, max height) each piece is fitted into, in cell pixels
FIT_FRONT = (16, 20)
FIT_SIDE = (16, 26)         # the original drawing, which sets the mane's length
FIT_SIDE_NEW = (14, 26)     # the redraw is wider in proportion, so it needs its own box
FIT_BACK = (16, 24)
TOP = 1                     # top margin inside the cell
SIDE_DX = -3                # the original side piece sits 3px back from centre, onto the skull
SIDE_NEW_DX = 0             # the redraw seats on the skull centred
SPLIT_ROW = 7               # cell row where the drawings meet: above is crown, below is mane
SHARPEN = 70                # unsharp strength that suits the original art's reduction
SIDE_NEW_SHARPEN = 130      # the redraw reduces harder, so it needs more (see place)


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


def place(piece, fit, flip=False, dx=0, sharpen=SHARPEN, predetail=False):
    """Fit inside the box, sharpen back the detail the downscale costs, seat it in the cell.

    predetail sharpens once at 4x the target size before the final reduction. The redraw is
    about a 25x reduction against the original art's 20x, and in that extra averaging whole
    strands disappear rather than merely soften, which no amount of sharpening afterwards
    brings back. Lifting the strand edges while they still span several pixels does.
    """
    max_w, max_h = fit
    scale = min(max_w / piece.width, max_h / piece.height)
    w = max(1, round(piece.width * scale))
    h = max(1, round(piece.height * scale))
    if predetail:
        img = piece.resize((w * 4, h * 4), Image.LANCZOS)
        img = img.filter(ImageFilter.UnsharpMask(radius=2.0, percent=90, threshold=2))
        img = img.resize((w, h), Image.LANCZOS)
    else:
        img = piece.resize((w, h), Image.LANCZOS)
    img = img.filter(ImageFilter.UnsharpMask(radius=1.0, percent=sharpen, threshold=2))
    if flip:
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
    cell = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    cell.alpha_composite(img, ((CW - w) // 2 + dx, TOP))
    return cell


def clipped(cell):
    """Opaque pixels sitting on a cell edge, reported so a change to the fit is visible."""
    left = sum(1 for y in range(CH) if cell.getpixel((0, y))[3] > 0)
    right = sum(1 for y in range(CH) if cell.getpixel((CW - 1, y))[3] > 0)
    return left, right


def _luminance(a):
    return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]


def palette_of(piece):
    """Mean colour at each luminance in a drawing, with unused luminances interpolated.

    Both drawings are continuous-tone rather than indexed, so matching them means matching
    tone for tone: this is the curve to look colours up in.
    """
    a = np.array(piece).astype(float)
    solid = a[..., 3] > 200
    levels = np.clip(_luminance(a[solid]).round().astype(int), 0, 255)
    rgb = a[solid][:, :3]

    lut = np.zeros((256, 3))
    known = np.zeros(256, bool)
    for level in range(256):
        sel = levels == level
        if sel.sum() >= 3:                    # ignore stray pixels, they skew the mean
            lut[level] = rgb[sel].mean(axis=0)
            known[level] = True
    idx = np.arange(256)
    for channel in range(3):
        lut[:, channel] = np.interp(idx, idx[known], lut[known, channel])
    return lut


def recolour(piece, lut):
    """Repaint a drawing with another's palette, keeping its own shading and alpha."""
    a = np.array(piece).astype(float)
    out = a.copy()
    out[..., :3] = lut[np.clip(_luminance(a).round().astype(int), 0, 255)]
    return Image.fromarray(out.astype(np.uint8), "RGBA")


def main():
    old_side = extract(SRC)[3]                               # bottom-right of the 2x2 grid
    new_side = extract(SRC_SIDE)[3]

    lower = place(old_side, FIT_SIDE, flip=True, dx=SIDE_DX)  # flipped so the profile faces right
    upper = place(recolour(new_side, palette_of(old_side)), FIT_SIDE_NEW, flip=True,
                  dx=SIDE_NEW_DX, sharpen=SIDE_NEW_SHARPEN, predetail=True)

    cell = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    cell.paste(upper.crop((0, 0, CW, SPLIT_ROW)), (0, 0))
    cell.paste(lower.crop((0, SPLIT_ROW, CW, CH)), (0, SPLIT_ROW))

    sheet = Image.open(SHEET).convert("RGBA")
    sheet.paste(cell, (COL * CW, CH))                        # row 1 only; front and back stay put
    l, r = clipped(cell)
    print(f"side: edge pixels left={l} right={r}")
    sheet.save(SHEET)
    print("wrote", SHEET)


if __name__ == "__main__":
    main()
