# Trapple

Учебный 2D-платформер на Unity C#. В этой версии реализован только первый этап:
персонаж, три типа ловушек и один небольшой уровень с яблоком.

## Запуск

1. Установите **Unity 6000.6.0f1** через Unity Hub. Версия фиксируется в
   `ProjectSettings/ProjectVersion.txt`.
2. В Hub выберите **Add → Add project from disk**, укажите корень этого
   репозитория и откройте проект установленной версией.
3. Дождитесь импорта и откройте
   `Assets/_Trapple/Scenes/Levels/Tutorial.unity`.
4. Нажмите Play и щёлкните по окну Game, чтобы оно получало ввод.

**A / D** и **← / →** — движение, **Space** — прыжок. Дойдите до яблока:
появится `LEVEL COMPLETE`. После победы **R** перезапускает уровень.
После смерти уровень перезапускается автоматически через 0,65 секунды.

Проект использует Built-In Render Pipeline и встроенный **Input Manager**:
Project Settings → Player → Active Input Handling = **Input Manager (Old)**.
Input System, URP, Cinemachine и сторонние библиотеки для прототипа не нужны.
Ввод считывается в Update, физика применяется в FixedUpdate.

## Создание нового проекта через Unity Hub

В исходном репозитории Unity-проекта не было. Эта ветка уже содержит проект,
созданный установленным Unity Editor. Следующие шаги нужны только для нового
пустого репозитория; при клонировании этой ветки используйте инструкцию выше.

1. Установите Unity 6000.6.0f1 в Hub → Installs. Для сборки Windows нужен модуль
   Windows Build Support; для запуска в Editor он не требуется.
2. Выберите New project → эту версию Editor → **2D / 2D Core (Built-In Render
   Pipeline)**. Шаблон при необходимости загрузите в Hub. Назовите проект Trapple
   и выберите отдельную пустую папку.
3. В Project Settings → Editor установите Mode = 2D и Asset Serialization =
   Force Text; в Version Control установите Mode = Visible Meta Files.
4. В Player выберите Input Manager (Old), в Tags and Layers назначьте слой
   **Ground** индексу **6**. Добавьте Unity Test Framework 1.8.0 через Package
   Manager, если будете переносить автоматические тесты.
5. Перенесите `Assets/_Trapple` со всеми `.meta` и файлы `.gitignore` и
   `.gitattributes`. Не переносите Library, Temp, Logs и UserSettings.
6. Используйте **Trapple → Rebuild Tutorial (replaces generated assets)**.
   Команда создаёт сцену и префабы через Editor API и добавляет Tutorial в
   список сцен сборки. Она заменяет сгенерированные ресурсы: сохраните свои
   изменения перед её повторным запуском.

## Компоненты и настройка

| Компонент | Назначение и параметры Inspector |
| --- | --- |
| `PlayerController` | Скорость, ускорение, торможение, высота прыжка в мировых единицах, маска земли и расстояние проверки опоры, SpriteRenderer. |
| `PlayerDeath` | `public void Die()`, задержка перезапуска и высота смерти при падении. Повторные вызовы за попытку игнорируются. |
| `SpikeTrap` | Убивает при trigger/collision контакте; Armed позволяет включить или отключить опасность. |
| `FallingPlatform` | Активируется при приземлении сверху; задержка падения, гравитация, срок удаления после падения. |
| `HiddenTrap` | Отдельный trigger и дочерние шипы; ссылка Spikes, задержка, длительность и высота появления. |
| `TutorialCamera` | Цель, смещение и сглаживание слежения. |
| `TutorialGoal` | Яблоко, блокировка движения после победы и временное сообщение через IMGUI. |

У игрока CapsuleCollider2D, Rigidbody2D с Gravity Scale = 3, Freeze Rotation,
Continuous Collision Detection и Interpolate. Материал без трения предотвращает
застревание на вертикальных гранях. Земля и падающая платформа находятся на
слое Ground; trigger-коллайдеры и боковые стены не дают опору для прыжка.
Нажатие Space расходуется один раз: удержание и нажатие в воздухе не создают
дополнительный прыжок при приземлении.

Четыре готовых префаба находятся в `Assets/_Trapple/Prefabs/Player/` и
`Assets/_Trapple/Prefabs/Traps/`. Используйте копии префабов для интеграции;
сцена Tutorial содержит их связанные экземпляры. Простые PNG-фигуры создаются
редактором из пикселей и сохранены в `Art/Prototype`.

## API для второго разработчика

```csharp
using Trapple.Player;

// Например, из trigger атаки босса:
var player = other.GetComponentInParent<PlayerDeath>();
if (player != null)
    player.Die();
```

`IsDead` доступен для чтения. `Die()` немедленно отключает управление и физику,
затем загружает сцену, которой принадлежит игрок. Эта сцена должна быть добавлена
в список сцен сборки. Задержка смерти использует реальное время и работает при
`Time.timeScale = 0`; сам метод не меняет глобальную шкалу времени.
После перезагрузки новый экземпляр игрока начинает следующую попытку.
Меню, GameManager, боссы и другие миры в эту задачу не входят.

## Проверка в Unity

1. Убедитесь, что Console не содержит ошибок, откройте Tutorial и включите Play.
2. Проверьте обе пары клавиш движения, остановку, поворот и прыжок со ступеньки.
   Удерживайте Space и нажимайте его в воздухе: второго прыжка быть не должно.
3. Коснитесь красных шипов. Управление отключится, затем восстановится стартовая
   сцена. Повторные контакты не должны запускать несколько перезагрузок.
4. Наступите на оранжевую платформу над первым разрывом: через 0,4 секунды она
   падает. Контакт сбоку её не активирует. Успейте уйти или прыгнуть.
5. После мостика пройдите область x≈20–22: скрытые шипы появятся с задержкой.
   Область можно перепрыгнуть. Прыгните через следующие шипы и платформы.
6. Упадите в разрыв: ниже y=-8 должен сработать тот же перезапуск.
7. Дойдите до яблока около x=40. Проверьте `LEVEL COMPLETE`, остановку персонажа
   и перезапуск по R. Камера должна плавно следовать на всём пути.
8. Откройте Window → General → Test Runner и запустите PlayMode-тесты.
   Тесты загружают сохранённую Tutorial и проверяют настоящую физику Unity.

Для командной проверки (PowerShell, Editor должен быть закрыт):

```powershell
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
$project = (Get-Location).Path
& $editor -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testResults "$project\Logs\playmode-results.xml" -logFile "$project\Logs\playmode.log" | Out-Host
```

Для Windows-сборки можно использовать File → Build Profiles → Windows либо:

```powershell
& $editor -batchmode -nographics -projectPath $project -executeMethod Trapple.Editor.TutorialBuilder.BuildWindows -quit -logFile "$project\Logs\windows-build.log" | Out-Host
```

Сборки и журналы исключены из Git. Видимые `.meta`, проектные настройки, сцена,
префабы и исходники должны сохраняться в Git вместе.
