## Изменение

Раньше яблоки сразу завершали оба маршрута, первый босс жил только в BossTest, а лес не записывал победу над боссом. Теперь Apple Meadow заканчивается существующим боем Ancient Tree с рычагом E. Deceptive Forest после прежних ловушек и сбора 9 палок приводит на арену Forest Guardian.

В лесу подключены составной анимированный спрайтовый риг, три атаки с предупреждениями, вступление с пропуском Enter, броски F, HP и 60 секунд выживания. Победа возможна пятью попаданиями или по времени. Чекпоинт восстанавливает запас при Retry через прежний экран смерти. Существующие палки для преград сохраняют E/F и приоритет ввода.

Завершение использует прежний progression/save, обновляет все варианты карты и открывает следующий настроенный контент. Forest содержит одну реальную сцену, поэтому её конфигурация больше не требует прохождения трёх несуществующих уровней. Factory/Lava не создаются.

## Интеграция для команды

- Сцены Tutorial и DeceptiveForest_Level02 сохранены Unity Editor API; добавлена отдельная ForestGuardianTest.
- Новые префабы: ForestGuardian, GuardianHazard, StickPickup, StickProjectile. AncientTree и BossTest сохранены.
- Общие интерфейсы: WorldCompletion.Record/EnsureProgression, IWorldVictoryPresentation; BossController.completeLevelOnDeath по умолчанию true, интеграция Meadow задаёт false.
- GameManager, PlayerController, PlayerDeath и PlayerSpriteAnimator сохраняют прежние контракты.
- Повторная интеграция: Trapple → Boss progression → Build and integrate both worlds. Ручные правки внутри генерируемой ветки Forest Guardian Encounter следует переносить в builder/prefab.
- Русское описание архитектуры, Inspector и полного ручного сценария: Docs/ForestGuardian/README.md. Происхождение графики и точный prompt: ArtPrompt.md.

## Проверка

Фактические результаты Unity и ограничения приведены в Docs/ForestGuardian/Validation.md.

Проверено: 30/30 EditMode; по совокупности PlayMode прогонов 135 уникальных passed, 0 failed, 2 opt-in skipped. Окончательные прогоны: 19/19 бой и GPU-отрисовка, 4/4 первый босс с сохранением. Оба длинных маршрута пройдены production-физикой до вступлений боссов. Windows x64 Development build: Success. Четыре просмотренных снимка находятся в Docs/ForestGuardian/Images.

В Validation.md сохранены результаты первоначального полного прогона и повторных проверок после исправления устаревших ожиданий тестов, баланса листьев и попадания в Close на 4:3. Сводка каждого теста — TestResults.json. git diff --check пройден.

Нужен ручной проход новым игроком для оценки сложности и читаемости предупреждений. Чекпоинт хранит только текущий сеанс; пройденные миры сохраняются на диск существующим сервисом.
