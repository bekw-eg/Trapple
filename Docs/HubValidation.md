# Hub validation report

The current pixel-art/input update and refreshed screenshots are documented in
[PixelUpdate.md](PixelUpdate.md). This section records the original 2026-10-16 run.

Validated on **2026-10-16** with **Unity 6000.6.0f1** on Windows.
Base: `main` (`31da8cf`), which already includes the merged player/traps and boss
branches. Feature branch: `feature/hub-ui-screens`.

## Implemented

- Saved, playable orchard Hub scene and reusable player/station prefabs.
- Existing PlayerController, smooth camera, E interaction prompts, modal control
  locking, Escape closing, and animated scenery without a global time pause.
- Shared UI Toolkit HUD, workshop, world map, wardrobe and settings.
- Real local preview purchases/saves, maximum-level and insufficient-funds states.
- Four world cards with themed artwork and supplied character designs. Locked
  scenes cannot launch; registered Tutorial can launch and return to Hub.
- Tutorial completion/reward persistence and world completion state.
- Four wardrobe previews; world-linked skins remain locked until their world is
  available. Cosmetics do not change abilities.

## Checks performed

| Check | Result |
| --- | --- |
| C# compilation and Unity scene/prefab generation | Passed |
| Windows development player build | Passed |
| EditMode preview service tests | **10 / 10 passed** |
| Full PlayMode suite, including Tutorial and boss regressions | **34 / 34 passed** |
| Focused Hub PlayMode rerun after final UI polish | **7 / 7 passed** (part of the 34 unique tests) |
| Real Windows player visual/flow smoke test | Passed |
| 1920×1080, 1600×900, 1366×768 | All five screens captured and checked |
| Modal/card bounds, control restoration, four worlds | Passed |
| Purchases, MAX LEVEL, disabled purchase after spending | Passed |
| Tutorial launch, completion, reward, return | Passed |
| Runtime error/exception log scan during final capture | No errors or exceptions |

The final smoke run produced **18 screenshots**: five screens at each of three
resolutions, the upgraded workshop, Tutorial completion/return, and the completed
world card. Representative images:

![Hub](HubPreview/hub.png)
![Workshop](HubPreview/workshop.png)
![World selection](HubPreview/worlds.png)
![Wardrobe](HubPreview/wardrobe.png)

Full NUnit XML, screenshots and the player/build logs are available locally in
the ignored `Logs/HubValidation/` directory. The tested Windows build is available
locally at `Builds/Hub/Trapple.exe`; generated build/cache files are not committed.
The validation ran in a separate copy so the existing Unity Editor session did
not need to close.

## Not performed / limitations

- A human keyboard/mouse playthrough was not performed. Automated tests exercise
  the real interaction methods and UI Toolkit navigation-submit events; OS-level
  A/D, Space, E, Escape and hover feel still warrant a manual pass.
- No production economy, cloud save, or movement modifier integration. Upgrade
  levels/prices/save data are real within the isolated local preview service.
- Forest/Factory/Lava levels and their abilities are not implemented here. Their
  world cards and skins are previews; they stay locked.
- Supplied hero designs are transparent adaptations, not pixel-identical crops
  or a complete run/jump animation set. Original reference sheets are preserved.
- No audio content and no non-Windows platform/build testing.

Integration contracts and the small changes to existing menu/GameManager/UI
systems are described in [Hub.md](Hub.md). Artwork provenance and exact image
prompts are in [HubArtSources.md](HubArtSources.md).
The complete changed-file inventory is in [HubChangedFiles.md](HubChangedFiles.md).
