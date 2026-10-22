# Trapple hub

Open `Assets/_Trapple/Scenes/Hub/Hub.unity` in Unity **6000.6.0f1** and press Play.
The main menu's Play button also opens Hub when it is included in the active build.
The existing boss-only build retains its Tutorial entry.

Move with A/D or arrows, jump with Space, approach a station and press E.
Escape opens settings during gameplay and closes an open window. Space only
jumps during gameplay; the HUD never retains keyboard focus after a modal closes.
Tab / Shift+Tab and Enter navigate controls inside open windows.
Opening a window disables the existing `PlayerController` through its public API;
the world keeps animating at normal time. A short closing transition restores the
previous control state, including when a window is reopened during that transition.

## Scene and artwork

The safe meadow contains the workshop on the left, a wardrobe beside it, the
existing player at the centre, and animated portal stations on the right.
There are three scenery layers, drifting clouds, swaying grass and falling leaves.
The camera follows horizontally with smoothing. Sprites use the current built-in
renderer; no URP or new input system is required.

`Assets/_Trapple/UI/Hub/Hub.uxml` and `Hub.uss` contain the shared visual system.
`HubPanelSettings.asset` scales from a 1600×900 design resolution. All panels are
designed for 1920×1080, 1600×900 and 1366×768. The reusable stations and player
variant are saved in `Assets/_Trapple/Prefabs/Hub/`.

The base Appi uses 20 frames sliced directly from the supplied transparent
animation PNG: 4 idle, 6 walking right, 6 walking left and 4 jump/landing poses.
The image pixels are unchanged. `PlayerSpriteAnimator` drives presentation from
existing velocity and ground contact; the original PlayerController and collider
settings are unchanged. The shared player prefab supplies these visuals to Hub,
Tutorial and the boss arena. Its three world-linked cosmetic previews remain
available in the wardrobe, using the earlier supplied designs.

The workshop, wardrobe and portals now use the supplied pixel-art designs with
their dark background removed. Forest/Factory/Lava card illustrations are native
sprite rectangles from the supplied reference sheet. All use Point filtering.
Original references remain in `Assets/_Trapple/Art/Hub/Source/`. No pickup, throw,
combat or dodge ability was added; the DODGE row remains reference artwork only.

## Preview data and integration boundaries

This is an **isolated local UI progression prototype**, not a production economy.
`HubConfiguration.asset` exposes starting seeds (80), prices (10/20/35), upgrade
counts, percentages (speed/jump 2%, agility 3%), world scene paths and cosmetics.
Purchases deduct seeds and persist levels. The UI displays the computed bonus;
**movement modifiers are not applied** until platforming balance is approved.

`PreviewSaveService` uses only `PlayerPrefs["Trapple.HubPreview.v1"]`.
It does not read or overwrite future production save data. Tutorial completion
grants its configured reward once and updates the meadow card to COMPLETED.
To reset this preview, delete that one PlayerPrefs key from an editor script.

`HubServices.cs` defines `IProgressionService`, `IUpgradeService`, `ISaveService`
and `ISceneFlowController`. Replace the preview implementations at the composition
root (`HubSceneFlowController.Ensure`) when production services are ready. UI
controllers receive these interfaces and do not save progress or change physics.
The scene flow keeps services across Hub → Tutorial → Hub and Tutorial deaths.
A return-to-Hub button is added only for a Tutorial trip launched from Hub.

The world map contains exactly four planned locations. Only the existing Tutorial
is launchable. Forest, Factory and Lava remain locked with unavailable level
buttons. The Ancient Tree test arena is not presented as a finished forest world.
Both world state and `Application.CanStreamedLevelBeLoaded` are checked before
loading. Register real levels and connect production unlocking before enabling
additional worlds. Cosmetic availability follows its world's unlock contract;
all four looks can be previewed, but only the base look is initially equippable.
Cosmetics have no effect on abilities.

Necessary integration changes to Developer 2's systems are small:

- `GameManager`: prefer the registered Hub for the main menu's Play action, and
  let Hub own Escape input.
- `GameInterface`: hide the older IMGUI HUD in Hub.
- `BossPrototypePlayMode`: select the separate Hub profile for Hub/main-menu
  sessions, restoring the previous profile when leaving Play Mode. Tutorial and
  boss-only sessions keep their previous profile.
- The menu regression test accepts the entry scene supplied by the active build.

No boss behavior, existing level scene or original player controller was changed.

## Rebuild and validation

The saved scene is ready to open; rebuilding is optional.
`Trapple > Hub > Rebuild Hub` regenerates Hub assets using Unity Editor APIs.
It replaces the generated scene, hub prefabs, support sprites and configuration
defaults. Keep deliberate scene/configuration edits before using this command.
Unity performs scene and prefab serialization; no scene YAML is handwritten.
`Trapple > Hub > Build Windows Preview` builds enabled global scenes into
`Builds/Hub/Trapple.exe`.

Run the EditMode `HubServiceTests` and PlayMode `HubTests`, plus the existing
`TutorialTests` and `BossSystemsTests`, in Unity Test Runner. From a closed
validation copy, Unity's `-runTests -testPlatform EditMode` and corresponding
`PlayMode` command-line options produce NUnit XML results.

A development player can run the visual smoke test with:

```powershell
& './Builds/Hub/Trapple.exe' -hub-capture 'C:/TrappleCaptures' -logFile 'C:/TrappleCaptures/player.log'
```

The explicit opt-in runner captures all five screens at the three supported
resolutions, checks window/card bounds, exercises Tutorial completion and return,
writes `result.txt`, restores the pre-existing preview save and exits. It is
inactive during normal play and in non-development builds. Automated interaction
uses the same interaction methods and UI Toolkit navigation-submit events as the
runtime UI; a human keyboard/hover feel check is still useful.

Current limitations: no audio content, no production economy/cloud save, no
movement modifier integration, no finished additional worlds and no dodge ability.

See `Docs/PixelUpdate.md` for the 2026-10-22 sprite/input update and verification.

See `Docs/HubArtSources.md` for artwork provenance and generation prompts.
