# Physical world map in the hub

The hub now contains one wooden map in place of the three world portals. The
existing scenery, lights, floor, workshop, wardrobe, Appi, camera and player
controller are unchanged. Both `Hub.unity` and the existing `TrappleUI.unity`
hub preview use the same reusable `WorldMapBoard.prefab`.

## Artwork

- Runtime sprite: `Assets/Art/WorldMap/WorldMapBoard.png`.
- Original supplied image: `Docs/Art/WorldMap/WorldMapBoardSource.png`.
- Both images are 778 x 568. Pixel comparison confirms **zero RGB differences**;
  only alpha was changed. The original illustration, frame, foliage, flowers,
  two lanterns and existing printed dotted path remain intact.
- Alpha: 89,708 fully transparent pixels, 337,008 opaque pixels and 15,188 edge
  pixels with intermediate alpha. The sprite was visually inspected against a
  light checkerboard and the actual hub background.
- Imported with Unity's `TextureImporter`: Sprite (2D and UI), Single, input
  alpha, full rectangular mesh, bilinear filtering, no mipmaps, no compression,
  100 pixels per unit. The custom pivot is on the feet of the supplied frame.

The initial built-in imagegen edit used this prompt:

> Remove only the exterior background; preserve the complete original wooden
> map, frame, foliage, flowers, both lanterns, colors and world positions;
> return transparent PNG; add no objects, labels or progress markers.

That draft changed foreground details and was rejected. A Runway cutout also
omitted foreground parts and was rejected. The final asset restores every RGB
pixel from the supplied source and applies only an exterior alpha mask, retaining
the dark interior lines and smoothing the outer edge. Neither generated draft
is used by the project. Nothing outside the supplied canvas was invented.

Source SHA-256:
`57662376678141A60682FD20CB902535271F0E47D48BBE3106D19B6F1C675B6F`

Final PNG SHA-256:
`F27DAD358A99E9C14021A7DDD98E70F0A10C3EAA67133D69A2CD813C67D51B70`

## Interaction and wiring

1. Appi approaches the board's `BoxCollider2D` trigger. Its `OverlapPoint` check
   supplies proximity to the existing `HubInteractionController`.
2. The existing E handler calls `WorldMapInteractable.BeginInteraction`, which
   disables movement through `PlayerController.SetControlEnabled(false)`.
   The board stays in the world; no screen opens at this point.
3. The existing interaction controller handles a left click. A camera ray is
   intersected with the board plane and tested against four invisible child
   `PolygonCollider2D` triggers. UI clicks are excluded. The zones have no
   renderer, highlighting, animation or solid collision.
4. The selected zone calls the existing `HubUIController.OpenWorld(worldId)`:
   `meadow` top left, `forest` top right, `factory` bottom left, `lava` bottom
   right. This opens the existing level selector for that world. The original
   `WorldMapPanelUI`, progression checks and `HubSceneFlowController` still
   control level availability and loading.
5. Escape exits physical map interaction immediately. Escape in the level
   selector follows its existing close transition. The control handoff also
   handles opening a different hub panel or disabling the board/controller.

Hotspot outlines are authored in original image pixel coordinates in
`WorldMapBuilder.BuildPrefab`; `P(x,y)` converts the original top-left image
origin to sprite space using the custom pivot. All polygons follow the world
illustrations, excluding the frame and empty parchment. They scale with the
board without depending on display resolution or camera position.

The map is 9.4 world units wide at `(6.7, 0.02)`, behind Appi's sorting layer.
It uses the existing camera without moving, zooming or replacing it.

## Editor integration

`Trapple > Hub > Install World Map` imports the supplied PNG, builds the prefab,
replaces old world interactables and removes portal labels/auras in both hub
scenes. It updates their existing interaction target arrays and deletes the
three retired portal prefabs. Unity itself creates and saves all prefab/scene
serialization and metadata; no scene or prefab YAML is hand generated.

