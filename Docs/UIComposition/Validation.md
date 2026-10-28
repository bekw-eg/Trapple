# UI composition validation — 2026-10-27

Unity Editor: **6000.6.0f1**, Windows, DX12. Tests and screenshots below were
produced after the final hub prompt placement change.

| Check | Result |
| --- | --- |
| Unity script compilation and UI asset rebuild | Successful |
| `Trapple.Tests.StorybookUITests` | **8 passed, 0 failed**; see [PlayMode.xml](PlayMode.xml) |
| Automated Game View validation | **1420 passed, 0 failed**; see [Checks.txt](Checks.txt) and [Result.txt](Result.txt) |
| Actual Game View captures | **68 PNGs**, 17 contexts at each of four resolutions |
| Required resolutions | **1920×1080, 1600×900, 1366×768** |
| Additional resolution | **2560×1440** |
| Runtime errors during final capture | None recorded |
| Whitespace/error review | `git diff --check` passed for task files |
| User's existing forest scene edits | SHA-256 unchanged: `BD83F64A41BB24DEF64FF19A78AEE3B79C9C81E5ECB29F7825F8BB66719B6334` |

The automated run covered main menu, settings, hub, E interaction, world map,
tutorial movement/jump/sprint, pause, completion, wardrobe, workshop, forest
warning, pickup, throw, expired-warning gameplay and forest pause. Assertions
include text overflow/Thaleah, screen bounds, key count/aspect, separation from
the player and visible buttons, button hit targets, navigation, original stick
actions, pause/resume, saved volume and workshop input restoration.

The two added PlayMode tests specifically check tutorial context progression,
dismissal after demonstrating a jump/leaving a region, modal hiding, warning
fade-in/fade-out, expiry and frozen lifetime during pause. The existing six UI
tests also pass, including wardrobe equip and configured upgrade purchases.
The unavailable-world test now selects Factory because Forest was already
unlocked by the preceding portal fix.

Representative final screenshots were inspected visually at the three required
resolutions. They include all eight requested screens and all five control
categories. During iteration visual review caught and corrected the wardrobe
equip label wrapping and the hub prompt overlapping the upper navigation.

## Screenshots

- [Movement and separate warning, 1920×1080](1920x1080-HUD.png)
- [Hub interaction below navigation, 1920×1080](1920x1080-Interact.png)
- [Forest throw, 1920×1080](1920x1080-Throw.png)
- [Jump, 1600×900](1600x900-Jump.png)
- [Sprint, 1366×768](1366x768-Sprint.png)
- [Forest pickup, 1366×768](1366x768-Pickup.png)
- [Forest after warning expiry, 1366×768](1366x768-ForestIdle.png)
- [Main menu](1366x768-MainMenu.png), [settings](1366x768-Settings.png),
  [world map](1366x768-WorldMap.png), [pause](1366x768-Pause.png),
  [completion](1366x768-Complete.png), [wardrobe](1366x768-Wardrobe.png),
  [workshop](1366x768-Workshop.png) at 1366×768.

All 68 original screenshots remain in the ignored local directory
`Temp/StorybookEditorCaptures`; 14 representative PNGs are committed here.

## Limits and integration

- A Windows standalone build and a continuous human playthrough of the entire
  levels were **not run for this change**. The contextual capture runner moves
  actors to test locations in Play Mode; those movements are not scene edits.
- The user's pre-existing dirty forest scene was used for local forest captures
  but was not changed or committed. This report does not claim validation of a
  newly rebuilt forest scene.
- Tutorial hint regions are serialized in the current Tutorial scene. If its
  opening obstacles move, update these presentation regions in the Inspector.
- The existing Thaleah asset supplies Latin captions, matching the supplied art.
- The branch starts at `78e4864` and depends on the existing, unmerged work in
  PRs [#24](https://github.com/bekw-eg/Trapple/pull/24),
  [#25](https://github.com/bekw-eg/Trapple/pull/25) and
  [#26](https://github.com/bekw-eg/Trapple/pull/26). The requested target is `main`;
  the new task commit can be reviewed independently of those prerequisite commits.
  No pull request has been merged by this task.
