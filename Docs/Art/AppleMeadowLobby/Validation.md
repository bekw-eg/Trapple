# Lobby art validation

Validated on 2026-10-23. This package is a visual target and supporting art
handoff, not an implemented Unity scene.

## Visual review

- Compared the generated composition with the supplied Apple Meadow sheets and
  LevelReference: smooth cartoon silhouettes, leafy grass, warm rounded soil,
  honey timber, apple trees and layered blue-green scenery are consistent.
- Workshop is the dominant left-hand building; the wardrobe has a separate
  silhouette, and three readable portal arches sit to the right.
- The central ground stays level and clear. The conservative clear interval
  spans 605 reference pixels, approximately 31.2% of the width.
- The visible sun sits at approximately 91.6% width and 12.6% height, in the
  upper-right. Rays travel down-left; highlights remain warm and readable.
- The final scene contains no player, HUD, menu, text, labels or character stats.
- Distant buildings seen in the first draft were removed in the selected final.
- The scenery plate omits the workshop, wardrobe, portals and stone gameplay
  ground. It retains low edge foliage and combines its environment depths.

## File and layout checks

Pillow decoded and verified all three saved PNGs. Read-only checks confirmed:

| Check | Result |
| --- | --- |
| Concept dimensions / mode | 1942 × 809, RGB |
| Scenery plate dimensions / mode | 1942 × 809, RGB |
| Prop atlas dimensions / mode | 1536 × 1024, RGBA |
| Atlas alpha range | 0–254 |
| Completely transparent atlas pixels | 49.89% |
| Five atlas rectangles | In bounds, mutually disjoint, nonempty |
| Unity bottom-left rectangle conversion | Matches image top-left rectangles |
| Foot anchors | Inside their respective sprite rectangles |
| Maximum alpha on each rectangle boundary | 2, 1, 1, 2, 1 of 255 |
| Maximum alpha outside all five rectangles | 1 of 255 |
| Significant sprite pixels (alpha > 32) | All contained in the five rectangles |
| Layout JSON | Parsed successfully |

The lava rectangle was widened during validation to include its small right-hand
foliage tip. No image pixels were cropped, repainted or processed with Python;
Pillow was used only to inspect the files and validate coordinates.

`git diff --exit-code origin/main -- Assets Packages ProjectSettings Tools`
passed. Existing source art, Unity scenes, prefabs, code, project settings and
their metadata are unchanged.

## Image fingerprints

SHA-256:

```text
LobbyConcept.png
2d475ddeda74f1c673c6e1b610656f5255f61f24e46e6fbfe49d1276f81a2209

SceneryPlate.png
5ab9865e1d63688d5984f23356f880cbd4f2c8ce56d76076c9d27abe4332810f

InteractablesAtlas.png
d0ebbbf8a0f541ce3435810359f38b5b1b2afb7f8a2aa8cff148e6b159a319f0
```

## Checks not performed and limitations

- Unity is installed and open, but no scene or runtime asset was changed. Unity
  import, Play Mode, build and gameplay tests were not run for this concept-only
  handoff. Sprite scaling, camera framing and interaction spacing require a
  later in-engine review after integration.
- The environment is not an independently layered PSD or complete parallax set.
  Sky, mountains, hills and edge foliage remain combined in the scenery plate.
- Prop cutouts were generated from the concept; they are not pixel-identical
  extractions. Relative scales and foot pivots need fitting during integration.
- Portal interiors, stone frames and local glows are combined per portal. No
  animation, masks, emission maps or separate VFX textures are supplied.
- The atlas's near-opaque maximum alpha is 254; inspect imported edges over light
  and dark scenery before choosing final compression settings.
- The images are supplied at their native tool resolution. No higher-resolution
  repaint, full runtime lighting pass or final platform compression review was
  performed.
