"""Extract continuous terrain from the supplied level reference (Pillow required)."""
import shutil
import sys
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
stored = root / 'Assets/_Trapple/Art/AppleMeadow/Sources/LevelReference.png'
source = Path(sys.argv[1]) if len(sys.argv) > 1 else stored
if source.resolve() != stored.resolve():
    shutil.copyfile(source, stored)
image = Image.open(source).convert('RGBA').crop((575, 262, 745, 312))
pixels = image.load()
w, h = image.size
# Match both sides without introducing a rounded island edge at every repeat.
for y in range(h):
    seam = tuple(round((pixels[0, y][c] + pixels[w-1, y][c]) / 2) for c in range(4))
    for i in range(12):
        blend = i / 12
        for x in (i, w-1-i):
            pixels[x, y] = tuple(round(seam[c] * (1-blend) + pixels[x, y][c] * blend) for c in range(4))
# The grass cap occupies the top twenty pixels; only soil repeats vertically.
for x in range(w):
    seam = tuple(round((pixels[x, 20][c] + pixels[x, h-1][c]) / 2) for c in range(4))
    for i in range(4):
        blend = i / 4
        for y in (20+i, h-1-i):
            pixels[x, y] = tuple(round(seam[c] * (1-blend) + pixels[x, y][c] * blend) for c in range(4))
image.save(root / 'Assets/_Trapple/Resources/AppleMeadow/Ground.png')
print('Prepared continuous grass and soil terrain from LevelReference.png')
