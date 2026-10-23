using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Trapple.Hub;
using Trapple.Workshop;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class WorkshopDoorTests
    {
        private WorkshopDoorAnimator door;
        private SpriteRenderer cottage;
        private Sprite staticSprite;
        private Vector3 overlayPosition, overlayScale, cottagePosition, cottageScale;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath);
            yield return null; door=Object.FindAnyObjectByType<WorkshopDoorAnimator>();
            cottage=door.Presentation.transform.parent.GetComponent<SpriteRenderer>(); staticSprite=cottage.sprite;
            overlayPosition=door.Presentation.transform.localPosition; overlayScale=door.Presentation.transform.localScale;
            cottagePosition=cottage.transform.position; cottageScale=cottage.transform.localScale;
        }
        [UnityTearDown]
        public IEnumerator Cleanup() { door.Set(0); Time.timeScale=1; yield return null; }
        private IEnumerator Observe(List<int> indices)
        {
            float until=Time.realtimeSinceStartup+5;
            while(door.IsAnimating && Time.realtimeSinceStartup<until)
            {
                if(indices.Count==0 || indices[indices.Count-1]!=door.FrameIndex) indices.Add(door.FrameIndex);
                Assert.That(cottage.sprite,Is.SameAs(staticSprite));
                Assert.That(cottage.transform.position,Is.EqualTo(cottagePosition));
                Assert.That(cottage.transform.localScale,Is.EqualTo(cottageScale));
                Assert.That(door.Presentation.transform.localPosition,Is.EqualTo(overlayPosition));
                Assert.That(door.Presentation.transform.localScale,Is.EqualTo(overlayScale));
                yield return null;
            }
            Assert.That(door.IsAnimating,Is.False);
        }
        [UnityTest]
        public IEnumerator OpeningAndClosingShowEveryFrameInTheSuppliedOrder()
        {
            Assert.That(door.FrameCount,Is.EqualTo(9)); Assert.That(door.Presentation.enabled,Is.True);
            var seen=new List<int>(); door.StartCoroutine(door.Animate(true)); yield return Observe(seen);
            CollectionAssert.AreEqual(Enumerable.Range(0,9).ToArray(),seen);
            Assert.That(door.Presentation.sprite.name,Is.EqualTo("door_08"));
            seen.Clear(); door.StartCoroutine(door.Animate(false)); yield return Observe(seen);
            CollectionAssert.AreEqual(Enumerable.Range(0,9).Reverse().ToArray(),seen);
            Assert.That(door.Presentation.enabled,Is.True);
            Assert.That(door.Presentation.sprite.name,Is.EqualTo("door_00"));
            var station=Object.FindObjectsByType<HubInteractable>().Single(value=>value.Screen==HubScreen.Workshop);
            Assert.That(station.GetComponentInChildren<SpriteRenderer>().enabled,Is.False,"The old building must not draw underneath the supplied base.");
            Assert.That(cottage.enabled,Is.True);
        }
        [UnityTest]
        public IEnumerator ReversingOrResettingDuringOpeningDoesNotResumeTheStaleSequence()
        {
            Time.timeScale=0;
            door.StartCoroutine(door.Animate(true));
            yield return new WaitForSecondsRealtime(.3f);
            int index=door.FrameIndex; Assert.That(index,Is.InRange(1,6));
            var seen=new List<int>(); door.StartCoroutine(door.Animate(false)); yield return Observe(seen);
            CollectionAssert.AreEqual(Enumerable.Range(0,index+1).Reverse().ToArray(),seen);
            yield return new WaitForSecondsRealtime(.3f); Assert.That(door.FrameIndex,Is.Zero);
            door.StartCoroutine(door.Animate(true)); yield return new WaitForSecondsRealtime(.2f);
            door.Set(0); yield return new WaitForSecondsRealtime(.7f);
            Assert.That(door.FrameIndex,Is.Zero); Assert.That(door.Presentation.sprite.name,Is.EqualTo("door_00"));
            Assert.That(door.IsAnimating,Is.False);
        }
        [UnityTest]
        public IEnumerator PlaybackHoldsEachOverlayForTheConfiguredTenFpsAtZeroTimescale()
        {
            Time.timeScale=0;
            var times=new List<float>(); var indices=new List<int>();
            door.StartCoroutine(door.Animate(true));
            float until=Time.realtimeSinceStartup+5;
            while(door.IsAnimating && Time.realtimeSinceStartup<until)
            {
                if(indices.Count==0 || indices[indices.Count-1]!=door.FrameIndex)
                {
                    indices.Add(door.FrameIndex); times.Add(Time.realtimeSinceStartup);
                }
                yield return null;
            }
            Assert.That(door.IsAnimating,Is.False);
            CollectionAssert.AreEqual(Enumerable.Range(0,9).ToArray(),indices);
            for(int index=1;index<times.Count;index++)
                Assert.That(times[index]-times[index-1],Is.GreaterThanOrEqualTo(.095f),"A supplied overlay was held for less than 1/10 second.");
        }
    }
}
