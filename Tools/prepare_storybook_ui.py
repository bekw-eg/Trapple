"""Extract the supplied transparent artwork without repainting or erasing labels.

Coordinates are in original 1448 x 1086 source pixels, from the top left.
Run from the repository root. Unity's StorybookUIBuilder configures importers.
"""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = Path('Assets/_Trapple/Art/SourceSheets')
OUT = Path('Assets/_Trapple/UI/Storybook')
SHEETS = {int(p.stem.rsplit('-', 1)[1]): p for p in ROOT.glob('*.png')}
# Full element envelopes; transparency, leaves and painted shadows are retained.
REGIONS = {
 1: {'Buttons/Logo': (155, 0, 1275, 380), 'Buttons/Play': (35, 380, 875, 615),
     'Buttons/Settings': (35, 615, 875, 845), 'Buttons/Exit': (35, 845, 875, 1086),
     'Buttons/Back': (905, 380, 1390, 570), 'Buttons/Blank': (910, 575, 1380, 697)},
 2: {'Buttons/WorldMap': (40, 0, 1020, 241), 'Buttons/Workshop': (40, 241, 1020, 449),
     'Buttons/Wardrobe': (40, 449, 1020, 651), 'Buttons/HubSettings': (40, 651, 1020, 849),
     'Buttons/Close': (80, 849, 1000, 1086)},
 3: {'WorldMap/Meadow': (0, 0, 720, 308), 'WorldMap/Forest': (720, 0, 1448, 308),
     'WorldMap/Factory': (0, 308, 720, 626), 'WorldMap/Lava': (720, 308, 1448, 626),
     'WorldMap/Level': (0, 630, 257, 825), 'WorldMap/Level2': (257,630,490,825), 'WorldMap/Level3': (490,630,728,825), 'WorldMap/Locked': (728, 630, 982, 825),
     'WorldMap/Complete': (980, 630, 1215, 825), 'WorldMap/Selected': (1208, 626, 1448, 831)},
 4: {'Workshop/Speed': (0, 0, 487, 309), 'Workshop/Jump': (487, 0, 975, 309),
     'Workshop/Agility': (975, 0, 1448, 309), 'Workshop/Upgrade': (0, 551, 525, 764),
     'Workshop/Max': (524, 551, 985, 764)},
 5: {'Wardrobe/Slot': (0, 0, 371, 375), 'Wardrobe/Selected': (371, 0, 746, 375),
     'Wardrobe/Equipped': (746, 0, 1095, 375), 'Wardrobe/Locked': (1095, 0, 1448, 375),
     'Wardrobe/Equip': (0, 375, 740, 602), 'Wardrobe/EquippedButton': (740, 375, 1448, 602)},
 6: {'HUD/Pause': (0, 0, 342, 380),
     'HUD/A': (0, 420, 263, 635), 'HUD/D': (263, 420, 503, 635),
     'HUD/Left': (503, 420, 739, 635), 'HUD/Right': (739, 420, 978, 635),
     'HUD/E': (978, 420, 1216, 635), 'HUD/F': (1216, 420, 1448, 635),
     'HUD/Space': (0, 638, 565, 855), 'HUD/Esc': (565, 638, 970, 855),
     'HUD/Shift': (970, 638, 1448, 855)},
 7: {'Panels/PauseTitle': (120, 0, 1215, 330), 'Buttons/Continue': (100, 330, 840, 492),
     'Buttons/PauseSettings': (100, 492, 840, 648), 'Buttons/Restart': (100, 648, 840, 806),
     'Buttons/Lobby': (100, 806, 840, 944), 'Buttons/MainMenu': (100, 944, 840, 1086)},
 8: {'Panels/CompleteTitle': (0, 0, 970, 525), 'Buttons/Retry': (0, 525, 472, 751),
     'Buttons/NextLevel': (472, 525, 985, 751), 'Buttons/CompleteLobby': (985, 525, 1448, 751)},
 9: {'Settings/Title': (175, 0, 1255, 350), 'Settings/MasterVolume': (0, 350, 600, 540),
     'Settings/SliderTrack': (1110, 398, 1394, 517),
     'Settings/SliderFill': (600, 420, 1005, 487), 'Settings/SliderHandle': (1042, 430, 1100, 490)},
 10: {'Panels/Banner': (165, 0, 1275, 273), 'Panels/Window': (0, 273, 855, 690),
      'Panels/Card': (855, 273, 1448, 527), 'Panels/Small': (875, 527, 1410, 692),
      'Icons/Pause': (0, 692, 193, 867), 'Icons/Menu': (193, 692, 375, 867),
      'Icons/Settings': (375, 692, 558, 867), 'Icons/Home': (558, 692, 741, 867),
      'Icons/Info': (741, 692, 916, 867), 'Icons/Back': (916, 692, 1087, 867),
      'Icons/Close': (1087, 692, 1260, 867), 'Icons/Check': (1260, 692, 1448, 867),
      'Icons/Lock': (800, 867, 1002, 1086), 'Icons/Seed': (1002, 867, 1212, 1086)}
}

