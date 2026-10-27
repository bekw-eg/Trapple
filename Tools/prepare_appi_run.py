"""Extract the supplied Appi run sheet without repainting it (Pillow required)."""
from collections import deque
from pathlib import Path
import shutil
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/_Trapple/Art/Hub/Source/AppiRunReference.png'
OUTPUT = ROOT / 'Assets/_Trapple/Art/Hub/AppiRun.png'


def character_only(image):
    """Retain the connected character; discard disconnected alpha speckles/text."""
    pixels = image.load()
    remaining = {(x, y) for y in range(image.height) for x in range(image.width)
                 if pixels[x, y][3] >= 32}
    largest = set()
    while remaining:
        seed = remaining.pop()
        component = {seed}
        pending = deque([seed])
        while pending:
            x, y = pending.popleft()
            for neighbour in ((x-1, y), (x+1, y), (x, y-1), (x, y+1)):
                if neighbour in remaining:
                    remaining.remove(neighbour)
                    component.add(neighbour)
                    pending.append(neighbour)
        if len(component) > len(largest):
            largest = component
    if len(largest) < 15000:
        raise ValueError('Expected a complete connected Appi silhouette')
    result = Image.new('RGBA', image.size)
    for xy in largest:
        result.putpixel(xy, pixels[xy])
    return result


def prepare(source=SOURCE):
    if source.resolve() != SOURCE.resolve():
        shutil.copyfile(source, SOURCE)
    image = Image.open(SOURCE)
    if image.mode != 'RGBA' or image.size != (1536, 1024):
        raise ValueError('Expected the supplied 1536 x 1024 RGBA run sheet')
    atlas = Image.new('RGBA', (8 * 144, 2 * 156))
    frames = []
    for row, top in enumerate((240, 638)):
        for index in range(8):
            left = 30 + index * 184
            character = character_only(image.crop((left, top, left+184, top+208)))
            # Centre the apple body, not its alternating outstretched feet/hand.
            pixels = character.load()
            red = [(x, y) for y in range(65, 140) for x in range(184)
                   if pixels[x, y][3] >= 128 and pixels[x, y][0] > 160
                   and pixels[x, y][1] < 100 and pixels[x, y][2] < 100]
            centre = round((min(x for x, y in red) + max(x for x, y in red)) / 2)
            canvas = Image.new('RGBA', (192, 208))
            lift = (0, 2, 3, 1, 0, 2, 3, 1)[index]
            canvas.paste(character, (96-centre, -lift))
            # Uniform 3/4 nearest-neighbour normalization matches the old Appi's
            # height at its existing 128 PPU. No interpolation or new RGB colours.
            frame = canvas.resize((144, 156), Image.Resampling.NEAREST)
            atlas.paste(frame, (index*144, row*156))
            frames.append(frame)
    atlas.save(OUTPUT)
    print('Prepared 16 run frames: 144 x 156, common body centre and baseline, RGBA.')
    return frames


if __name__ == '__main__':
    prepare(Path(sys.argv[1]) if len(sys.argv) > 1 else SOURCE)
