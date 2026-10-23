# Apple Meadow

The seven source sheets were supplied by the user for the first world's artwork.
They are retained in `Sources/`. `Tools/prepare_meadow_art.py <source-folder>`
extracts the sprites and pause illustration into `Resources/AppleMeadow/`.
The extraction uses Pillow; no image-generation service is used.

The revised ground uses the user's `Sources/LevelReference.png` level image.
Run `python Tools/prepare_meadow_ground.py` after the sheet extraction to restore
that terrain. Its matching edges and fixed grass cap allow a single tiled
renderer per platform rather than repeated rounded floating islands.

`AppleMeadowArtImporter` configures the extracted PNGs as bilinear sprites.
`AppleMeadowPresentation` dresses the existing Tutorial when it loads, including
direct Editor Play, death/restart, and entry from the hub. The saved scene and
shared player/trap prefabs retain their gameplay setup. Decorative objects have
no colliders; the existing trap hitboxes and platform route remain unchanged.
The dressing appears in Play mode rather than the scene's edit-time preview.

Space jumps, A/D or arrow keys move, and Escape opens/closes pause. The supplied
pause illustration has working Continue, Settings (saved volume), Restart and
Main Menu buttons. Pause applies to the tutorial's existing GameManager.

The first world's IMGUI consumes Space before any menu button can submit it;
the Input Manager still delivers the press to the player's jump controller.
For a development build, `-tutorial-keyboard-check <output-folder>` checks real
Space/Escape events, a Space press while paused, Continue, jumping again, and
Escape to resume, and the actual Exit-to-menu mouse click. The check enters via
the hub and verifies that Exit loads `MainMenu`, restores time, and does not go
back to the hub. It also checks Escape before the first jump. It writes
`result.txt` and is inactive during ordinary play.

The sheets also show other hazards and objects. Only details useful to the
existing tutorial are integrated; no new trap mechanics or worlds are added.
