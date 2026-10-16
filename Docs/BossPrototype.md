# Boss and systems prototype

Unity **6000.6.0f1**, Built-In Render Pipeline, existing Input Manager. Based on
`chore/add-agents-guidelines` and `feature/player-and-traps`; their original
commits/authors are preserved. New work is authored by `AlikhanM29` on
`feature/boss-and-systems`.

## Run and play

1. Open this repository in Unity 6000.6.0f1 and wait for script compilation.
2. Open `Assets/_Trapple/Scenes/Menu/MainMenu.unity` and press Play.
3. **PLAY** opens the existing Tutorial. **BOSS PROTOTYPE** opens BossTest.
   You can also open `Assets/_Trapple/Scenes/BossArenas/BossTest.unity` directly.
4. Move with **A/D** or **arrows**, jump with **Space**. Yellow markers are safe
   warnings; red/brown roots, falling branches and the glowing platform are lethal.
5. After each attack the trunk turns gold for **3.2 seconds**. Return to the cyan
   lever on the left and press **E** within **1.6 units**. A falling stone deals
   **25 damage**, once per opening. Pressing E early, far away or repeatedly
   cannot damage the boss.
6. Four successful openings defeat the 100 HP boss. Phase two starts **below**
   50 HP, speeding up attacks and recovery intervals. The warning never becomes
   shorter than 0.65 seconds; the lever window stays 3.2 seconds.
7. **Escape** pauses/resumes; pause UI also offers restart, settings and menu.
   Victory UI offers retry and menu. Player death reloads the arena through the
   existing `PlayerDeath.Die()`.

The arena intentionally uses simple shapes and the original player prefab.
The tutorial goal remains unchanged; access the boss through the main menu.
No combat input, weapon, alternate player or extra level progression is added.

## Scene loading without shared settings changes

`Assets/_Trapple/Settings/BossPrototype.asset` is a Unity build profile containing
MainMenu, Tutorial and BossTest. `BossPrototypePlayMode` temporarily selects it
when entering Play from a Trapple scene and restores the prior profile afterward.
This lets the existing player death reload any prototype scene using SceneManager.
Global `ProjectSettings/EditorBuildSettings.asset` is not modified.

For a Windows executable, use **Trapple → Build Boss Prototype (Windows)**.
The build command explicitly includes the same three scenes, with MainMenu first.
The result is `Builds/BossPrototype/Trapple.exe` (ignored by Git).

## Inspector and integration

| Component | Settings / responsibility |
| --- | --- |
| `GameManager` | First Level; singleton, scene loading, Playing/Paused/Victory/MainMenu. Created automatically even when opening Tutorial directly. |
| `GameInterface` | Background/accent colours and hover speed; main menu, pause, volume and victory, boss HP/status. Uses the project's existing IMGUI system without adding packages. |
| `BossHealth` | Maximum Health; read-only current HP, vulnerability gate, HealthChanged and Died events. |
| `BossPhaseController` | Phase Two Threshold and Duration Multiplier. Strictly below threshold enters phase two. |
| `BossController` | Initial Delay, Attack Interval, Vulnerability Duration, trunk renderer; readable state machine and FightFinished event. |
| `BossAttack` | Player Target, warning and hazard references, Arena X, Floor Y, all attack durations, minimum warning and branch starting height. Cycles roots → branch → platform. |
| `BossHazard` | Reusable trigger adapter. Calls `Trapple.Player.PlayerDeath.Die()`; does not implement player death itself. |
| `ArenaLever` | Boss, player, damage, activation distance and stone visual/positions. Allows one environmental hit per vulnerability window. |

Reusable boss: `Assets/_Trapple/Prefabs/Bosses/AncientTree.prefab`. On a new arena,
assign the player target and lever references, reposition the trap to the target
platform, configure floor/arena bounds and include the scene in your build profile.
Boss parts are intentionally arranged at arena coordinates for this first prototype.
Subscribe to `BossController.FightFinished` for later level progression. The boss
also calls `GameManager.CompleteBossFight()` for the prototype victory screen.

Master volume changes `AudioListener.volume` and is stored in PlayerPrefs.
Sound assets are not present yet; the settings panel states this explicitly.
Button hover animations use unscaled time, so they work while paused.

## Regenerate generated assets

**Trapple → Rebuild Boss Prototype (replaces menu, arena and boss)** serializes
the two scenes, AncientTree prefab and build profile using Unity Editor APIs.
It replaces those prototype assets; save or commit custom edits first. It never
runs TutorialBuilder, edits the player/trap prefabs or changes shared settings.

## Checks

Run all PlayMode tests in **Window → General → Test Runner**. The added
`BossSystemsTests` exercises health/vulnerability, strict phase threshold,
one-hit lever/range, all three warnings, hazard contact through the real player,
death reload while paused, frozen attack timers, victory cleanup and scene flow.
Existing TutorialTests cover the original player and traps.

Command-line checks (close the Editor first):

```powershell
$editor = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
$project = (Get-Location).Path
& $editor -batchmode -nographics -projectPath $project `
  -activeBuildProfile 'Assets/_Trapple/Settings/BossPrototype.asset' `
  -runTests -testPlatform PlayMode `
  -testResults "$project\Logs\boss-playmode-results.xml" `
  -logFile "$project\Logs\boss-playmode.log" | Out-Host
& $editor -batchmode -nographics -projectPath $project `
  -executeMethod Trapple.Editor.BossPrototypeBuilder.BuildWindows -quit `
  -logFile "$project\Logs\boss-build.log" | Out-Host
```

Manual UI checks: open menu at 1280×720 and resize the Game view, exercise all
buttons and hover states, change/reopen volume, resume/restart/return from pause,
dodge each attack, operate the lever only during openings, and confirm victory.

### Validation performed

- Unity 6000.6.0f1 PlayMode: **27 passed, 0 failed**, including the existing
  tutorial suite and a regression for starting the encounter on the first scene load.
- Windows development build with all three scenes: succeeded.
- Real Editor Play entry/exit: prototype profile selected and previous profile restored.
- `git diff --check`: passed; existing player/trap code, Tutorial, packages and
  ProjectSettings remain identical to the player-and-traps branch.
- Manual mouse/keyboard playthrough and visual review remain to be performed.
  The hidden player did not provide usable framebuffer screenshots, so no visual
  approval is claimed. Sound playback cannot be checked until sound assets exist.
