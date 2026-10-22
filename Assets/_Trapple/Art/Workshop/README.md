# Workshop artwork

The PNG files listed below were supplied by the user and are copied unchanged.

| File | Source attachment | Use |
| --- | --- | --- |
| Interior.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_50-1.png | Interior illustration |
| UpgradeFrame.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_51-2.png | Wooden frame and parchment decoration |
| SeedsSign.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_53-3.png | Currency sign; example number is covered by live UI |
| WorkshopSign.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_54-4.png | Animated hanging sign |
| Source/WorkshopDoor8Frames.png | Изображение ChatGPT 23 окт. 2026 г., 00_01_08.png | Eight authored building/door frames; top row 1–4, bottom row 5–8 |

Flat example text, character and prices inside UpgradeFrame are covered by live uGUI panels. Preview characters use existing APPi sprite frames. No purchased amount or demonstration progression is baked into the new interface.

WorkshopDoorAssets imports the supplied door sheet unchanged as eight equal Sprite subassets, excluding grid dividers and bottom padding. The building is part of each supplied frame, so WorkshopDoorAnimator animates the whole building SpriteRenderer and hides the old static illustration. Frame 1 stays visible when closed; opening plays 1–8, closing 8–1. No temporary dark rectangle remains. Warm entrance light is retained.

The source contains a translucent dark matte outside the building. WorkshopDoor.mat removes low-alpha matte at draw time using the Workshop Door sprite shader; the original PNG is not retouched. Alpha Floor and Solid Alpha are configurable on the material. Small differences in building detail and chimney smoke between supplied frames are preserved. Independent step tiles are still not supplied.

The non-classic existing cosmetics have static artwork. Optional CosmeticDefinition.previewAnimations supports future skin-specific frame sets. Until supplied, those cosmetics retain their actual sprite and tint with procedural motion, rather than inventing sprite frames.
