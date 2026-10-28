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
        public void RunClipsUseAllEightAlignedTransparentFramesAtTwelveFps()
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>("Assets/_Trapple/Settings/AppiAnimations.asset");
            Assert.That(set.idleClip, Is.Not.Null);
            var frames = new[] { set.walkRight, set.walkLeft };
            var clips = new[] { set.runRightClip, set.runLeftClip };
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                for (int direction = 0; direction < 2; direction++)
                {
                    Assert.That(frames[direction].Length, Is.EqualTo(8));
                    var clip = clips[direction];
                    Assert.That(clip.frameRate, Is.EqualTo(12f));
                    Assert.That(clip.length, Is.EqualTo(8f / 12f).Within(.0001f));
                    Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);
                    var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    Assert.That(bindings.Length, Is.EqualTo(1));
                    Assert.That(bindings[0].type, Is.EqualTo(typeof(SpriteRenderer)));
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                    Assert.That(keys.Length, Is.EqualTo(8));
                    for (int i = 0; i < 8; i++)
                    {
                        Assert.That(keys[i].value, Is.EqualTo(frames[direction][i % 8]));
                        Assert.That(keys[i].time, Is.EqualTo(i / 12f).Within(.0001f));
                    }
                    foreach (var sprite in frames[direction])
                    {
                        Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(144, 156)));
                        Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f));
                        Assert.That(sprite.pivot, Is.EqualTo(frames[0][0].pivot));
                        string path = AssetDatabase.GetAssetPath(sprite.texture);
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
                        Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                        Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True);
                        // Actual alpha, including every outer edge, rather than a black matte.
                        int x = (int)sprite.rect.x, y = (int)sprite.rect.y;
                        for (int edgeX = 0; edgeX < 144; edgeX++)
                        {
                            Assert.That(texture.GetPixel(x + edgeX, y).a, Is.Zero, sprite.name);
                            Assert.That(texture.GetPixel(x + edgeX, y + 155).a, Is.Zero, sprite.name);
                        }
                    }
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void AnimationFramesDoNotContainTheSourceSheetsBlackFloorBars()
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>("Assets/_Trapple/Settings/AppiAnimations.asset");
            Assert.That(set, Is.Not.Null);
            var frames = set.idle.Concat(set.walkRight).Concat(set.walkLeft).Concat(set.jump)
                .Concat(set.walkingRight).Concat(set.walkingLeft).ToArray();
            Assert.That(frames.Length, Is.EqualTo(36));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                foreach (var sprite in frames)
                {
                    Assert.That(sprite, Is.Not.Null);
                    Assert.That(texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture))), Is.True);
                    var pixels = texture.GetPixels32();
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
