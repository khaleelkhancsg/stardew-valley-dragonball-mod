import os, numpy as np
from PIL import Image
from scipy import ndimage

SRC = r"C:/Users/khale/Downloads/stardew images"
TMP = r"C:/Users/khale/AppData/Local/Temp/claude/C--Users-khale-Documents-Claude-projects/eb83b425-c5a0-4059-a65a-74408bbaaf84/scratchpad/sheets"
os.makedirs(TMP, exist_ok=True)

# file (exact basename, no ext) -> (sheet, kind, frame_w, frame_h)
MAP = {
 "saibaman": ("saibamen","GreenSlime",24,34),
 "cell junior": ("celljr","GreenSlime",22,32),
 "frieza (first form)": ("friezafirst","SquidKid",24,36),
 "nappa": ("nappa","ShadowBrute",30,46),
 "saiyan elite": ("eliteboss","ShadowBrute",28,44),
 "burter": ("burter","ShadowBrute",28,46),
 "recoome": ("recoome","ShadowBrute",32,48),
 "captain ginyu": ("captainginyu","ShadowBrute",28,44),
 "frieza (final form)": ("friezafinal","ShadowBrute",26,42),
 "golden frieza": ("friezagolden","ShadowBrute",28,46),
 "black frieza": ("friezablack","ShadowBrute",28,46),
 "cooler (first form)": ("coolerfirst","ShadowBrute",28,44),
 "cooler (final form)": ("coolerfinal","ShadowBrute",30,48),
 "metal cooler": ("metalcooler","Mummy",28,46),
 "imperfect cell": ("cellimperfect","Mummy",30,46),
 "semi perfect cell": ("cellsemiperfect","Mummy",30,48),
 "perfect cell": ("cellperfect","Mummy",30,50),
 "bojak": ("bojack","ShadowBrute",28,46),
 "dabura": ("dabura","ShadowBrute",28,46),
 "broly": ("broly","ShadowBrute",32,50),
 "god of destruction": ("destroyer","ShadowBrute",30,50),
 "fat buu": ("buufat","Mummy",32,46),
 "super buu (gohan absorbed)": ("buusupergohan","Mummy",30,50),
 "super buu": ("superbuu","Mummy",28,46),
 "dragonball guardian": ("guardian","ShadowBrute",30,48),
 # invader excluded: dark-on-dark source does not key cleanly; keeps its placeholder
}
ROWS = {"GreenSlime":7,"SquidKid":4,"ShadowBrute":8,"Mummy":5}
ROW_FACING = [2,1,0]  # cycles: down(front), right(right), up(back)

def key_white(im):
    a = np.array(im.convert("RGBA"))
    r,g,b = a[:,:,0].astype(int), a[:,:,1].astype(int), a[:,:,2].astype(int)
    # background colour = median of the four corners (handles white OR dark sheets)
    cs = np.array([a[3,3,:3], a[3,-4,:3], a[-4,3,:3], a[-4,-4,:3]]).astype(int)
    bg = np.median(cs, axis=0)
    dist = np.sqrt((r-bg[0])**2 + (g-bg[1])**2 + (b-bg[2])**2)
    thr = 24 if bg.mean() < 70 else 46      # tighter key on dark sheets
    bgish = dist < thr
    lbl, n = ndimage.label(bgish)
    border = set(lbl[0,:]) | set(lbl[-1,:]) | set(lbl[:,0]) | set(lbl[:,-1])
    border.discard(0)
    a[np.isin(lbl, list(border)), 3] = 0
    return a

