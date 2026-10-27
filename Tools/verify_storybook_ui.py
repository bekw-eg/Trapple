"""Read-only checks for the delivered sprite and serialized UI asset contracts."""
from pathlib import Path
import json
import re
import subprocess
from PIL import Image

root=Path('Assets/_Trapple/UI/Storybook')
entries=json.loads((root/'slices.json').read_text(encoding='utf-8'))
checks=[]
def check(value, label):
    checks.append((bool(value), label))
    if not value:
        print('FAIL', label)

check(len(list(Path('Assets/_Trapple/Art/SourceSheets').glob('*.png')))==10, 'All ten original source sheets retained')
for entry in entries:
    path=Path(entry['asset'])
    image=Image.open(path)
    check(image.mode=='RGBA',str(path)+' preserves RGBA')
    check(list(image.size)==entry['size'],str(path)+' matches manifest dimensions')
    meta=path.with_suffix('.png.meta').read_text(encoding='utf-8')
    for pattern,label in [(r'\btextureType: 8\b','Sprite texture'),(r'\bspriteMode: 1\b','Single sprite'),
                          (r'\balphaIsTransparency: 1\b','Alpha transparency'),(r'\benableMipMap: 0\b','No mipmaps')]:
        check(re.search(pattern,meta),str(path)+' / '+label)

prefabs=['PrimaryButton','SecondaryButton','SmallButton','IconButton','WorldButton','LevelButton',
         'UpgradeButton','WardrobeSlot','UIWindow','UISlider']
for name in prefabs:
    check((root/'Prefabs'/(name+'.prefab')).exists(),'Reusable prefab '+name)

known=set()
for directory in (Path('Assets'),Path('Packages'),Path('Library/PackageCache')):
    for path in directory.rglob('*.meta'):
        match=re.search(r'^guid: ([a-f0-9]{32})$',path.read_text(encoding='utf-8-sig',errors='replace'),re.M)
        if match: known.add(match[1])
paths=[Path('Assets/_Trapple/Resources/Storybook/GameInterface.prefab'),
       Path('Assets/_Trapple/UI/Illustrated/Prefabs/TrappleUI.prefab'),
       Path('Assets/_Trapple/Prefabs/Hub/WorkshopSystem.prefab'),
       Path('Assets/_Trapple/Scenes/Hub/Hub.unity'),Path('Assets/_Trapple/Scenes/Menu/TrappleUI.unity')]
font_meta=(root/'Fonts/Thaleah SDF.asset.meta').read_text()
font_guid=re.search(r'guid: (\w+)',font_meta)[1]
for path in paths:
    text=path.read_text(encoding='utf-8-sig')
    local_ids=set(re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M))
    for reference in re.findall(r'\{fileID: [^}]+\}',text):
        guid=re.search(r'guid: ([a-f0-9]{32})',reference)
        if guid:
            check(guid[1].startswith('0000000000000000') or guid[1] in known,str(path)+' resolves GUID '+guid[1])
        else:
            file_id=re.search(r'fileID: (-?\d+)',reference)[1]
            check(file_id=='0' or file_id in local_ids,str(path)+' resolves local reference '+file_id)
    for match in re.findall(r'm_fontAsset: \{fileID: \d+, guid: ([a-f0-9]+)',text):
        check(match==font_guid,str(path)+' uses original Thaleah')

view=paths[0].read_text(encoding='utf-8-sig')
buttons=re.search(r'\n  buttons:\n((?:  - \{fileID: -?\d+\}\n)+)',view)
check(buttons is not None and len(re.findall('fileID:',buttons[1]))==17,'All 17 shared actions have serialized buttons')
unchanged=subprocess.check_output(['git','diff','f179e5a','--name-only','--',
    'Assets/_Trapple/Scripts/Player','Assets/_Trapple/Scripts/Traps','Assets/_Trapple/Settings/HubConfiguration.asset',
    'Assets/_Trapple/Scenes/Levels','Assets/_Trapple/Scenes/DeceptiveForest'],text=True).strip()
check(not unchanged,'Player, traps, level scenes and economy configuration preserved')
failed=sum(not passed for passed,_ in checks)
print(f'{len(checks)-failed}/{len(checks)} asset checks passed; {len(entries)} sprites; {failed} failures.')
raise SystemExit(1 if failed else 0)
