# Forest Guardian — источник графики

Создано встроенным инструментом `image_gen` (навык `imagegen`), прозрачный PNG.
Исходник сохранён в `Assets/_Trapple/Art/ForestGuardian/GuardianKit.png`.
Фактический результат — 1280×1280; шесть деталей нарезаны через Unity Sprite API
по визуально проверенным прямоугольникам. Геометрия и анимация собираются в prefab.
Копирование в проект сохраняет исходный alpha-канал. Чужие изображения не использовались.

Точный prompt:

```text
Use case: stylized-concept. Asset type: modular 2D sprite atlas for Trapple cartoon platformer Forest Guardian, a huge living tree boss. Create a TRANSPARENT square 1536x1536 sprite sheet with EXACTLY 3 columns and 2 rows of equal 512x512 cells. Each isolated component stays inside its cell with clear padding; no text, labels, grid lines or backgrounds. Soft hand-painted cartoon storybook game style, rich textured warm brown bark, moss emerald green, dark outline and soft form shadows, clear silhouette, polished professional 2D game art. Orthographic front view. Cell TOP LEFT: massive tapered tree trunk torso ONLY, with expressive stern carved mouth and eyebrows, WITHOUT eyes, without arms, without roots, without crown; broad shoulders, natural jagged top. Cell TOP MIDDLE: small rounded lush leafy green crown ONLY to place atop torso. Cell TOP RIGHT: long crooked muscular branch arm ONLY, oriented from upper left attachment toward lower right fist made of twigs; isolated. Cell BOTTOM LEFT: broad claw-like gnarled tree root foot ONLY pointing right. Cell BOTTOM MIDDLE: a PAIR of luminous amber expressive eyes ONLY, no face or trunk, horizontal aligned. Cell BOTTOM RIGHT: one curled pointed green leaf ONLY. Consistent light from top left. All 6 parts fully visible, nonoverlapping and separated by transparent margins. This is a riggable character kit; do not draw a full assembled tree anywhere.
```
