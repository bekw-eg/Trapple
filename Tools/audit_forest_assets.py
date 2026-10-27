"""Read-only image audit; does not modify source artwork. Requires Pillow."""
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folders = [root / 'Assets/_Trapple/Art', root / 'Assets/_Trapple/Resources/AppleMeadow',
           root / 'Assets/_Trapple/UI/DeceptiveForest/Art']
used_meadow = {'Ground', 'Island', 'Tree', 'Bush', 'Grass', 'Flowers', 'Stump', 'Apple',
               'Crate', 'Spikes', 'Sign', 'Warning', 'Flag'}
rows = []
for path in sorted(p for folder in folders for p in folder.rglob('*') if p.suffix.lower() in ('.png', '.jpg', '.jpeg')):
    relative = path.relative_to(root).as_posix()
    with Image.open(path) as image:
        alpha = str(image.getchannel('A').getextrema()) if 'A' in image.getbands() else 'нет'
        dimensions = f'{image.width}×{image.height}'
    if '/Sources/' in relative or '/Source/' in relative:
        category, usage = 'Нарезка / референс', 'Исходник сохранён; для уровня используются готовые производные'
        style = 'pixel art' if '/Hub/' in relative else 'рисованный'
        if path.stem == 'AppiAnimation':
            category, usage = 'Готовая нарезка', '20 кадров существующего Appi; Point, исходник неизменён'
    elif '/UI/DeceptiveForest/' in relative:
        category, style = 'Готов', 'рисованный UI'
        usage = 'UI Toolkit; текст программный; исходные пропорции'
        if path.stem.endswith('full_asset_sheet'): category, usage = 'Только референс', 'Общий лист; не используется как интерфейс'
        elif path.stem in ('04_universal_button_moss', '04_universal_button_wood', '08_item_slot_hexagonal', '08_item_slot_round', '08_item_slot_square_dark'):
            usage = 'Импортированный запасной вариант, в сцене не используется'
    elif '/DeceptiveForest/' in relative:
        category = 'Готов'
        style = 'pixel art' if path.stem == 'Stick' else 'рисованный'
        usage = 'Прозрачная палка, Point; подбор/бросок/слот' if path.stem == 'Stick' else 'Фоновая производная концепта; Bilinear, параллакс'
    elif '/AppleMeadow/' in relative and '/Resources/' in relative:
        category, style = 'Готов', 'рисованный'
        usage = 'Переиспользован в сцене; Bilinear; отдельный SpriteRenderer' if path.stem in used_meadow else 'Доступен, для леса не требуется'
    else:
        category, style, usage = 'Готов / резерв', 'существующий арт проекта', 'Не изменён; не требуется для уровня'
        if path.stem == 'Leaf': usage = 'Переиспользован для лёгкой анимации листьев'
    rows.append(f'| `{relative}` | {dimensions} | {alpha} | {style} | {category} | {usage} |')

header = '''# Реестр графики Deceptive Forest — Level 2

Проверка читает фактические файлы, размеры и диапазон alpha через Pillow.
Расширение каждого изображения указано в пути; размеры приведены в пикселях.
Для полного локального пути прибавьте путь из таблицы к корню проекта:
`C:/Users/zdeat/OneDrive/Документы/ChatGPT/Trapple/`.

Appi использует существующую нарезку анимации. Движение дерева, палки и платформ
выполняется трансформациями отдельных объектов, а не несуществующими кадрами.
Существующие производные AppleMeadow уже имеют прозрачность; повторно не обрабатывались.
В UI kit есть alpha, но также полупрозрачный зелёный фон вокруг декора; это часть
поставленного оформления, а не обещание идеального выделения каждого листа.
Декоративные панели сохраняют пропорции, поэтому 9-slicing им не требуется.

Отсутствуют: готовая отдельная палка, чистый лесной фон без игровой геометрии,
звуки, кадры удержания/броска Appi, отдельный набор анимаций состояний кнопок.
Первые два пробела закрыты производными через imagegen (см. ArtSources.md).
Звук и новые анимации Appi не добавлены. Состояния кнопок заданы USS.
LevelReference.png — исходный концепт из Downloads, только композиционный референс.

| Путь | Размер | Alpha min/max | Стиль | Категория | Использование / обработка |
| --- | --- | --- | --- | --- | --- |
'''
(root / 'Docs/DeceptiveForest/AssetInventory.md').write_text(header + '\n'.join(rows) + '\n', encoding='utf-8')
print(f'Audited {len(rows)} images; source artwork unchanged.')
