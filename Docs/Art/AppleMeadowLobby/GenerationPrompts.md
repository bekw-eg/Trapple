# Lobby image-generation provenance

Mode: built-in `image_gen.imagegen`; no API or CLI fallback.
Reference files are retained unchanged in the repository.
The selected image was copied from the tool's generated-images folder into
`Docs/Art/AppleMeadowLobby/`.

## Initial composition

The initial image is an intermediate and is not included in the final package.

Use case: stylized-concept.
Asset type: polished art-direction target for a real side-view Unity 2D platformer lobby, one complete cohesive in-game environment illustration, wide 2:1 landscape canvas at the highest useful resolution.

Primary request: Create the Trapple Apple Meadow lobby using the provided project references as the foundation. A charming cozy fantasy orchard hub, warm magical golden afternoon, soft painterly cartoon rendering with confident clean rounded silhouettes. It should feel like the same game as the FIRST FOUR references, not a style redesign.

Input images and priority:
Image 1 Sheet1: primary exact material and shape language for the grassy ground, lime-gold grass cap and rounded ochre stone soil.
Image 2 Sheet3: primary foliage and apple-tree language, scalloped leaves, glossy small red apples, warm branching trunks, white daisies.
Image 3 Sheet6: primary landscape language, layered blue mountains, soft clouds, rolling green hills and dense leaf silhouettes.
Image 4 LevelReference: primary established gameplay camera/readability and unified illustrated cartoon rendering.
Image 5 PixelHubReference: ONLY architectural/design reference for workshop, cream-door wardrobe and pale stone magical arch. Translate these approved designs into the smooth non-pixel cartoon rendering of images 1-4. Do not import any pixels, world-card panels, typography or UI.

Composition: Strict orthographic side-view 2D platformer, one broad continuous horizontal grass-topped ground baseline at roughly 79% of image height. Shallow visible ground top, thick warm earth/rounded-stone cutaway under it, no vanishing-point plaza. Workshop is the main focal point on the left, occupying approximately x=7%-32%, with a complete distinctive sweeping deep green tiled roof, pale chimney, cream stucco and honey timber frame, glowing round window, dark-green arched closed door, open tools/workbench nook, hanging small brass gear-and-leaf emblem with no lettering. Tiny apple crate stays beside the wall. Keep the full building readable, no tree covering it.
Freestanding wardrobe near workshop at x=38%, approximately half workshop height: warm wooden cabinet with curved green cornice, cream double doors, brass knobs, small golden hanger pictogram, neat little green drawers. It has its own distinct readable silhouette with clear air on both sides.
CENTER x=44%-64% stays open and quiet, a broad unobstructed horizontal walkable lane with no props, no tall plants and no inset UI. All interactable bases share the ground baseline.
RIGHT x=68%-95%: exactly THREE large separate pale weathered stone magical destination arches on the same baseline, each with complete open readable silhouette and space between them. Same family of rounded chunky pale stones, small restrained moss and daisies, leaf-shaped small brass accents, subtly glowing green, teal and warm amber magical interiors; stylized hints of leafy woodland, distant industrial shapes and volcanic warm terrain may be visible only INSIDE their respective portals, never as extra buildings in the hub. Portals are tall and important, wardrobe much smaller than portal arches, workshop still strongest shape. No overlap between arches.
Use modest apple-tree framing at extreme left behind the workshop and at far right behind the portals; keep the upper-right sky open.

Background structure: clear pale blue warm sky, a few softly lit ivory-peach clouds, desaturated distant blue mountain chain, middle-distance sage green rounded orchard hills and sparse small tree silhouettes; foreground trees and low grass/white daisies border outer edges. Atmospheric separation lowers contrast/detail with distance. No distant new buildings. Preserve natural greens instead of applying a uniform orange filter.

Critical lighting: the SINGLE visible SUN is at x=91%, y=12%, clearly inside the UPPER-RIGHT corner. Gentle golden halo and just a few soft diagonal rays travel down-left from that sun. Warm highlights consistently hit the right-facing edges of roof, stone, wood and foliage; short soft shadows fall down-left. Bloom subtle, forms remain crisp and gameplay-readable. Portal glows are restrained local accents.

Layer-aware staging: clean independent silhouettes for workshop, wardrobe, each portal; background continues behind props; no vines or rays crossing between interactables; separate low decorative foliage at extreme front corners only; continuous straight ground top; distinguish sky/far mountains, rolling midground, near orchard framing, ground, props and front-edge decorative nature by depth and value. No central foreground occluders.

Quality: charming slightly handcrafted high-quality game art, soft painted volumes and restrained brush texture, round friendly shapes, coherent outline weight, matching material treatments across architecture and landscape. Seamless designed environment rather than a collage or sprite-sheet presentation.
Avoid: pixel art, photorealism, 3D render, gritty concept painting, anime, random buildings, statues, extra mechanics, bridges/gaps/traps, characters or player, animals, UI, menu panels, HUD, captions, text, words, letters, numbers, labels, watermark, multi-panel layout, borders. Output ONLY the finished unlabeled lobby illustration.

## Selected final composition

Edit input: the initial composition. Output: `LobbyConcept.png`.

Use case: precise-object-edit / stylized-concept.
Edit target: the provided Trapple lobby concept. Preserve its established Apple Meadow cartoon rendering, exact workshop design, freestanding wardrobe design, three stone destination portal designs, color harmony, soft painted textures, foliage language, golden lighting, and clean side-on gameplay camera.

