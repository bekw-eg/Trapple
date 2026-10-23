# Прозрачный фон мастерской

У домика убран тёмный прямоугольный фон. Через внешнюю область теперь виден пейзаж хаба, а дверной проём остаётся тёмным и непрозрачным.

Исходный `house_empty_doorway.png` и девять кадров двери сохранены без изменения. Материал `CottageCutout.mat` использует только альфа-канал нового `cottage_cutout.png`; цвет берётся из прежнего спрайта 469×347. Порог 0,5 даёт чёткий пиксельный контур. Размер домика, pivot, положение двери, 10 FPS и порядок кадров не менялись.

## Файлы и пересборка

- `Assets/_Trapple/Art/Workshop/Door/cottage_cutout.png` — контур с прозрачным фоном, подготовленный встроенным imagegen. Исходный результат 1458×1079 сохранён целиком; при отрисовке используются нормализованные UV и только его альфа-канал.
- `Assets/_Trapple/Art/Workshop/Door/CottageCutout.mat` и `Assets/_Trapple/Shaders/CottageCutout.shader` — материал и шейдер для исходного домика.
- `WorkshopDoorAssets` импортирует контур без сжатия, mipmaps и изменения размера, с Point-фильтром. `WorkshopBuilder` назначает материал префабу и обеим сценам хаба через Unity Editor API.
- Команда **Tools → Trapple → Build 2D Workshop** сохраняет новый материал при пересборке.
- `WorkshopDoorAssets.BuildAndCapture` дважды пересобирает мастерскую и сохраняет девять RGBA-рендеров с настоящей прозрачностью в `Builds/WorkshopDoorOverlays/Frames/`.

Избранные рендеры находятся в `Docs/Images/WorkshopBackground/`. Проверки и отчёты — `Docs/Validation/WorkshopBackground-*`. Полный список изменённых файлов — `Docs/WorkshopBackground-ChangedFiles.txt`.

## Запрос imagegen

Использован встроенный инструмент с `transparent_background=true`. CLI/API-режим не использовался. Исходник — `house_empty_doorway.png`.

```text
Use case: background-extraction. EDIT TARGET: the provided 469 x 347 pixel Unity sprite. Remove ONLY the exterior dark charcoal rectangular backdrop surrounding this cottage, replacing it with genuine alpha transparency. This is an exact sprite cutout, not a redraw. Keep the full original 469 x 347 canvas, and keep every foreground pixel, the cottage's position, scale, colors, outlines, all plants, flowers, smoke puffs and hanging cog sign exactly unchanged. CRITICAL: the dark arched doorway inside the house at approximately x=110..156, y=216..309 must stay opaque and dark; do NOT make the interior doorway transparent and do NOT add a closed door. A separate animated door will overlay this image at crop (91,198), so registration must remain exact. Do not crop, recenter, resize, upscale, repaint, smooth, add a shadow, add a checkerboard, or change any house details. Output the same composition as transparent RGBA PNG. Only remove exterior background.
```

Инструмент выдал увеличенный контур. Чтобы его масштаб не менял пиксели домика и положение двери, в игре оставлен оригинальный спрайт; новый результат служит только маской прозрачности.

## Проверки

Unity 6000.6.0f1:

- EditMode: **26/26**, без пропусков. В том числе назначение материала и маски, прозрачные внешние точки и непрозрачность всего проверяемого прямоугольника внутри дверного проёма.
- Полный PlayMode: **51/51**, без пропусков. Проверены все кадры открытия/закрытия, неподвижный домик, разворот/сброс и вход/выход/повторный вход в мастерскую; прежние тесты также прошли.
- GPU D3D11: девять RGBA-рендеров 469×347. Внешние контрольные точки имеют alpha=0; дверной проём — alpha=255. Альфа-каналы всех девяти кадров одинаковы. Все пиксели вне кропа двери неподвижны. Цвета непрозрачных пикселей совпадают с исходными превью с погрешностью не более одного уровня RGB.
- Все **24** исходных файла/референса побайтно совпадают с версией до удаления фона. Новый файл imagegen скопирован в проект без изменений альфа-канала.
- Повторная сборка обеих сцен: одна система мастерской, один интерфейс, одна диафрагма и один EventSystem; ссылки назначены. Windows x64 development build: **Success**.
- Windows D3D11 player: **PASS** — вход, выход, восстановление управления/физики и повторный вход/выход. Скриншот `Docs/Images/WorkshopBackground/hub.png` показывает реальный хаб без прямоугольного фона.

## Ограничения

Материал использует UV полного исходного спрайта; домик не следует упаковывать в Sprite Atlas с изменением UV. Кадры двери остаются обычными спрайтами. Встроенный Render Pipeline и Windows D3D11 проверяются в этой задаче; другие платформы и Render Pipeline не проверялись.