def prepare():
    manifest = []
    thumbnails = []
    for sheet, entries in REGIONS.items():
        original = Image.open(SHEETS[sheet]).convert('RGBA')
        if original.size != (1448, 1086):
            raise ValueError(f'Unexpected source dimensions: {SHEETS[sheet]}')
        for name, box in entries.items():
            # Select the connected painted island on the *whole* sheet. Envelopes
            # identify an element; they are never used as clipping boundaries.
            # This also removes fragments of adjacent buttons in close rows.
            if name.startswith('Settings/Slider') or name == 'Settings/MasterVolume' or name in {
                'Buttons/PauseSettings','Buttons/Restart','Buttons/Lobby','Buttons/MainMenu'}:
                continue  # These are baked into a single slider; never erase its paint.
            alpha = original.getchannel('A')
            # Several source shadows touch at low alpha. Use the opaque painted
            # island to locate its envelope, then keep the original RGBA pixels.
            threshold = 200 if sheet == 5 else 128 if sheet == 3 else 64
            binary = alpha.point(lambda a: 255 if a > threshold else 0)
            cx, cy = (box[0]+box[2])//2, (box[1]+box[3])//2
            if binary.getpixel((cx,cy)) == 0:
                # Title/slot centers can contain transparent holes.
                candidates = [(x,y) for y in range(box[1]+10,box[3]-10,8)
                              for x in range(box[0]+10,box[2]-10,8)
                              if binary.getpixel((x,y))]
                cx,cy = min(candidates,key=lambda xy:(xy[0]-cx)**2+(xy[1]-cy)**2)
            ImageDraw.floodfill(binary,(cx,cy),128)
            mask = binary.point(lambda a: 255 if a == 128 else 0)
            bbox = mask.getbbox()
            # Add a 3px envelope for the alpha feather at the island boundary.
            bbox = (max(0,bbox[0]-3),max(0,bbox[1]-3),min(original.width,bbox[2]+3),min(original.height,bbox[3]+3))
            isolated = original.copy()
            isolated.putalpha(ImageChops.multiply(alpha, mask.filter(ImageFilter.MaxFilter(9))))
            crop = isolated.crop(bbox)
            if not bbox:
                raise ValueError(f'Empty element: {name}')
            # Padding retains antialiased shadow pixels and avoids atlas bleeding.
            padded = Image.new('RGBA', (crop.width + 8, crop.height + 8))
            padded.paste(crop, (4, 4))
            target = OUT / (name + '.png')
            target.parent.mkdir(parents=True, exist_ok=True)
            padded.save(target)
            manifest.append(dict(asset=str(target).replace('\\', '/'), source=str(SHEETS[sheet]).replace('\\', '/'),
                                 envelope=list(box), trim=list(bbox), size=list(padded.size)))
            thumb = padded.copy(); thumb.thumbnail((232, 140))
            thumbnails.append((name, thumb))
    # The circular faces inside the stat bars contain no labels or baked level
    # indicators. Extract just these icons, keeping all original painted pixels.
    original=Image.open(SHEETS[4]).convert('RGBA')
    for name,box in {'SpeedIcon':(85,124,216,255),'JumpIcon':(563,124,694,255),'AgilityIcon':(1042,124,1173,255)}.items():
        crop=original.crop(box); mask=Image.new('L',crop.size); ImageDraw.Draw(mask).ellipse((0,0,crop.width-1,crop.height-1),fill=255)
        crop.putalpha(ImageChops.multiply(crop.getchannel('A'),mask)); target=OUT/'Workshop'/(name+'.png'); crop.save(target)
        manifest.append(dict(asset=str(target).replace('\\','/'),source=str(SHEETS[4]).replace('\\','/'),envelope=list(box),size=list(crop.size)))
    # A plain painted gold surface is safe to shorten inside a slider. Decorative
    # leaves stay on the fixed track and handle rather than getting compressed.
    box=(510,112,920,175); crop=Image.open(SHEETS[10]).convert('RGBA').crop(box)
    target=OUT/'Settings'/'GoldFill.png'; crop.save(target)
    manifest.append(dict(asset=str(target).replace('\\','/'),source=str(SHEETS[10]).replace('\\','/'),envelope=list(box),size=list(crop.size)))
    (OUT / 'slices.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding='utf-8')
    preview = Image.new('RGB', (1200, ((len(thumbnails)+4)//5)*178), '#252820')
    draw = ImageDraw.Draw(preview)
    for i, (name, thumb) in enumerate(thumbnails):
        x, y = (i % 5)*240, (i // 5)*178
        preview.paste(thumb, (x+(240-thumb.width)//2, y), thumb)
        draw.text((x+4, y+145), name, fill='#fff0c1')
    Path('Temp').mkdir(exist_ok=True)
    preview.save('Temp/storybook-slices.png')
    print(f'Extracted {len(manifest)} sprites from {len(SHEETS)} unchanged source sheets.')

if __name__ == '__main__':
    prepare()
