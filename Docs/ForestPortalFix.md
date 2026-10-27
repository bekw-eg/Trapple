# Deceptive Forest в порталах Hub

Причина закрытого мира: у Forest стоял `initiallyUnlocked = false`, зависимость
от прохождения Meadow и пустой `levelScenes`. Дополнительно порталы не хранили
идентификатор мира, а профиль HubPreview не включал сцену леса.

Исправление:

- Forest доступен без предварительного прохождения Meadow.
- Первый доступный уровень Forest связан с существующей сценой
  `Assets/_Trapple/Scenes/DeceptiveForest/DeceptiveForest_Level02.unity`.
- Portal0 / Portal1 / Portal2 выбирают Forest / Factory / Lava соответственно.
  Существующие сцены Hub и TrappleUI наследуют привязки из prefab.
- Сцена Forest включена в профили запуска, использующие отдельные списки сцен.
- Новые привязки сохраняются при пересоздании Hub через существующий builder.

Сохранения, прогресс прохождения, цены и геометрия уровней не изменяются.
Factory и Lava сохраняют прежние условия открытия. Остальные запланированные
уровни Forest остаются недоступными, пока им не назначены сцены.

Тесты, захваты экранов, сборка и проверочные игровые запуски не выполнялись
по просьбе пользователя. Выполнены просмотр изменений и сохранение ассетов
через Unity Editor API. Несохранённые правки открытой сцены сохранены в редакторе
без записи в файлы этого исправления.

Для применения вручную предусмотрено меню
`Tools → Trapple → Connect Hub World Portals`. После импорта достаточно открыть
Hub и начать Play Mode: взаимодействие E с лесным порталом выбирает Forest,
а PLAY запускает существующую сцену леса.

Изменённые файлы:

- `Assets/_Trapple/Editor/HubBuilder.cs`.
- `Assets/_Trapple/Editor/ForestPortalSetup.cs` и его `.meta`.
- `Assets/_Trapple/Scripts/Hub/HubInteractable.cs`.
- `Assets/_Trapple/Scripts/Hub/HubInteractionController.cs`.
- `Assets/_Trapple/Scripts/Hub/UI/HubUIController.cs`.
- `Assets/_Trapple/Scripts/Hub/UI/Illustrated/TrappleUIManager.cs`.
- `Assets/_Trapple/Scripts/Hub/UI/Illustrated/WorldMapPanelUI.cs`.
- `Assets/_Trapple/Prefabs/Hub/Portal0.prefab`, `Portal1.prefab`, `Portal2.prefab`.
- `Assets/_Trapple/Settings/HubConfiguration.asset`, `HubPreview.asset`,
  `BossPrototype.asset`.
- `Docs/ForestPortalFix.md`, `Docs/StorybookUI.md`.
