using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Trapple.Level;
using Trapple.Player;
using Trapple.Traps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class FactoryWorldTests
    {
        private const string ScenePath = "Assets/_Trapple/Scenes/Levels/AbandonedFactory.unity";

        [UnityTearDown]
        public IEnumerator ReturnToTutorial()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Trapple/Scenes/Levels/Tutorial.unity");
        }

        [UnityTest]
        public IEnumerator FactoryRouteHasSixTimedSectionsAndEveryTrapUsesItsPhysicalCollider()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;

            var course = Object.FindAnyObjectByType<LevelCourse>();
            Assert.That(course, Is.Not.Null);
            Assert.That(course.Sections.Length, Is.EqualTo(6));
            Assert.That(course.Sections.Sum(s => s.estimatedSeconds), Is.InRange(300f, 360f));
            Assert.That(course.Sections[0].startX, Is.EqualTo(0f));
            for (int i = 1; i < course.Sections.Length; i++)
                Assert.That(course.Sections[i].startX, Is.EqualTo(course.Sections[i - 1].endX));
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            var follow = camera.GetComponent<TutorialCamera>();
            Assert.That(follow, Is.Not.Null);
            Assert.That(typeof(TutorialCamera).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(follow), Is.EqualTo(player.transform));
            var parallax = Object.FindObjectsByType<FactoryParallaxLayer>();
            Assert.That(parallax.Length, Is.EqualTo(2));
            Assert.That(parallax.All(layer => layer.GetComponentsInChildren<SpriteRenderer>().Length == 3), Is.True);

            var saws = Object.FindObjectsByType<FactorySawTrap>();
            Assert.That(saws.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(saws.All(s => s.GetComponent<CircleCollider2D>().isTrigger), Is.True);
            var presses = Object.FindObjectsByType<FactoryPressTrap>();
            Assert.That(presses.Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(presses.All(p => p.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Kinematic &&
                p.GetComponent<BoxCollider2D>().enabled && p.GetComponentsInChildren<BoxCollider2D>().Any(c => c.isTrigger)), Is.True);
            var movers = Object.FindObjectsByType<FactoryMovingPlatform>();
            Assert.That(movers.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(movers.All(m => m.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Kinematic &&
                m.GetComponent<BoxCollider2D>().enabled && m.gameObject.layer == 6), Is.True);
            Assert.That(Object.FindObjectsByType<FactoryConveyor>().Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(Object.FindObjectsByType<FallingPlatform>().Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(Object.FindObjectsByType<FactoryHazard>().Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(Object.FindObjectsByType<FactoryCheckpoint>().Length, Is.EqualTo(7));
            Assert.That(Object.FindAnyObjectByType<FactoryGoal>(), Is.Not.Null);

            foreach (var transform in Object.FindObjectsByType<Transform>())
                Assert.That(transform.GetComponents<MonoBehaviour>().All(component => component != null), Is.True,
                    transform.name + " has a missing script");
        }

        [UnityTest]
        public IEnumerator PressCyclesConveyorsAndCheckpointRestoreMoveTheirBodies()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;

            var player = Object.FindAnyObjectByType<PlayerController>();
            var playerBody = player.GetComponent<Rigidbody2D>();
            var saw = Object.FindObjectsByType<FactorySawTrap>().OrderBy(s => s.transform.position.x).First();
            float sawAngle = saw.transform.eulerAngles.z;
            yield return null;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(sawAngle, saw.transform.eulerAngles.z)), Is.GreaterThan(.1f));
            var belt = Object.FindObjectsByType<FactoryConveyor>().OrderBy(c => c.transform.position.x).First();
            playerBody.position = (Vector2)belt.transform.position + Vector2.up * .55f;
            playerBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.That(Mathf.Abs(playerBody.linearVelocity.x), Is.GreaterThan(.1f), "The belt pushes the player along its travel direction.");

            var press = Object.FindObjectsByType<FactoryPressTrap>().OrderBy(p => p.transform.position.x).First();
            Vector2 raised = press.GetComponent<Rigidbody2D>().position;
            yield return new WaitForSeconds(4.6f);
            Assert.That(press.IsDangerous, Is.True);
            Assert.That(press.GetComponent<Rigidbody2D>().position.y, Is.Not.EqualTo(raised.y).Within(.02f));

            var moving = Object.FindObjectsByType<FactoryMovingPlatform>().First();
            Vector2 movingStart = moving.GetComponent<Rigidbody2D>().position;
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector2.Distance(movingStart, moving.GetComponent<Rigidbody2D>().position), Is.GreaterThan(.01f));

            var checkpoint = Object.FindObjectsByType<FactoryCheckpoint>().OrderBy(c => c.transform.position.x).First();
            float checkpointX = checkpoint.transform.position.x;
            checkpoint.SendMessage("OnTriggerEnter2D", player.GetComponent<CapsuleCollider2D>());
            playerBody.position = new Vector2(20f, 4f);
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            var respawned = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(respawned.transform.position.x, Is.EqualTo(checkpointX).Within(.1f));
        }
    }
}
