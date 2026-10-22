# Apple Meadow

The seven source sheets were supplied by the user for the first world's artwork.
They are retained in `Sources/`. `Tools/prepare_meadow_art.py <source-folder>`
extracts the sprites and pause illustration into `Resources/AppleMeadow/`.
The extraction uses Pillow; no image-generation service is used.

`AppleMeadowArtImporter` configures the extracted PNGs as bilinear sprites.
`AppleMeadowPresentation` dresses the existing Tutorial when it loads, including
direct Editor Play, death/restart, and entry from the hub. The saved scene and
shared player/trap prefabs retain their gameplay setup. Decorative objects have
no colliders; the existing trap hitboxes and platform route remain unchanged.
The dressing appears in Play mode rather than the scene's edit-time preview.

Space jumps, A/D or arrow keys move, and Escape opens/closes pause. The supplied
pause illustration has working Continue, Settings (saved volume), Restart and
Main Menu buttons. Pause applies to the tutorial's existing GameManager.

The sheets also show other hazards and objects. Only details useful to the
existing tutorial are integrated; no new trap mechanics or worlds are added.
