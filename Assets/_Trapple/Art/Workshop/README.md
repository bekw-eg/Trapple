# Workshop artwork

The four PNG files were supplied by the user for this task and are copied unchanged.

| File | Source attachment | Use |
| --- | --- | --- |
| Interior.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_50-1.png | Interior illustration |
| UpgradeFrame.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_51-2.png | Wooden frame and parchment decoration |
| SeedsSign.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_53-3.png | Currency sign; example number is covered by live UI |
| WorkshopSign.png | Изображение ChatGPT 22 окт. 2026 г., 23_02_54-4.png | Animated hanging sign |

Flat example text, character and prices inside UpgradeFrame are covered by live uGUI panels. Preview characters use existing APPi sprite frames. No purchased amount or demonstration progression is baked into the new interface.

The original hub building contains painted steps and a closed door. There are no independent step tiles or authored door-opening frames. WorkshopDoorAnimator supports an ordered openingFrames array; its current presentation uses a temporary flat opening and warm light over the existing building. It uses no 3D rotation. New door artwork is still needed to replace this temporary presentation.

The non-classic existing cosmetics have static artwork. Optional CosmeticDefinition.previewAnimations supports future skin-specific frame sets. Until supplied, those cosmetics retain their actual sprite and tint with procedural motion, rather than inventing sprite frames.
