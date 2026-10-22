using System.IO;
using System.Linq;
using NUnit.Framework;
using Trapple.Player;
using UnityEditor;
using UnityEngine;

namespace Trapple.Tests
{
    public sealed class AppiSpriteTests
    {
        [Test]
        public void AnimationFramesDoNotContainTheSourceSheetsBlackFloorBars()
        {
            const string sourcePath = "Assets/_Trapple/Art/Hub/Source/AppiAnimation.png";
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>("Assets/_Trapple/Settings/AppiAnimations.asset");
            Assert.That(set, Is.Not.Null);
            var frames = set.idle.Concat(set.walkRight).Concat(set.walkLeft).Concat(set.jump).ToArray();
            Assert.That(frames.Length, Is.EqualTo(20));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(sourcePath)), Is.True);
                var pixels = texture.GetPixels32();
                foreach (var sprite in frames)
                {
                    Assert.That(sprite, Is.Not.Null);
                    var rect = new RectInt(Mathf.RoundToInt(sprite.rect.x), Mathf.RoundToInt(sprite.rect.y),
                        Mathf.RoundToInt(sprite.rect.width), Mathf.RoundToInt(sprite.rect.height));
                    // Floor strips span most of a frame's width; legitimate feet and outlines do not.
                    for (int edge = 0; edge < 2; edge++)
                    for (int offset = 0; offset < 4; offset++)
                    {
                        int y = edge == 0 ? rect.yMin + offset : rect.yMax - 1 - offset;
                        int run = 0, longest = 0;
                        for (int x = rect.xMin; x < rect.xMax; x++)
                        {
                            var pixel = pixels[y * texture.width + x];
                            run = pixel.a > 15 && pixel.r < 64 && pixel.g < 64 && pixel.b < 64 ? run + 1 : 0;
                            longest = Mathf.Max(longest, run);
                        }
                        Assert.That(longest, Is.LessThan(rect.width * .65f), sprite.name + " includes a black floor strip");
                    }
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }
    }
}
