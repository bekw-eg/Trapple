# Contextual UI composition

Branch: `fix/contextual-ui-layout`.

## Cause

The screenshot came from the shared `Resources/Storybook/GameInterface.prefab`,
authored by `StorybookUIBuilder`. Its Canvas was already **Screen Space Overlay**,
with **Scale With Screen Size**, a **1920 × 1080** reference and 0.5 width/height
matching. It was not a world-space Canvas or an unsliced sprite sheet.

The builder put a 960 × 135 decorative card at a fixed top-left offset of
(480, 895), using a top-left pivot and anchors. Seven keys with different native
aspect ratios were squeezed into identical 110 × 60 boxes. `GameInterface`
showed the entire card for five seconds in every playing scene, combining
onboarding, pause information and `WATCH YOUR STEP`. The separate E/F prompt
also used a fixed lower-screen position. Correct Canvas scaling could not fix
that composition or determine which information was relevant.

## Changes

- Removed the old card, its seven simultaneous keys and the separate lower
  interaction panel from the existing shared prefab.
- One content-sized control prompt uses upper-center anchors/pivot, a compact
  dark backing, 16 px internal padding, native-aspect wooden key sprites at
  48 px high and a measured Thaleah TMP caption at font size 50. It is 80 px high and
  stays below 500 px wide for existing captions. Drawn key text is not repeated.
- In levels the control prompt starts 32 reference pixels below the screen top.
  In the hub it starts at 144 px, below the navigation buttons. The pause button
  anchors to the upper-right corner; hub shortcuts and boss text anchor centrally.
- A scene-owned, presentation-only `TutorialControlHints` component teaches
  movement at spawn, jumping on approach to the existing first step, and sprinting
  on the safe x=14..19 runway after the bridge. A demonstrated action, leaving
  its area or six seconds of active teaching dismisses a hint. There are no new
  gameplay triggers, colliders, gates, completion conditions or save keys.
- Hub proximity, forest pickup/throw and the boss lever retain their original
  controllers and conditions. Their prompts have priority over onboarding.
  The existing forest one-time flags and three-second lifetime remain intact.
- `WATCH YOUR STEP` has its own centered 400 × 68 notice. It retains the level
  entry event and five-second lifetime, with 0.25 s fade-in and 0.5 s fade-out.
  It is hidden in modals and does not age while paused. It is not displayed in
  the hub or menu.
- Wardrobe slots, names, states and the equip button now clear the window's
  lower decorations. Dynamic button text has larger side padding. Workshop
  progress pips occupy their own header space rather than the decorated corner.
- Menu, map, settings, pause, completion and workshop actions still use their
  existing handlers. UI source and prefab replacement avoid adding a second UI.

The tutorial scene and all three changed prefabs were serialized by Unity Editor
APIs. The pre-existing dirty forest scene is excluded from this change and is
preserved byte for byte in the user's checkout.

## Reproduce

1. In Unity 6000.6.0f1 use `Tools > Trapple > Fix UI composition` to regenerate
   only these UI assets and the tutorial presentation component. Save any open
   scene first. The command restores the previous scene setup.
2. Run `Trapple.Tests.StorybookUITests` in the PlayMode Test Runner.
3. For opt-in automated visual verification, write `capture` to
   `Temp/storybook-build-request.txt` with the Editor idle and scripts compiled.
   `StorybookCaptureRunner` records actual Game View frames, verifies bounds,
   key counts, text fitting, button hits, player clearance, contextual activation,
   scene navigation and original actions. It temporarily moves test actors to
   the relevant contexts; it does not save those runtime movements to scenes.
4. Generated full screenshots and the complete assertion log are under
   `Temp/StorybookEditorCaptures`. Representative screenshots and results are
   retained in `Docs/UIComposition/`.

Validation results are recorded in `Docs/UIComposition/Validation.md`.
