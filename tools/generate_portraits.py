import os, glob, numpy as np
from PIL import Image
from scipy import ndimage

SRC = r"C:/Users/khale/Downloads/stardew images"
OUT = r"C:/Users/khale/AppData/Local/Temp/claude/C--Users-khale-Documents-Claude-projects/eb83b425-c5a0-4059-a65a-74408bbaaf84/scratchpad/portraits"
os.makedirs(OUT, exist_ok=True)

# BossId -> source file basename (no ext)
DATA = {
 "Saibamen":"saibaman","Guldo":"guldo","Nappa":"nappa","Jeice":"jeice",
 "EliteWarrior":"saiyan elite","Burter":"burter","Recoome":"recoome",
 "CaptainGinyu":"captain ginyu","FriezaFirst":"frieza (first form)",
 "CoolerFirst":"cooler (first form)","FriezaFinal":"frieza (final form)",
 "CoolerFinal":"cooler (final form)","CellImperfect":"imperfect cell",
 "CellSemiPerfect":"semi perfect cell","CellJuniors":"cell junior",
 "CellPerfect":"perfect cell","Bojack":"bojak","Broly":"broly","Dabura":"dabura",
 "BuuFat":"fat buu","SuperBuu":"real super buu",
 "BuuSuperGohan":"super buu (gohan absorbed)","MetalCoolerLegion":"metal cooler",
 "KidBuu":"super buu","FriezaGolden":"golden frieza","FriezaBlack":"black frieza",
 "Destroyer":"god of destruction","Invader":"multi dimensional invader",
}
UPPER_FRAC = 0.52   # keep the top ~half of the figure: head + shoulders/chest

def key_white(im):
    a = np.array(im.convert("RGBA"))
    r,g,b = a[:,:,0].astype(int), a[:,:,1].astype(int), a[:,:,2].astype(int)
    cs = np.array([a[3,3,:3], a[3,-4,:3], a[-4,3,:3], a[-4,-4,:3]]).astype(int)
    bg = np.median(cs, axis=0)
    dist = np.sqrt((r-bg[0])**2 + (g-bg[1])**2 + (b-bg[2])**2)
    thr = 22 if bg.mean() < 70 else 46
    bgish = dist < thr
    lbl,n = ndimage.label(bgish)
    border = set(lbl[0,:])|set(lbl[-1,:])|set(lbl[:,0])|set(lbl[:,-1]); border.discard(0)
    a[np.isin(lbl, list(border)),3] = 0
    if bg.mean() < 70:                       # dark sheet: solidify figure
        op = a[:,:,3] > 10
        filled = ndimage.binary_fill_holes(ndimage.binary_closing(op, iterations=3))
        a[filled & (a[:,:,3] <= 10),3] = 255
    return a

def front_frame1(a):
    op = a[:,:,3] > 24
    lbl,n = ndimage.label(op)
    comps=[]
    for i in range(1,n+1):
        ys,xs = np.where(lbl==i)
        if len(xs) < 2500: continue
        x0,x1,y0,y1 = xs.min(),xs.max(),ys.min(),ys.max()
        if (y1-y0) < 120: continue
        comps.append((x0,y0,x1,y1))
    # top row = smallest y-centre; within it, leftmost x = frame 1
    med = np.median([c[3]-c[1] for c in comps])
    comps.sort(key=lambda c:(c[1]+c[3])//2)
    topy = (comps[0][1]+comps[0][3])//2
    top = [c for c in comps if abs((c[1]+c[3])//2 - topy) < med*0.6]
    top.sort(key=lambda c:c[0])
    c = top[0]
    return a[c[1]:c[3]+1, c[0]:c[2]+1]

def portrait(a):
    sub = front_frame1(a)
    H = sub.shape[0]
    bust = sub[0:int(H*UPPER_FRAC)]
    # trim transparent margins around the bust
    op = bust[:,:,3] > 24
    ys,xs = np.where(op)
    bust = bust[ys.min():ys.max()+1, xs.min():xs.max()+1]
    im = Image.fromarray(bust, "RGBA")
    # fill the full 64px height; if the bust is wider than the frame, centre-crop the sides
    r = 64 / im.height
    w, h = max(1, int(round(im.width * r))), 64
    im = im.resize((w, h), Image.LANCZOS)
    out = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    out.alpha_composite(im, ((64 - w) // 2, 0))
    return out

made=[]
for bid, base in DATA.items():
    matches = [f for f in glob.glob(os.path.join(SRC,"*.png")) if os.path.basename(f)[:-4].lower()==base]
    if not matches:
        print("MISSING source:", bid, base); continue
    a = key_white(Image.open(matches[0]))
    portrait(a).save(os.path.join(OUT, bid+".png"))
    made.append(bid)
print("made", len(made), "portraits")
