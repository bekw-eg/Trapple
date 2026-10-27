using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class PlayerSprintTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerController player;
        private Rigidbody2D body;
        private PlayerSpriteAnimator animation;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Tutorial");
            player = Object.FindAnyObjectByType<PlayerController>();
            player.enabled = false; // Inject input, but run the actual controller and Physics2D.
            player.GetComponent<PlayerDeath>().enabled = false;
            body = player.GetComponent<Rigidbody2D>();
            animation = player.GetComponent<PlayerSpriteAnimator>();
            // Test the saved character on a long, flat floor away from level traps.
            var floor = new GameObject("Sprint test floor") { layer = 6 };
            floor.transform.position = new Vector3(-1000f, -.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(200f, 1f);
            body.position = new Vector2(-1000f, .5f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.3f);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Tutorial");
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void ShiftSmoothlyAddsBoostAndReleaseReturnsToConfiguredWalkSpeed(float direction)
        {
            Set("moveSpeed", 5f);
            Set("sprintMultiplier", 1.8f);
            Set("moveInput", direction);
            for (int i = 0; i < 10; i++) Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(direction * 5f).Within(.001f));
            Set("sprintHeld", true);
            float before = body.linearVelocity.x;
            Tick();
            Assert.That((body.linearVelocity.x - before) * direction,
                Is.EqualTo(18f * Time.fixedDeltaTime).Within(.001f));
            for (int i = 0; i < 30; i++) Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(direction * 9f).Within(.001f));
            Set("sprintHeld", false);
            Tick();
            Assert.That(body.linearVelocity.x * direction,
                Is.EqualTo(9f - 24f * Time.fixedDeltaTime).Within(.001f));
            for (int i = 0; i < 30; i++) Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(direction * 5f).Within(.001f));
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void ReleasingMovementStopsQuicklyAndSprintReversalRemainsResponsive(float direction)
        {
            Set("moveInput", direction);
            Set("sprintHeld", true);
            for (int i = 0; i < 30; i++) Tick();
            Assert.That(body.linearVelocity.x * direction, Is.EqualTo(9.6f).Within(.001f));
            Set("moveInput", 0f);
            for (int i = 0; i < 8; i++) Tick();
            Assert.That(body.linearVelocity.x, Is.Zero);
            Assert.That(player.IsSprinting, Is.False);
            Set("moveInput", direction);
            for (int i = 0; i < 30; i++) Tick();
            Set("moveInput", -direction);
            for (int i = 0; i < 10; i++) Tick();
            Assert.That(body.linearVelocity.x * direction, Is.LessThan(0f));
            Assert.That(player.GetComponent<SpriteRenderer>().flipX, Is.EqualTo(direction > 0f));
        }

        [Test]
        public void HoldingShiftAtRestDoesNotMoveOrStoreABoost()
        {
            Set("sprintHeld", true);
            for (int i = 0; i < 100; i++) Tick();
            Assert.That(body.linearVelocity.x, Is.Zero);
            Assert.That(player.IsSprinting, Is.False);
            Set("moveInput", 1f);
            Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(60f * Time.fixedDeltaTime).Within(.001f));
        }

        [Test]
        public void SuspensionAndFocusLossClearSprintAndDoNotOverwriteExternalMotion()
        {
            Set("moveInput", 1f);
            Set("sprintHeld", true);
            player.SetControlEnabled(false);
            body.linearVelocity = new Vector2(18f, 3f);
            Tick();
            Assert.That(body.linearVelocity, Is.EqualTo(new Vector2(18f, 3f)));
            player.SetControlEnabled(true);
            Assert.That(player.IsSprinting, Is.False);
            Set("moveInput", 1f);
            Set("sprintHeld", true);
            typeof(PlayerController).GetMethod("OnApplicationFocus", Private).Invoke(player, new object[] { false });
            Assert.That(player.IsSprinting, Is.False);
            Assert.That(typeof(PlayerController).GetField("sprintHeld", Private).GetValue(player), Is.False);
        }

        [UnityTest]
        public IEnumerator WalkingUsesAllSixDistinctPosesInBothDirectionsAndSprintUsesRunCycle()
        {
            foreach (float direction in new[] { 1f, -1f })
            {
                Set("moveInput", direction);
                for (int i = 0; i < 15; i++)
                {
                    Tick();
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }
                var seen = new HashSet<Sprite>();
                for (int i = 0; i < 65; i++)
                {
                    Tick();
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    Assert.That(animation.State, Is.EqualTo(direction > 0 ? PlayerVisualState.WalkRight : PlayerVisualState.WalkLeft));
                    seen.Add(player.GetComponent<SpriteRenderer>().sprite);
                }
                Assert.That(seen.Count, Is.EqualTo(6));
                Assert.That(player.GetComponent<SpriteRenderer>().flipX, Is.False, "Left poses are authored, not double-flipped");
            }
            Set("sprintHeld", true);
            Tick();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.RunLeft));
            Assert.That(animation.Animations.walkLeft, Does.Contain(player.GetComponent<SpriteRenderer>().sprite));
        }

        [UnityTest]
        public IEnumerator WallContactStopsTheRunCycleAndMovingAwayRestartsIt()
        {
            var wall = new GameObject("Sprint test wall") { layer = 6 };
            wall.transform.position = new Vector3(body.position.x + 1f, 2f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.5f, 4f);
            Physics2D.SyncTransforms();
            Set("moveInput", 1f);
            Set("sprintHeld", true);
            for (int i = 0; i < 40; i++)
            {
                Tick();
                yield return new WaitForFixedUpdate();
                yield return null;
            }
            Assert.That(Mathf.Abs(body.linearVelocity.x), Is.LessThan(.15f));
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.Idle));
            Set("moveInput", -1f);
            for (int i = 0; i < 10; i++)
            {
                Tick();
                yield return new WaitForFixedUpdate();
                yield return null;
            }
            Assert.That(body.linearVelocity.x, Is.LessThan(-1f));
            Assert.That(animation.State, Is.EqualTo(PlayerVisualState.RunLeft));
        }

        [UnityTest]
        public IEnumerator SprintJumpPreservesHeightRejectsDoubleJumpAndRestoresGroundAnimation()
        {
            foreach (bool sprint in new[] { true, false })
            {
                Set("moveInput", 1f);
                Set("sprintHeld", sprint);
                float start = body.position.y;
                Set("jumpRequested", true);
                Tick();
                Assert.That(body.linearVelocity.y,
                    Is.EqualTo(Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * body.gravityScale * 2.4f)).Within(.001f));
                var states = new HashSet<PlayerVisualState>();
                float peak = start;
                for (int i = 0; i < 60; i++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    peak = Mathf.Max(peak, body.position.y);
                    states.Add(animation.State);
                    if (i == 5)
                    {
                        float verticalSpeed = body.linearVelocity.y;
                        Set("jumpRequested", true);
                        Tick();
                        Assert.That(body.linearVelocity.y, Is.EqualTo(verticalSpeed).Within(.001f));
                    }
                    Tick();
                }
                Assert.That(peak - start, Is.InRange(2.15f, 2.5f));
                Assert.That(states, Does.Contain(PlayerVisualState.Rising));
                Assert.That(states, Does.Contain(PlayerVisualState.Falling));
                Assert.That(states, Does.Contain(PlayerVisualState.Landing));
                Assert.That(animation.State, Is.EqualTo(sprint ? PlayerVisualState.RunRight : PlayerVisualState.WalkRight));
            }
        }

        private void Set(string field, object value) => typeof(PlayerController).GetField(field, Private).SetValue(player, value);
        private void Tick() => typeof(PlayerController).GetMethod("FixedUpdate", Private).Invoke(player, null);
    }
}
