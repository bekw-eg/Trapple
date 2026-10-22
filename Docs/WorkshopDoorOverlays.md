# Trapple — door opening animation

The workshop uses the supplied static 469×347 cottage and nine transparent 80×120 door overlays. Only the door SpriteRenderer changes frames. The supplied artwork is copied unchanged.

## Playback

- Opening: `door_00` → `door_08`.
- Closing: `door_08` → `door_00`.
- Default: 10 FPS, with a separate 0.1-second hold for every frame. A complete nine-frame sequence takes about 0.9 seconds plus frame scheduling overhead.
- The closed overlay stays visible between visits. Resetting, reversing or disabling playback invalidates the previous sequence.
- Playback uses unscaled time and works while `Time.timeScale` is zero.

The existing workshop entry/exit cutscene controls opening and closing. The reusable `WorkshopSystem.prefab` and both playable hub scenes are installed through Unity Editor APIs. The old station illustration is disabled to avoid drawing another cottage underneath.

## Import and alignment

Runtime artwork is in `Assets/_Trapple/Art/Workshop/Door/`:

| Asset | Import |
| --- | --- |
| `house_empty_doorway.png` | Single Sprite, 469×347, Center pivot |
| `doors_png/door_00.png` … `door_08.png` | Nine Single Sprites, 80×120, Center pivot `(40,60)` |
| `door_spritesheet_3x3.png` | Alternative Multiple import: nine 80×120 cells, top-left to bottom-right |

All use 100 pixels per unit, Full Rect meshes, Point filtering, Clamp wrap, no mipmaps, no compression and no NPOT resizing. Runtime uses the independent PNGs on one renderer. The sheet is available for manual reuse.

The overlay's top-left corner is `(91,198)` in the house image. Its center relative to the house center is `(-103.5,-84.5)` pixels in Unity's Y-up coordinates:

```text
X = 91 + 40 - 469 / 2 = -103.5
Y = 347 / 2 - (198 + 60) = -84.5
```

The door is a child of the static cottage at local position `(-1.035,-0.845,0)`, scale `(1,1,1)`. Both share the cottage's scale, including the existing hub presentation size. No frame changes the pivot, position, rotation or scale. The door draws one sorting order above its parent.

Use **Tools → Trapple → Build 2D Workshop** to rebuild the prefab and scenes. `Trapple.Editor.WorkshopDoorAssets.BuildAndCapture` rebuilds twice and renders all nine states at the original 469×347 resolution into `Builds/WorkshopDoorOverlays/Frames/`.

## Source references

`Docs/Images/WorkshopDoorOverlays/Reference/` preserves the supplied nine cottage previews, preview sheet, opening/closing GIF, original cropped door and README. These files are review references; they are not runtime animation frames.

Validation reports are in `Docs/Validation/WorkshopDoorOverlays-*`. The complete changed-file list is in `Docs/WorkshopDoorOverlays-ChangedFiles.txt`.

## Validation

Unity 6000.6.0f1:

- EditMode: **25/25 passed**, no skips. Covers independent imports, sheet cell order, pivots, fixed registration, prefab references and 10 FPS configuration.
- PlayMode: **51/51 passed**, no skips. Covers every opening/closing index, frame holds at zero timescale, reversal/reset, unchanged cottage sprite and transforms, and workshop entry/exit/re-entry. Existing hub, tutorial, player and boss tests also pass.
- Source verification: all **24** uploaded files are byte-identical. Each overlay matches its sheet cell and produces the supplied cottage preview at `(91,198)`.
- GPU verification: all nine states rendered through Unity with D3D11. Every pixel outside the 80×120 crop is identical across frames. The renders match the supplied previews within one RGB level at alpha-blended edges. Selected renders are `Unity-closed.png`, `Unity-middle.png` and `Unity-open.png` under `Docs/Images/WorkshopDoorOverlays/`.
- Windows x64 development build: **Succeeded**. Repeated rebuild validation confirms one workshop system, view, iris and EventSystem in each hub scene, with all required references assigned.
- Windows D3D11 player: **PASS** for entry, iris reveal, upgrade previews/purchases, exit, restored controls and physics, and repeated entry/exit. The actual hub render is `Docs/Images/WorkshopDoorOverlays/Unity-hub.png`.

## Limitations

The supplied base has an opaque dark background, which is preserved and appears as a rectangular backdrop in the hub. The opening frames simulate a swing by compressing the existing door pixels; they are not separately painted perspective views. The fully open frame retains the supplied narrow door edge. No upscaling or new artwork is generated.

Manual keyboard play in a visible window and platforms other than Windows were not tested. The player check uses the existing automated development-player harness and restores its test save/configuration changes.
