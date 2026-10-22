"""Extract the user's supplied sheets. Usage: python Tools/prepare_meadow_art.py <sheet-folder>.

Only matte-connected pixels are removed, preserving enclosed dark outlines.
The original sheets are retained in Art/AppleMeadow/Sources for provenance.
"""
import sys
import shutil
from collections import deque
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops, ImageStat

root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1])
out = root / 'Assets/_Trapple/Resources/AppleMeadow'
originals = root / 'Assets/_Trapple/Art/AppleMeadow/Sources'
out.mkdir(parents=True, exist_ok=True)
originals.mkdir(parents=True, exist_ok=True)
ids = ['e0635496-95c0-4a7d-b884-c455ff166a9a', '9c99edd2-cba5-4ab6-8511-06496ac4b74b',
       'aad183f3-da30-4ad4-ac09-f65f4080d817', '706ed167-d757-4185-b633-f61984470e8b',
       '388e3c11-56c0-4366-bf9d-1e86a94d42e3', '892a7efb-e789-4fa5-8a2e-8a5f964d25db',
       '9891a829-6e0e-4fee-b9f7-38744e196c15']
sheets = []
for i, id in enumerate(ids):
    path = source / f'codex-clipboard-{id}.png'
    if not path.exists():
        path = source / f'Sheet{i+1}.png'
    target = originals / f'Sheet{i+1}.png'
    if path.resolve() != target.resolve():
        shutil.copyfile(path, target)
    sheets.append(Image.open(path).convert('RGBA'))

def crop(name, sheet, box, matte=True, checker=False):
    im = sheets[sheet-1].crop(box)
    if matte:
        px = im.load()
        w, h = im.size
        visited = set()
        queue = deque([(x, y) for x in range(w) for y in (0, h-1)] +
                      [(x, y) for y in range(h) for x in (0, w-1)])
        while queue:
            x, y = queue.popleft()
            if (x, y) in visited or not (0 <= x < w and 0 <= y < h):
                continue
            visited.add((x, y))
            r, g, b, a = px[x, y]
            background = max(r, g, b) < 38
            if checker:
                background = min(r, g, b) > 165 and max(r, g, b)-min(r, g, b) < 25
            if not background:
                continue
            px[x, y] = (r, g, b, 0)
            queue.extend(((x-1, y), (x+1, y), (x, y-1), (x, y+1)))
    if name == 'Tree':
        # The source contains a brown background glow beside the trunk. Trim that
        # empty area explicitly rather than erasing similarly dark tree leaves.
        mask = Image.new('L', im.size)
        ImageDraw.Draw(mask).polygon([(0, 0), (434, 0), (434, 90), (402, 115),
            (420, 170), (418, 220), (393, 260), (370, 280), (322, 284),
            (286, 333), (364, 411), (300, 425), (45, 425), (61, 383),
            (150, 341), (162, 285), (0, 285)], fill=255)
        im.putalpha(ImageChops.multiply(im.getchannel('A'), mask))
    if matte:
        # Retain separate illustrated parts (rope, planks, stems) while excluding
        # isolated dark glows and tiny fragments of neighbouring sprites.
        px = im.load()
        w, h = im.size
        seen = set()
        retained = []
        for y in range(h):
            for x in range(w):
                if (x, y) in seen or px[x, y][3] == 0:
                    continue
                component = []
                queue = deque([(x, y)])
                while queue:
                    a, b = queue.popleft()
                    if (a, b) in seen or not (0 <= a < w and 0 <= b < h):
                        continue
                    seen.add((a, b))
                    if px[a, b][3] == 0:
                        continue
                    component.append((a, b))
                    queue.extend(((a-1,b), (a+1,b), (a,b-1), (a,b+1)))
                if len(component) > 40 and any(max(px[a, b][:3]) > 110 for a, b in component):
                    retained.extend(component)
        mask = Image.new('L', im.size)
        mp = mask.load()
        for x, y in retained:
            mp[x, y] = 255
        im.putalpha(ImageChops.multiply(im.getchannel('A'), mask))
        # Exclude fragments of neighbouring objects and glows at sheet crop edges.
        exclusions = {
            'Apple': [(0, 0, 15, 40), (125, 0, 135, 20)],
            'Bush': [(210, 0, 241, 40)],
            'Flowers': [(0, 0, 227, 14)],
            'Crate': [(0, 178, 185, 182), (0, 0, 3, 182), (181, 0, 185, 182)],
            'Sign': [(150, 203, 188, 223), (183, 75, 188, 203)],
            'Warning': [(0, 0, 50, 13), (165, 145, 175, 187)],
        }
        alpha = im.getchannel('A')
        draw = ImageDraw.Draw(alpha)
        for region in exclusions.get(name, []):
            draw.rectangle(region, fill=0)
        if name == 'Rocks':
            draw.polygon([(0, 0), (63, 0), (30, 44), (0, 70)], fill=0)
            draw.rectangle((0, 147, w, h), fill=0)
        if name == 'Stump':
            draw.polygon([(0, 140), (23, 157), (35, 180), (0, 190)], fill=0)
            draw.polygon([(224, 147), (259, 145), (259, 190), (228, 190)], fill=0)
        im.putalpha(alpha)
    if name == 'Landscape':
        # Extend the supplied panorama upward so the camera sees a continuous sky,
        # rather than a horizontal seam between the panorama and its clear colour.
        colour = tuple(ImageStat.Stat(im.crop((0, 0, im.width, 1))).median)
        sky = Image.new('RGBA', (im.width, im.height + 170), colour)
        feather = Image.new('L', im.size, 255)
        fd = ImageDraw.Draw(feather)
        for y in range(14):
            fd.line((0, y, im.width, y), fill=round(y / 13 * 255))
        sky.paste(im, (0, 170), feather)
        im = sky
    im.save(out / f'{name}.png')

crop('Ground', 1, (35, 40, 575, 188))
crop('Island', 1, (497, 250, 689, 376))
crop('Bridge', 2, (25, 15, 618, 210))
crop('Sign', 2, (540, 273, 728, 496))
crop('Flag', 2, (100, 483, 312, 700))
crop('Tree', 3, (37, 15, 471, 440))
crop('Bush', 3, (120, 462, 361, 628))
crop('Flowers', 3, (405, 488, 632, 618))
crop('Grass', 3, (47, 619, 239, 717))
crop('Crate', 4, (264, 45, 449, 227))
crop('Stump', 4, (467, 45, 726, 235))
crop('Rocks', 4, (767, 345, 942, 495))
crop('Apple', 5, (835, 25, 970, 166))
crop('Spikes', 5, (325, 17, 502, 94))
crop('Warning', 5, (566, 451, 741, 638))
crop('Landscape', 6, (15, 282, 947, 437), matte=False)
crop('Pause', 7, (0, 32, 552, 607), checker=True)
print(f'Extracted {len(list(out.glob("*.png")))} assets')
