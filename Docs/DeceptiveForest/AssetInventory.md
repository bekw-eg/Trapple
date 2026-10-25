# Реестр графики Deceptive Forest — Level 2

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
| `Assets/_Trapple/Art/AppleMeadow/Sources/LevelReference.png` | 1031×312 | (255, 255) | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet1.png` | 980×617 | (255, 255) | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet2.png` | 988×721 | (255, 255) | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet3.png` | 1027×726 | (255, 255) | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet4.png` | 986×662 | (255, 255) | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet5.png` | 1003×656 | (255, 255) | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet6.png` | 958×717 | (255, 255) | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/AppleMeadow/Sources/Sheet7.png` | 1077×718 | нет | рисованный | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/DeceptiveForest/ForestBackdrop.png` | 1903×826 | нет | рисованный | Готов | Фоновая производная концепта; Bilinear, параллакс |
| `Assets/_Trapple/Art/DeceptiveForest/Stick.png` | 1536×1024 | (0, 254) | pixel art | Готов | Прозрачная палка, Point; подбор/бросок/слот |
| `Assets/_Trapple/Art/Hub/AppleMeadow/InteractablesAtlas.png` | 1536×1024 | (0, 254) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/AppleMeadow/SceneryPlate.png` | 1942×809 | нет | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/Cloud.png` | 512×160 | (0, 64) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/Glow.png` | 256×256 | (0, 164) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/Grass.png` | 128×128 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/Ground.png` | 512×128 | (255, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/HeroAtlas.png` | 1254×1254 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/Hill.png` | 1024×256 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/HubProps.png` | 2161×728 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/Leaf.png` | 128×64 | (0, 255) | существующий арт проекта | Готов / резерв | Переиспользован для лёгкой анимации листьев |
| `Assets/_Trapple/Art/Hub/MeadowBackdrop.png` | 1672×941 | нет | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Hub/Source/AppiAnimation.png` | 1448×1086 | (0, 255) | pixel art | Готовая нарезка | 20 кадров существующего Appi; Point, исходник неизменён |
| `Assets/_Trapple/Art/Hub/Source/AppiMotionReference.png` | 1448×1086 | нет | pixel art | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/Hub/Source/AppleHeroReference.png` | 1448×1086 | нет | pixel art | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/Hub/Source/PixelHubReference.png` | 1672×941 | нет | pixel art | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/Hub/Source/WorldMechanicsReference.png` | 1448×1086 | нет | pixel art | Нарезка / референс | Исходник сохранён; для уровня используются готовые производные |
| `Assets/_Trapple/Art/Prototype/Apple.png` | 64×64 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Prototype/Player.png` | 64×64 | (255, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Prototype/Spikes.png` | 64×64 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Prototype/Square.png` | 64×64 | (255, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/cottage_cutout.png` | 1458×1079 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/door_spritesheet_3x3.png` | 240×360 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_00.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_01.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_02.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_03.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_04.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_05.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_06.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_07.png` | 80×120 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/doors_png/door_08.png` | 80×120 | (0, 233) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Door/house_empty_doorway.png` | 469×347 | (255, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/Interior.png` | 1672×941 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/SeedsSign.png` | 2172×724 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/UpgradeFrame.png` | 1448×1086 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Art/Workshop/WorkshopSign.png` | 2172×724 | (0, 255) | существующий арт проекта | Готов / резерв | Не изменён; не требуется для уровня |
| `Assets/_Trapple/Resources/AppleMeadow/Apple.png` | 135×141 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Bridge.png` | 593×195 | (0, 255) | рисованный | Готов | Доступен, для леса не требуется |
| `Assets/_Trapple/Resources/AppleMeadow/Bush.png` | 241×166 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Crate.png` | 185×182 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Flag.png` | 212×217 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Flowers.png` | 227×130 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Grass.png` | 192×98 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Ground.png` | 170×50 | (255, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Island.png` | 192×126 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Landscape.png` | 932×325 | (255, 255) | рисованный | Готов | Доступен, для леса не требуется |
| `Assets/_Trapple/Resources/AppleMeadow/Pause.png` | 552×575 | (0, 255) | рисованный | Готов | Доступен, для леса не требуется |
| `Assets/_Trapple/Resources/AppleMeadow/Rocks.png` | 175×150 | (0, 255) | рисованный | Готов | Доступен, для леса не требуется |
| `Assets/_Trapple/Resources/AppleMeadow/Sign.png` | 188×223 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Spikes.png` | 177×77 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Stump.png` | 259×190 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Tree.png` | 434×425 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/Resources/AppleMeadow/Warning.png` | 175×187 | (0, 255) | рисованный | Готов | Переиспользован в сцене; Bilinear; отдельный SpriteRenderer |
| `Assets/_Trapple/UI/DeceptiveForest/Art/01_level_title_panel.png` | 973×314 | (0, 254) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/02_small_HUD_panel.png` | 393×141 | (0, 253) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/03_hint_interaction_panel.png` | 504×244 | (0, 253) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/04_universal_button_moss.png` | 299×102 | (0, 253) | рисованный UI | Готов | Импортированный запасной вариант, в сцене не используется |
| `Assets/_Trapple/UI/DeceptiveForest/Art/04_universal_button_parchment.png` | 313×104 | (0, 253) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/04_universal_button_wood.png` | 326×106 | (0, 253) | рисованный UI | Готов | Импортированный запасной вариант, в сцене не используется |
| `Assets/_Trapple/UI/DeceptiveForest/Art/05_pause_menu_panel.png` | 552×384 | (0, 254) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/06_death_retry_panel.png` | 466×379 | (0, 253) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/07_level_complete_panel.png` | 505×400 | (0, 254) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/08_item_slot_hexagonal.png` | 144×158 | (0, 254) | рисованный UI | Готов | Импортированный запасной вариант, в сцене не используется |
| `Assets/_Trapple/UI/DeceptiveForest/Art/08_item_slot_round.png` | 122×146 | (0, 253) | рисованный UI | Готов | Импортированный запасной вариант, в сцене не используется |
| `Assets/_Trapple/UI/DeceptiveForest/Art/08_item_slot_square.png` | 136×149 | (0, 253) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/08_item_slot_square_dark.png` | 132×153 | (0, 253) | рисованный UI | Готов | Импортированный запасной вариант, в сцене не используется |
| `Assets/_Trapple/UI/DeceptiveForest/Art/09_information_card.png` | 485×218 | (0, 253) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/10_dark_overlay.png` | 466×215 | (0, 252) | рисованный UI | Готов | UI Toolkit; текст программный; исходные пропорции |
| `Assets/_Trapple/UI/DeceptiveForest/Art/Trapple_Deceptive_Forest_Level2_full_asset_sheet.png` | 1536×1024 | (0, 254) | рисованный UI | Только референс | Общий лист; не используется как интерфейс |
