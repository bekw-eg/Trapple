# Источники графики

Исходные материалы пользователя и проекта остаются неизменёнными.

- `Docs/DeceptiveForest/LevelReference.png`: копия
  `C:/Users/zdeat/Downloads/Enchanted Forest Platformer Adventure.png`.
  Композиционный референс, не игровая сцена и не sprite sheet.
- `Assets/_Trapple/UI/DeceptiveForest/Art/`: 16 PNG из пользовательского
  `Trapple_Deceptive_Forest_Level2_UI_Kit.zip`, включая общий лист-референс.
  Набор не содержит надписей: тексты и кнопки создаёт UI Toolkit.
- `Assets/_Trapple/Resources/AppleMeadow/`: ранее подготовленные производные
  предоставленных `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet1–7.png`.
  Использованы Ground, Island, Tree, Bush, Grass, Flowers, Stump, Crate, Apple,
  Spikes, Sign, Warning, Flag. Источники и производные не изменены.
- `Assets/_Trapple/Art/Hub/Leaf.png`: существующая декоративная текстура листа.
- `Assets/_Trapple/Prefabs/Player/Player.prefab` и
  `Assets/_Trapple/Art/Hub/Source/AppiAnimation.png`: существующий персонаж,
  его физика, размеры, анимации, Point-фильтрация и исходный PNG сохранены.

Две недостающие производные сделаны встроенным **imagegen**, без CLI/API fallback.
Они могут отличаться отдельными пикселями от референса. Это явно производные,
а не утверждение о точном пиксельном извлечении.

## ForestBackdrop.png

Сохранён в `Assets/_Trapple/Art/DeceptiveForest/ForestBackdrop.png`.
Источник: указанный выше лесной концепт. Bilinear, без mipmaps и компрессии.
Промпт:

> Edit target: supplied Trapple forest concept. Create a production 2D background-only scenery plate based on this exact forest art. Preserve the warm hand-painted forest palette, pale teal mist, distant huge woodland trunks, soft layered canopies, sunbeams from upper right and waterfall valley atmosphere. Remove ALL foreground playable ground, platforms, bridges, stumps, spikes, apple, player character, portal, signs, items and any gameplay objects. Fill their areas with continuous distant misty forest and low contrast distant bushes. Wide 2.3:1 panorama, side-view background, full bleed, no text or UI. Bottom half must be softly layered forest mist, no walkable surfaces or strong horizontal lines. No close large foreground trunks. It is a backdrop behind separate real sprites, never a full level mockup.

## Stick.png

Сохранён в `Assets/_Trapple/Art/DeceptiveForest/Stick.png`.
Источник: существующий `Assets/_Trapple/Art/Hub/Source/WorldMechanicsReference.png`.
Сохранён реальный alpha, Point-фильтрация. Промпт:

> Use case background extraction. Edit target supplied Trapple World Mechanic Assets reference. Extract ONLY the brown thrown STICK with small green leaf in the bottom of SECOND COLUMN (STICK PROJECTILE). One single clean isolated horizontal branch sprite centered on real transparent background, fill the canvas with branch and small leaf but leave 10% clear margin. Preserve its chunky crisp pixel-art blocks, warm brown highlights, black outline, and green leaf. Remove all yellow, text, motion speed lines, other icons and characters. No ground, no shadow, no background. Production Unity 2D inventory/projectile sprite, no redesign. Transparent pixels around all outside edges.