Make ONLY this composition refinement: create a WIDER panoramic 12:5 landscape scene (approximately 2.4:1). Widen the open CENTRAL clearing between the wardrobe and first portal. The clear horizontal walkable space between them must occupy approximately one quarter of the full canvas width. Keep complete workshop on far left approximately x=4%-27%, clearly separate smaller wardrobe around x=33%-40%, completely open quiet walkable center x=42%-66%, and three separate important portal arches on right x=68%-98%. Leave a small clean grass gap between workshop and wardrobe. Keep all interactables on the SAME perfectly level grass baseline at about 73% image height, with no steps across the walkable route. Preserve the workshop as the dominant focal silhouette and preserve the readable height of the portals. The ground remains the reference's continuous leafy grass cap over rounded ochre stones, no paths angling toward the camera.
Remove the tiny distant buildings/hamlet on the hills so the only building anywhere in the lobby is the approved workshop. Retain the pale blue sky, warm clouds, mountain ranges, middle-distance rolling orchard hills, distant water and soft trees. Keep foreground foliage low along bottom and outer corners and away from the walkable line. Refine value separation: background slightly softer and less detailed than interactables, clear readable central clearing.
Critical invariants: SUN stays clearly visible in the UPPER-RIGHT corner at x=92%, y=12%; warm subtle light rays shine down-left from the right. Workshop and stone right-facing edges lit from the right. Three portals in green, blue-teal and amber-orange with destination hints only INSIDE them. No players, characters, animals, text, labels, UI, borders, extra buildings, extra objects or extra mechanics. No pixel art. No multi-panel presentation or layer diagram. Output ONLY the final beautiful cohesive in-game scene.

## Clean scenery plate

Edit input: `LobbyConcept.png`. Output: `SceneryPlate.png`.

Use case: precise-object-edit / background-extraction.
Edit target: the attached final Trapple Apple Meadow lobby concept. Produce its CLEAN SCENERY BACKDROP PLATE for Unity, SAME very wide 12:5 composition and exact matching cozy smooth cartoon painterly style.
Remove the entire workshop, chimney and its smoke, all workshop signs/props, the entire wardrobe, and all three magical portals including their glow and stones. Also remove the complete near grass-topped stone cutaway ground strip and all dark decorative foliage along the bottom/front corners. Reconstruct the natural landscape behind all removed items so a coherent uninterrupted backdrop remains, with no silhouette-shaped holes or shadows from removed objects.
Keep the original sky, soft warm clouds, far blue mountain chain, orchard hills and trees, distant lake, left and right framing apple trees. Extend the orchard landscape naturally to the bottom edge so foreground ground and props can later cover it. Don't replace removed foreground ground with a blank solid band. Background landscape stays low-contrast, with soft depth and clear quiet center.
Preserve the SINGLE visible SUN at the UPPER-RIGHT at x=92%, y=12% with subtle down-left golden rays, same sunny blue palette and right-side warm lighting.
No foreground gameplay platforms, earth cutaway, buildings, wardrobes, portals, characters, animals, UI, labels, text or extra objects. This is a clean backdrop only, not a new scene or multi-panel image. Opaque background, full 12:5 canvas.

## Prop cutouts

Edit input: `LobbyConcept.png`. Output: `InteractablesAtlas.png`.
The requested equal grid was not followed exactly; use the measured irregular
rectangles in `LayerLayout.json`, not the grid described in this generation prompt.
The output contains a real alpha channel; hidden RGB colors in transparent
pixels are not an opaque background.

Use case: background-extraction / precise-object-edit.
Edit target: attached final Trapple Apple Meadow lobby concept. Create a production-oriented TRANSPARENT PROP ATLAS extracting its FIVE interactable props only, matching their designs, materials, silhouette shapes and smooth non-pixel cartoon rendering as faithfully as possible. No redesigns.
Canvas: landscape 3:2, precisely THREE equal columns and TWO equal rows of square cells, on genuinely transparent background. Each complete object fully inside its own cell with at least 10% transparent padding. Keep all props crisp and legible. Arrange:
TOP LEFT cell: complete workshop building, the sweeping green tiled roof and pale chimney with a small wisp of smoke, cream stucco and warm timber structure, glowing round window, dark green closed door, tools nook, apple crate and its hanging brass gear-and-leaf sign. Exclude the large apple tree behind it, background shrubs, continuous floor and neighboring wardrobe. Retain only foliage physically attached to the workshop and its small doorstep.
TOP MIDDLE cell: complete separate freestanding wardrobe, curved green cornice, cream double doors with small golden hanger pictogram, warm wood frame and green drawers, no surrounding shrubs or ground.
TOP RIGHT cell: complete green magical stone arch from the first portal, with its contained forest destination interior, moss, small daisies and brass leaf accents, no surrounding landscape.
BOTTOM LEFT cell: complete blue-teal magical stone arch from the second portal, with its contained distant industrial destination interior, same frame materials.
BOTTOM MIDDLE cell: complete amber-orange magical stone arch from the third portal, with its contained volcanic destination interior, same frame materials.
BOTTOM RIGHT cell: completely EMPTY transparent.
Remove all scenery, sky, trees, ground strips, foreground bushes and background effects outside the five objects. Preserve restrained local glow within portal cutout silhouettes. Preserve top/right golden object highlights. No new text, lettering, labels, grid lines, cell borders, characters, dark background or checkerboard pattern. Genuine alpha transparency. This is an asset extraction, not five environment thumbnails.
