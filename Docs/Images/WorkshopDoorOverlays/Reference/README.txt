TRAPPLE — DOOR OPEN ANIMATION (Unity 2D)

SOURCE: uploaded 469x347 house picture.
CONTENT: Nine states: 00 closed, 01–07 opening in-between, 08 fully open.

house_empty_doorway.png          Base cottage with doorway in darkness.
doors_png/door_00..08.png         Nine independent transparent PNG overlays.
door_spritesheet_3x3.png         Transparent 3x3 sheet (240x360 pixels).
preview_frames/cottage_00..08   Frames for reviewing against the untouched house.
preview_spritesheet_3x3.png     Nine preview frames with the house.
animation_preview.gif            Loop, opens then closes in reverse.
original_door_cutout.png        Door extracted from the source (cropped).

UNITY IMPORT
- Import house_empty_doorway.png as Sprite (2D and UI), with full scene static.
- Import doors_png/*.png as multiple frames of the same sprite renderer.
- All sprite files are 80x120 RGBA. Their pivot and position must be identical.
- Sprite overlay location: image coordinates (91, 198) relative to upper-left
  of the original house canvas. In Unity (Y-up), center of the crop corresponds
  to local offset (-103.5, -84.5) pixels relative to the center of 469x347 scene.
- Use Pivot: Center for each 80x120 frame; no moving the GameObject between frames.
- Suggested timing: 9 to 12 FPS; close using reverse order.
- Texture Filter Mode: Point (no filter) for crisp pixel art.
- For a higher-res version, upscale 2x or 4x via nearest-neighbor.

TECHNICAL NOTE
This is a 2D width-compression swing simulation based on the EXISTING source
pixels, not nine hand-painted 3D-perspective views. Thus there is no wobble in
cottage frames, but the door's apparent shape is intentionally compressed.
