using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class ExpandedLevelTests
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTearDown]
        public IEnumerator ResetScene()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(GameManager.TutorialScene);
        }

        [UnityTest]
        public IEnumerator BothCoursesHaveSevenSectionsAndPreserveWorldAbilities()
        {
            foreach (string scene in new[] { GameManager.TutorialScene, ForestGoal.ScenePath })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return null;
                var course = Object.FindAnyObjectByType<LevelCourse>();
                Assert.That(course.Sections.Length, Is.EqualTo(7));
                Assert.That(course.Sections.Sum(s => s.estimatedSeconds), Is.InRange(300, 360), "Design estimate, NOT measured player time");
                for (int i = 1; i < 7; i++) Assert.That(course.Sections[i].startX, Is.EqualTo(course.Sections[i - 1].endX));
                Assert.That(Object.FindObjectsByType<PlayerController>().Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<ForestStick>().Length,
                    scene == GameManager.TutorialScene ? Is.Zero : Is.EqualTo(3));
                foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>())
                    Assert.That(renderer.sprite, Is.Not.Null, renderer.name);
                foreach (var component in Object.FindObjectsByType<Transform>())
                    Assert.That(component.GetComponents<MonoBehaviour>().All(c => c != null), Is.True, component.name + " missing script");
            }
        }

        [UnityTest]
        public IEnumerator CameraAndParallaxCoverEverySectionAtCommonAspectRatios()
        {
            foreach (string scene in new[] { GameManager.TutorialScene, ForestGoal.ScenePath })
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return null;
                var course = Object.FindAnyObjectByType<LevelCourse>();
                var player = course.Player; player.GetComponent<PlayerController>().enabled = false;
                player.GetComponent<Rigidbody2D>().simulated = false;
                var camera = Camera.main;
                foreach (float aspect in new[] { 4f / 3, 16f / 9, 21f / 9, 9f / 16 })
                {
                    camera.aspect = aspect;
                    foreach (float x in course.Sections.Select(s => (s.startX + s.endX) / 2).Concat(new[] { -20f, course.WorldBounds.y + 20 }))
                    {
                        player.transform.position = new Vector3(x, -6, 0);
                        yield return new WaitForSeconds(.3f);
                        float left = camera.transform.position.x - camera.orthographicSize * aspect;
                        float right = camera.transform.position.x + camera.orthographicSize * aspect;
                        Assert.That(left, Is.GreaterThanOrEqualTo(course.WorldBounds.x - .02f));
                        Assert.That(right, Is.LessThanOrEqualTo(course.WorldBounds.y + .02f));
                        Assert.That(camera.transform.position.y, Is.EqualTo(3.4f).Within(.01));
                        var panels = Object.FindAnyObjectByType<RepeatingLevelBackdrop>().GetComponentsInChildren<SpriteRenderer>()
                            .OrderBy(p => p.bounds.min.x).ToArray();
                        Assert.That(panels[0].bounds.min.x, Is.LessThan(left));
                        Assert.That(panels[2].bounds.max.x, Is.GreaterThan(right));
                        for (int i = 1; i < 3; i++) Assert.That(panels[i].bounds.min.x, Is.LessThanOrEqualTo(panels[i - 1].bounds.max.x));
                        Assert.That(panels[1].bounds.min.y, Is.LessThan(camera.transform.position.y - camera.orthographicSize));
                        Assert.That(panels[1].bounds.max.y, Is.GreaterThan(camera.transform.position.y + camera.orthographicSize));
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator MultipleStickStationsKeepSingleItemCapacity()
        {
            yield return SceneManager.LoadSceneAsync(ForestGoal.ScenePath);
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerController>(); player.enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            var sticks = Object.FindObjectsByType<ForestStick>().OrderBy(s => s.transform.position.x).ToArray();
            body.position = sticks[0].transform.position; yield return new WaitForFixedUpdate();
            Assert.That(sticks[0].TryPickup(), Is.True);
            body.position = sticks[1].transform.position; yield return new WaitForFixedUpdate();
            Assert.That(sticks[1].TryPickup(), Is.False);
            Assert.That(sticks.Count(s => s.IsHeld), Is.EqualTo(1));
            Assert.That(sticks[0].TryThrow(), Is.True);
            Assert.That(sticks[1].TryPickup(), Is.True);
        }

        [UnityTest]
        public IEnumerator TimerExcludesPreStartPauseAndPreviousDeathAndStopsAtGoal()
        {
            yield return SceneManager.LoadSceneAsync(GameManager.TutorialScene);
            var player = Object.FindAnyObjectByType<PlayerController>(); player.enabled = false;
            var timer = Object.FindAnyObjectByType<LevelRunTimer>();
            yield return new WaitForSeconds(.25f);
            Assert.That(timer.ActiveSeconds, Is.Zero);
            typeof(PlayerController).GetField("moveInput", Private).SetValue(player, 1f);
            for (int i = 0; i < 8; i++)
            {
                typeof(PlayerController).GetMethod("FixedUpdate", Private).Invoke(player, null);
                yield return new WaitForFixedUpdate();
            }
            yield return null;
            Assert.That(timer.ActiveSeconds, Is.GreaterThan(0));
            GameManager.Instance.Pause(); float paused = timer.ActiveSeconds;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(timer.ActiveSeconds, Is.EqualTo(paused));
            GameManager.Instance.Resume();
            player.GetComponent<PlayerDeath>().Die(); yield return null;
            Assert.That(timer.IsFinished, Is.True); Assert.That(timer.IsSuccessful, Is.False);
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(timer == null, Is.True);
            timer = Object.FindAnyObjectByType<LevelRunTimer>();
            Assert.That(timer.ActiveSeconds, Is.Zero); Assert.That(timer.IsFinished, Is.False);
            player = Object.FindAnyObjectByType<PlayerController>(); player.enabled = false;
            player.GetComponent<Rigidbody2D>().position = Object.FindAnyObjectByType<TutorialGoal>().transform.position;
            yield return new WaitForSeconds(.15f);
            Assert.That(timer.IsSuccessful, Is.True);
            float completed = timer.ActiveSeconds;
            yield return new WaitForSeconds(.15f);
            Assert.That(timer.ActiveSeconds, Is.EqualTo(completed));
        }
    }
}
