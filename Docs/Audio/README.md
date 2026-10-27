# Trapple — пакет производства саундтрека

Статус: **готовые промпты и музыкальная спецификация; WAV пока не созданы**.
Это предусмотренный в ТЗ вариант при недоступности генерации аудио.
26 октября 2026 года подключение Runway подтверждено, но запрос музыки
отклонён с `paid_plan_required`. Задача генерации и аудиофайл не созданы.

Пакет охватывает 10 музыкальных тем: 11 фоновых файлов с учётом двух фаз
Heart of Magma, а также 5 отдельных коротких джинглов. Все композиции
объединяет семинотный мотив Appi. Предлагаемые ноты сочинены для этого задания;
сходство будущих сгенерированных результатов ещё необходимо проверить на слух.

## С чего начать

1. Открыть [MusicalIdentity.md](MusicalIdentity.md): мотив, гармония и характер.
2. Выбрать композицию в [TrackCatalog.json](TrackCatalog.json) или таблице ниже.
3. Скопировать целиком её файл из [Prompts](Prompts). Каждый промпт самодостаточен
   и написан по-английски для передачи музыкальному генератору или композитору.
4. Сначала утвердить Main Theme. Для остальных треков использовать ту же
   спецификацию мотива и, если генератор поддерживает, собственный утверждённый
   аудиореференс. Доступный сейчас инструмент Runway принимает только текст.
5. Выполнить [ProductionChecklist.md](ProductionChecklist.md), затем положить
   проверенные WAV в подготовленные папки `Assets/Audio/Music/`.
6. Внедрять музыку по [UnityIntegration.md](UnityIntegration.md).

Ожидаемые длительности, темп и формат — требования к будущему результату,
а не измерения существующих файлов. Текстовый генератор может не соблюсти
ноты, длину, темп или шов; потребуется музыкальный монтаж и проверка.
Runway возвращает MP3 фиксированной для модели длины, а не готовый PCM-мастер
указанной продолжительности. Конвертация MP3 в WAV не восстанавливает утраченное
качество. Для полноценного мастера предпочтителен исходный lossless-экспорт или
новый рендер аранжировки в DAW.

## Содержание

| Композиция | BPM | Такты | Целевая длительность | Промпт |
| --- | ---: | ---: | ---: | --- |
| Trapple — Main Theme | 120 | 48 | 96.000 s | [01_main_theme](Prompts/01_main_theme.txt) |
| Appi's Little World | 96 | 60 | 150.000 s | [02_little_world](Prompts/02_little_world.txt) |
| Appi's Workshop | 100 | 48 | 115.200 s | [03_workshop](Prompts/03_workshop.txt) |
| The Friendly Trap | 120 | 104 | 208.000 s | [04_friendly_trap](Prompts/04_friendly_trap.txt) |
| Whispering Tricks | 112 | 96 | 205.714 s | [05_whispering_tricks](Prompts/05_whispering_tricks.txt) |
| Roots of Fury | 144 | 88 | 146.667 s | [06_roots_of_fury](Prompts/06_roots_of_fury.txt) |
| Gears Gone Wrong | 128 | 112 | 210.000 s | [07_gears_gone_wrong](Prompts/07_gears_gone_wrong.txt) |
| Mechanical Mayhem | 160 | 96 | 144.000 s | [08_mechanical_mayhem](Prompts/08_mechanical_mayhem.txt) |
| Burning Steps | 144 | 120 | 200.000 s | [09_burning_steps](Prompts/09_burning_steps.txt) |
| Heart of Magma — Phase 1 | 160 | 128 | 192.000 s | [10_heart_of_magma_phase1](Prompts/10_heart_of_magma_phase1.txt) |
| Heart of Magma — Phase 2 | 160 | 128 | 192.000 s | [11_heart_of_magma_phase2](Prompts/11_heart_of_magma_phase2.txt) |
| Level Complete | 120 | — | 4.000 s | [12_level_complete](Prompts/12_level_complete.txt) |
| Victory | 120 | — | 6.500 s | [13_victory](Prompts/13_victory.txt) |
| Game Over | 120 | — | 4.000 s | [14_game_over](Prompts/14_game_over.txt) |
| Boss Defeated | 120 | — | 8.000 s | [15_boss_defeated](Prompts/15_boss_defeated.txt) |
| Upgrade Purchased | 120 | — | 1.500 s | [16_upgrade_purchased](Prompts/16_upgrade_purchased.txt) |

Полный цикл каждого фонового трека начинается в кадре 0 и заканчивается
перед `loop_end_frame_exclusive` из каталога. Кадр означает одну стереопару
сэмплов, а не сумму сэмплов двух каналов. В каталоге указан целевой рендер
48 kHz / 24-bit PCM / stereo; ТЗ также допускает 44.1 kHz и 16-bit PCM.
При выборе другого sample rate пересчитать все границы.

`Reference/Appi_Motif.mid` — только семь MIDI-нот, темп и размер, без аудиозаписи
и без библиотек инструментов. Его можно открыть в DAW для точного ввода мотива;
это не готовая музыкальная композиция и не клип для Unity.

Runtime-скрипты, сцены, префабы и настройки проекта в этой поставке не изменены.
Предложение MusicManager описано отдельно, поскольку аудиоклипы ещё не получены.
Фабрика, крепость и соответствующие боссы остаются планируемым контентом.

Результаты проверки и ограничения: [Validation.md](Validation.md).
