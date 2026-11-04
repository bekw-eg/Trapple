# World 2: Abandoned Factory

`Assets/_Trapple/Scenes/Levels/AbandonedFactory.unity` is a 350-unit route split into six timed course sections (about 330 seconds by the course estimates). It reuses Appi, the existing `PlayerController`, `TutorialCamera`, `FallingPlatform`, scene completion flow, and the project's ground layer.

## Route

1. Factory entrance introduces the steel walkways and hazard language.
2. Conveyor belts and rotating saws teach timing and safe lanes.
3. Press hall combines cycling presses with narrower platforms.
4. Multi-hazard assembly line mixes belts, saws, presses, and breakable ledges.
5. Molten-channel crossing uses moving platforms and stepped ascents/descents.
6. Final industrial approach leads to a marked, boss-ready arena entrance.

Seven factory checkpoints restore the player after a fall. Presses, saws, conveyors, moving platforms, breakable platforms, and molten industrial zones have gameplay colliders and runtime behavior. Factory parallax, lamps, steam, and sparks provide atmosphere. The final gate is an end-of-level marker; a Robot boss encounter is not included in this level task.

## Controls

Move with **A/D** or **Left/Right Arrow**, jump with **Space**, and sprint with **Left Shift**. Checkpoint progress is local to the active scene and resets when the scene is freshly loaded.

## Art source

The industrial tilesheet and character/environment reference images provided for the task are kept in `Assets/_Trapple/Art/Factory/Source`; the level uses extracted sprites in `Assets/_Trapple/Art/Factory/Sprites`.