The installer opens additional scenes additively and saves only the two target
hub scenes. It refuses to overwrite unsaved edits in a target hub. Other loaded
scenes are not saved or replaced. The existing hub and lobby builders now use
the board, so rebuilding cannot recreate portals. The obsolete
`ForestPortalSetup` editor command and portal pulse mode were removed; numeric
values of the other ambience modes were preserved for existing scene data.

## Verification

Unity Editor: **6000.6.0f1**.

- Seven new Play Mode tests passed: scene/prefab wiring, proximity, in-world
  interaction, all four camera-to-polygon hits, ignored frame/empty parchment,
  existing world selection and locked loading guards, unchanged save data and
  sprite, control restoration, tutorial launch/return and the alternate hub.
- Manual Play Mode check with actual E, mouse click, Escape and Space passed:
  E shows the click hint over the physical board; the meadow click opens its
  existing selector; Escape closes the selector or physical map mode; jumping
  is blocked during selection and works again after closing.
- Scene-object comparison against the task base found only two modified
  existing objects per hub: interaction target references and scene roots.
  Camera, player, workshop, wardrobe, ground and scenery object data match.
- PNG dimensions, unchanged RGB values, alpha channel and `git diff --check`
  were checked. The pre-existing uncommitted forest scene change is excluded.

Three existing shared hub regressions also passed: grounded spawn/camera follow,
workshop proximity and control restoration, and rapid reopen/disable handling.
The final run passed **10 tests, 0 failures**, recorded in
`Logs/world-map-play-tests.xml` (generated locally, not committed).

No standalone player build was run for this change. Factory and lava still
have no level scenes configured and remain subject to the existing access
rules. Manual playthroughs of every level are outside this map replacement.

## Files changed by this task

Added (including Unity-generated `.meta` files for every new Unity asset):

- `Assets/Art.meta`, `Assets/Art/WorldMap.meta`.
- `Assets/Art/WorldMap/WorldMapBoard.png`.
- `Assets/_Trapple/Scripts/Hub/WorldMapInteractable.cs`.
- `Assets/_Trapple/Editor/WorldMapBuilder.cs`.
- `Assets/_Trapple/Prefabs/Hub/WorldMapBoard.prefab`.
- `Assets/_Trapple/Tests/PlayMode/WorldMapTests.cs`.
- `Docs/Art/WorldMap/WorldMapBoardSource.png`.
- `Docs/WorldMap.md`.

Updated:

- `Assets/_Trapple/Scenes/Hub/Hub.unity`.
- `Assets/_Trapple/Scenes/Menu/TrappleUI.unity`.
- `Assets/_Trapple/Scripts/Hub/HubInteractable.cs`.
- `Assets/_Trapple/Scripts/Hub/HubInteractionController.cs`.
- `Assets/_Trapple/Scripts/Hub/HubAmbience.cs`.
- `Assets/_Trapple/Scripts/Hub/UI/HubUIController.cs`.
- `Assets/_Trapple/Scripts/UI/ControlHintView.cs`.
- `Assets/_Trapple/Scripts/UI/GameInterface.cs`.
- `Assets/_Trapple/Editor/HubBuilder.cs`.
- `Assets/_Trapple/Editor/AppleMeadowLobbyBuilder.cs`.

Deleted, with their `.meta` files:

- `Assets/_Trapple/Editor/ForestPortalSetup.cs`.
- `Assets/_Trapple/Prefabs/Hub/Portal0.prefab`.
- `Assets/_Trapple/Prefabs/Hub/Portal1.prefab`.
- `Assets/_Trapple/Prefabs/Hub/Portal2.prefab`.

Branch: `feature/interactive-world-map`, based on the current
`fix/contextual-ui-layout` hub. The PR targets `main` as required and also
contains the existing unmerged prerequisites from PRs #24–#27. This task does
not merge those PRs or change the progression/save implementation.