def components(a):
    op = a[:,:,3] > 24
    lbl, n = ndimage.label(op)
    out = []
    for i in range(1, n+1):
        ys, xs = np.where(lbl==i)
        if len(xs) < 2500: continue
        x0,x1,y0,y1 = xs.min(), xs.max(), ys.min(), ys.max()
        if (y1-y0) < 120: continue      # drop label text (short)
        out.append((x0,y0,x1,y1,(x0+x1)//2,(y0+y1)//2))
    return out

def cluster(vals, gap):
    order = sorted(range(len(vals)), key=lambda i: vals[i])
    groups=[[order[0]]]
    for k in range(1,len(order)):
        if vals[order[k]] - vals[order[k-1]] > gap:
            groups.append([])
        groups[-1].append(order[k])
    return groups

def extract_rows(comps):
    if not comps: return []
    hs=[c[3]-c[1] for c in comps]; med=np.median(hs)
    ys=[c[5] for c in comps]
    rowgroups = cluster(ys, med*0.6)   # ascending Y => front,back,right,left
    rows=[]
    for grp in rowgroups:
        cs=[comps[i] for i in grp]
        cs.sort(key=lambda c:c[4])      # left->right = frames
        rows.append(cs)
    return rows

def crop(a, c):
    x0,y0,x1,y1,_,_ = c
    sub = a[y0:y1+1, x0:x1+1]
    # trim fully-transparent border
    op = sub[:,:,3] > 24
    if op.any():
        ys,xs = np.where(op)
        sub = sub[ys.min():ys.max()+1, xs.min():xs.max()+1]
    return Image.fromarray(sub, "RGBA")

def fit_cell(spr, fw, fh, scale):
    # scale is uniform across every frame of a boss, so the character keeps one height
    cell = Image.new("RGBA", (fw, fh), (0,0,0,0))
    w,h = max(1,int(round(spr.width*scale))), max(1,int(round(spr.height*scale)))
    s = spr.resize((w,h), Image.LANCZOS)
    x = (fw - w)//2
    y = fh - h            # bottom-align (feet at cell bottom)
    cell.alpha_composite(s, (x,y))
    return cell

def build(name, kind, fw, fh, rows):
    facings = {}   # 2=front(down),1=right,0=back(up)
    if len(rows)>=1: facings[2]=rows[0]
    if len(rows)>=2: facings[0]=rows[1]   # back
    if len(rows)>=3: facings[1]=rows[2]   # right
    nrows = ROWS[kind]
    # one uniform scale for the whole boss, sized so the tallest/widest frame fits
    used = [crop_cache[c] for r in rows for c in r]
    Wmax = max(im.width for im in used); Hmax = max(im.height for im in used)
    scale = min((fh-1)/Hmax, (fw-2)/Wmax)
    sheet = Image.new("RGBA", (4*fw, nrows*fh), (0,0,0,0))
    for row in range(nrows):
        facing = ROW_FACING[row % len(ROW_FACING)]
        frames = facings.get(facing) or facings.get(2) or []
        for col in range(4):
            if not frames: continue
            spr = frames[col % len(frames)]
            cell = fit_cell(crop_cache[spr], fw, fh, scale)
            sheet.alpha_composite(cell, (col*fw, row*fh))
    sheet.save(os.path.join(TMP, name+".png"))
    return sheet

# ---- run
import glob
crop_cache = {}
results = []
for path in glob.glob(os.path.join(SRC,"*.png")):
    base = os.path.basename(path)[:-4].lower()
    if base not in MAP: continue
    sheet, kind, fw, fh = MAP[base]
    a = key_white(Image.open(path))
    comps = components(a)
    rows = extract_rows(comps)
    total = sum(len(r) for r in rows)
    if total < 8:
        print(f"{base:28s} -> SKIP (only {total} sprites found; likely dark/low-contrast source)")
        continue
    # cache crops as Image objects keyed by comp tuple
    rows_img = []
    for r in rows:
        row=[]
        for c in r:
            img = crop(a, c)
            crop_cache[c] = img
            row.append(c)
        rows_img.append(row)
    out = build(sheet, kind, fw, fh, rows_img)
    results.append((base, sheet, kind, fw, fh, sum(len(r) for r in rows_img), out.size))
    print(f"{base:28s} -> {sheet:16s} {kind:11s} {fw}x{fh}  sprites={sum(len(r) for r in rows_img):2d} sheet={out.size}")

print("done", len(results))

# ---- preview montage of the generated game-ready sheets (scaled up)
from PIL import ImageDraw
SCALE=4; cols=5; pad=16; lblh=26
sheets = sorted(results, key=lambda r: r[1])
cellw = max(s[6][0] for s in sheets)*SCALE + pad
rowh_by = {}
mont_rows = (len(sheets)+cols-1)//cols
def cell_h(idx):
    return sheets[idx][6][1]*SCALE
rh=[]
for r in range(mont_rows):
    hs=[cell_h(i) for i in range(r*cols,min((r+1)*cols,len(sheets)))]
    rh.append(max(hs)+lblh+pad)
W = cols*cellw + pad
H = sum(rh) + pad
mont = Image.new("RGBA",(W,H),(58,60,68,255))
d = ImageDraw.Draw(mont)
y=pad
for r in range(mont_rows):
    x=pad
    for c in range(cols):
        i=r*cols+c
        if i>=len(sheets): break
        base,sheet,kind,fw,fh,ns,size = sheets[i]
        img = Image.open(os.path.join(TMP, sheet+".png")).resize((size[0]*SCALE,size[1]*SCALE), Image.NEAREST)
        card = Image.new("RGBA",(size[0]*SCALE, size[1]*SCALE),(34,36,44,255))
        mont.alpha_composite(card,(x,y))
        mont.alpha_composite(img,(x,y))
        d.text((x+2,y+size[1]*SCALE+3), f"{sheet}  {fw}x{fh}", fill=(232,236,244,255))
        x+=cellw
    y+=rh[r]
OUTP = r"C:/Users/khale/AppData/Local/Temp/claude/C--Users-khale-Documents-Claude-projects/eb83b425-c5a0-4059-a65a-74408bbaaf84/scratchpad/fitted_sheets_preview.png"
mont.save(OUTP)
print("montage", mont.size, "->", OUTP)
