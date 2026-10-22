using System.Linq;
using NUnit.Framework;
using Trapple.Workshop;
using UnityEditor;
using UnityEngine;

namespace Trapple.Tests
{
    public sealed class WorkshopDoorAssetTests
    {
        private const string Source="Assets/_Trapple/Art/Workshop/Source/WorkshopDoor8Frames.png";
        [Test]
        public void SuppliedSheetHasEightEqualFramesInTopRowThenBottomRowOrder()
        {
            var frames=AssetDatabase.LoadAllAssetsAtPath(Source).OfType<Sprite>().OrderBy(sprite=>sprite.name).ToArray();
            Assert.That(frames.Length,Is.EqualTo(8));
            for(int i=0;i<8;i++)
            {
                Assert.That(frames[i].name,Is.EqualTo("WorkshopDoor"+(i+1).ToString("00")));
                Assert.That(frames[i].rect,Is.EqualTo(new Rect(4+i%4*496,413-i/4*384,488,376)));
                Assert.That(frames[i].pivot,Is.EqualTo(new Vector2(244,188)));
                Assert.That(frames[i].texture.filterMode,Is.EqualTo(FilterMode.Point));
            }
        }
        [Test]
        public void ReusablePrefabContainsAuthoredClosedFrameAndMatteMaterial()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Trapple/Prefabs/Hub/WorkshopSystem.prefab");
            var door=prefab.GetComponentInChildren<WorkshopDoorAnimator>();
            Assert.That(door.FrameCount,Is.EqualTo(8)); Assert.That(door.HasAuthoredFrames,Is.True);
            Assert.That(door.Presentation.enabled,Is.True);
            Assert.That(door.Presentation.sprite.name,Is.EqualTo("WorkshopDoor01"));
            Assert.That(door.Presentation.sharedMaterial.shader.name,Is.EqualTo("Trapple/Sprites/Workshop Door"));
        }
    }
}
