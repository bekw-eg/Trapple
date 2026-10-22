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
        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath);
            yield return null; door=Object.FindAnyObjectByType<WorkshopDoorAnimator>();
        }
        [UnityTearDown]
        public IEnumerator Cleanup() { door.Set(0); Time.timeScale=1; yield return null; }
        private IEnumerator Observe(List<int> indices)
        {
            float until=Time.realtimeSinceStartup+5;
            while(door.IsAnimating && Time.realtimeSinceStartup<until)
            {
                if(indices.Count==0 || indices[indices.Count-1]!=door.FrameIndex) indices.Add(door.FrameIndex);
                yield return null;
            }
            Assert.That(door.IsAnimating,Is.False);
        }
        [UnityTest]
        public IEnumerator OpeningAndClosingShowEveryFrameInTheSuppliedOrder()
        {
            Assert.That(door.FrameCount,Is.EqualTo(8)); Assert.That(door.Presentation.enabled,Is.True);
            var seen=new List<int>(); door.StartCoroutine(door.Animate(true)); yield return Observe(seen);
            CollectionAssert.AreEqual(Enumerable.Range(0,8).ToArray(),seen);
            Assert.That(door.Presentation.sprite.name,Is.EqualTo("WorkshopDoor08"));
            seen.Clear(); door.StartCoroutine(door.Animate(false)); yield return Observe(seen);
            CollectionAssert.AreEqual(Enumerable.Range(0,8).Reverse().ToArray(),seen);
            Assert.That(door.Presentation.enabled,Is.True);
            Assert.That(door.Presentation.sprite.name,Is.EqualTo("WorkshopDoor01"));
            var station=Object.FindObjectsByType<HubInteractable>().Single(value=>value.Screen==HubScreen.Workshop);
            Assert.That(station.GetComponentInChildren<SpriteRenderer>().enabled,Is.False,"The old static building must not draw underneath.");
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
            Assert.That(door.FrameIndex,Is.Zero); Assert.That(door.Presentation.sprite.name,Is.EqualTo("WorkshopDoor01"));
            Assert.That(door.IsAnimating,Is.False);
        }
    }
}
