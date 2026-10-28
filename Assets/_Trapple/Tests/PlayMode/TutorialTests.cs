using System.Collections;
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
    // Exercise the saved scene and real 2D physics/coroutines; no stand-in Unity API.
    public sealed class TutorialTests
    {
        private PlayerController controller;
        private PlayerDeath death;
        private Rigidbody2D body;

        [UnitySetUp]
        public IEnumerator LoadTutorial()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Tutorial");
            controller = Object.FindAnyObjectByType<PlayerController>();
            death = controller.GetComponent<PlayerDeath>();
            body = controller.GetComponent<Rigidbody2D>();
            // Drive input fields explicitly to keep physics tests independent of the OS keyboard.
            controller.enabled = false;
            yield return new WaitForSeconds(0.25f);
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Tutorial");
        }

        [Test]
        public void SceneContainsRequiredComponents()
        {
            Assert.That(Object.FindObjectsByType<SpikeTrap>(FindObjectsInactive.Include).Length, Is.GreaterThan(3));
            Assert.That(Object.FindAnyObjectByType<FallingPlatform>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<HiddenTrap>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<TutorialGoal>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<TutorialCamera>(), Is.Not.Null);
            Assert.That(body.gravityScale, Is.GreaterThan(0f));
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints2D.FreezeRotation));
        }

        [Test]
        public void MeadowArtworkLoadsOnceAndKeepsTrapHitboxes()
        {
            Assert.That(Object.FindObjectsByType<AppleMeadowPresentation>().Length, Is.EqualTo(1));
            Assert.That(Resources.Load<Sprite>("AppleMeadow/Tree"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("AppleMeadow/Pause"), Is.Not.Null);
            var spike = GameObject.Find("Start Spikes").GetComponent<BoxCollider2D>();
            Assert.That(spike.bounds.size.x, Is.EqualTo(1.44f).Within(.01f));
            Assert.That(spike.bounds.size.y, Is.EqualTo(.595f).Within(.01f));
        }

        [UnityTest]
        public IEnumerator PauseFreezesPlayerAndClearsPendingJump()
        {
            var manager = Trapple.Core.GameManager.Instance;
            Set(controller, "jumpRequested", true);
            manager.Pause();
            Vector2 position = body.position;
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(controller.ControlEnabled, Is.False);
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(body.position, Is.EqualTo(position));
            manager.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(controller.ControlEnabled, Is.True);
            Tick();
            Assert.That(body.linearVelocity.y, Is.LessThanOrEqualTo(.1f));
        }

        [Test]
        public void SpawnIsGrounded()
        {
            Tick();
            Assert.That(controller.IsGrounded, Is.True);
        }

        [Test]
        public void MovementAcceleratesTurnsAndStops()
        {
            Set(controller, "moveInput", 1f);
            Tick();
            Assert.That(body.linearVelocity.x, Is.InRange(0.1f, 5.9f));
            for (int i = 0; i < 10; i++) Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(6f).Within(0.001f));
            Set(controller, "moveInput", -1f);
            Tick();
            Assert.That(controller.GetComponent<SpriteRenderer>().flipX, Is.True);
            Set(controller, "moveInput", 0f);
            for (int i = 0; i < 10; i++) Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator JumpReachesConfiguredHeightAndRejectsDoubleJump()
        {
            float start = body.position.y;
            Set(controller, "jumpRequested", true);
            Tick();
            float jumpVelocity = body.linearVelocity.y;
            Assert.That(jumpVelocity, Is.GreaterThan(10f));
            yield return new WaitForFixedUpdate();
            float airborneVelocity = body.linearVelocity.y;
            Set(controller, "jumpRequested", true);
            Tick();
            Assert.That(body.linearVelocity.y, Is.EqualTo(airborneVelocity).Within(0.001f));
            Assert.That(controller.IsGrounded, Is.False);

            float peak = body.position.y;
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, body.position.y);
            }
            Assert.That(peak - start, Is.InRange(2.15f, 2.5f));
        }

        [UnityTest]
        public IEnumerator AirbornePressIsNotReplayedOnLanding()
        {
            body.position = new Vector2(0f, 2f);
            Physics2D.SyncTransforms();
            Set(controller, "jumpRequested", true);
            Tick();
            Assert.That(body.linearVelocity.y, Is.EqualTo(0f).Within(0.01f));
            yield return new WaitForSeconds(0.6f);
            Tick();
            Assert.That(controller.IsGrounded, Is.True);
            Assert.That(body.linearVelocity.y, Is.LessThanOrEqualTo(0.1f));
        }

        [Test]
        public void NearbyWallCannotGrantJump()
        {
            body.position = new Vector2(2.35f, 0.7f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Tick();
            Assert.That(controller.IsGrounded, Is.False);
        }

        [Test]
        public void DeathIsIdempotentAndDisablesPhysicsAndControl()
        {
            Set(death, "restartDelay", 30f);
            death.Die();
            death.Die();
            Assert.That(death.IsDead, Is.True);
            Assert.That(controller.ControlEnabled, Is.False);
            Assert.That(body.simulated, Is.False);
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator DeathAllowsRespawnEvenWhenTimeIsPaused()
        {
            Set(death, "restartDelay", 0.08f);
            death.Die();
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.3f);
            var screen = Object.FindAnyObjectByType<Trapple.UI.DeathScreenUI>();
            Assert.That(screen.IsOpen, Is.True);
            Assert.That(death != null, Is.True, "Death waits for the Respawn action");
            screen.RespawnButton.onClick.Invoke();
            yield return null;
            yield return null;
            var respawned = Object.FindAnyObjectByType<PlayerDeath>();
            Assert.That(death == null, Is.True);
            Assert.That(respawned, Is.Not.Null);
            Assert.That(respawned.IsDead, Is.False);
        }

        [UnityTest]
        public IEnumerator SpikeTriggerKillsPlayer()
        {
            Set(death, "restartDelay", 30f);
            body.position = new Vector2(7.5f, 0.55f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(death.IsDead, Is.True);
        }

        [UnityTest]
        public IEnumerator UnarmedSpikesDoNotKill()
        {
            var spike = GameObject.Find("Start Spikes").GetComponent<SpikeTrap>();
            spike.SetArmed(false);
            body.position = new Vector2(7.5f, 0.55f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(death.IsDead, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformFallsOnlyAfterLandingAndDelay()
        {
            var platform = GameObject.Find("FallingPlatform").GetComponent<FallingPlatform>();
            Assert.That(platform.IsTriggered, Is.False);
            body.position = new Vector2(12f, 0.55f);
            yield return new WaitForSeconds(0.15f);
            Assert.That(platform.IsTriggered, Is.True);
            Assert.That(platform.HasFallen, Is.False);
            yield return new WaitForSeconds(0.5f);
            Assert.That(platform.HasFallen, Is.True);
            Assert.That(platform.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(platform.transform.position.y, Is.LessThan(-0.2f));
        }

        [UnityTest]
        public IEnumerator SideContactDoesNotActivatePlatform()
        {
            var platform = GameObject.Find("FallingPlatform").GetComponent<FallingPlatform>();
            body.gravityScale = 0f;
            body.position = new Vector2(9.8f, -0.2f);
            body.linearVelocity = Vector2.right;
            yield return new WaitForSeconds(0.15f);
            Assert.That(platform.IsTriggered, Is.False);
        }

        [UnityTest]
        public IEnumerator HiddenTrapRevealsAfterDelayAndKillsOverlappingPlayer()
        {
            var hidden = GameObject.Find("HiddenTrap").GetComponent<HiddenTrap>();
            var spikes = hidden.GetComponentInChildren<SpikeTrap>(true);
            Assert.That(spikes.gameObject.activeSelf, Is.False);
            Set(death, "restartDelay", 30f);
            body.position = new Vector2(21.7f, 0.55f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(hidden.IsTriggered, Is.True);
            Assert.That(spikes.gameObject.activeSelf, Is.False);
            yield return new WaitForSeconds(0.5f);
            Assert.That(spikes.gameObject.activeSelf, Is.True);
            Assert.That(spikes.IsArmed, Is.True);
            Assert.That(death.IsDead, Is.True);
        }

        [UnityTest]
        public IEnumerator AppleCompletesLevelAndStopsPlayer()
        {
            var goal = Object.FindAnyObjectByType<TutorialGoal>();
            body.position = goal.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(goal.IsComplete, Is.True);
            Assert.That(controller.ControlEnabled, Is.False);
            Assert.That(goal.GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(death.IsDead, Is.False);
        }

        [UnityTest]
        public IEnumerator FallingOutOfLevelKillsPlayer()
        {
            Set(death, "restartDelay", 30f);
            body.position = new Vector2(0f, -10f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(death.IsDead, Is.True);
        }

        [UnityTest]
        public IEnumerator CameraFollowsPlayer()
        {
            var camera = Object.FindAnyObjectByType<TutorialCamera>();
            body.position = new Vector2(100f, 3f);
            body.transform.position = new Vector3(100f, 3f, 0f);
            body.simulated = false;
            yield return new WaitForSeconds(0.7f);
            Assert.That(camera.transform.position.x, Is.EqualTo(102f).Within(0.3f));
            Assert.That(camera.transform.position.y, Is.EqualTo(3.4f).Within(0.01f));
            Assert.That(camera.transform.position.z, Is.EqualTo(-10f).Within(0.01f));
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator EntireTutorialIsReachableWithMovementAndJumps()
        {
            Set(death, "restartDelay", 30f);
            yield return WalkTo(2f);
            yield return HopTo(4.9f);
            yield return WalkTo(5.7f);
            yield return HopTo(9.1f);
            yield return WalkTo(9.3f);
            yield return HopTo(13.7f);
            yield return HopTo(16.7f);
            yield return WalkTo(18.7f);
            yield return HopTo(23.1f);
            yield return WalkTo(23.8f);
            yield return HopTo(27.3f);
            yield return WalkTo(27.7f);
            yield return HopTo(29.4f);
            yield return HopTo(32f);
            yield return HopTo(35.3f);
            yield return WalkTo(39.9f);
            yield return ExpandedRoutePlayback.Continue(controller, death, false);
            Assert.That(Object.FindAnyObjectByType<TutorialGoal>().IsComplete, Is.True);
            Assert.That(death.IsDead, Is.False);
        }

        private IEnumerator WalkTo(float target)
        {
            for (int i = 0; i < 200; i++)
            {
                if (!controller.ControlEnabled)
                    yield break;
                Set(controller, "moveInput", body.position.x < target - 0.15f ? 1f : 0f);
                Tick();
                yield return new WaitForFixedUpdate();
                Assert.That(death.IsDead, Is.False, "Died while walking to " + target);
                if (body.position.x >= target - 0.15f && Mathf.Abs(body.linearVelocity.x) < 0.01f)
                    yield break;
            }
            Assert.Fail("Could not walk to " + target + "; position=" + body.position);
        }

        private IEnumerator HopTo(float target)
        {
            Tick();
            Assert.That(controller.IsGrounded, Is.True, "No ground before jump to " + target);
            Set(controller, "jumpRequested", true);
            for (int i = 0; i < 100; i++)
            {
                Set(controller, "moveInput", body.position.x < target - 0.15f ? 1f : 0f);
                Tick();
                yield return new WaitForFixedUpdate();
                Assert.That(death.IsDead, Is.False, "Died while jumping to " + target);
                if (i > 3 && controller.IsGrounded && Mathf.Abs(body.linearVelocity.x) < 0.01f)
                    yield break;
            }
            Assert.Fail("Could not land at " + target + "; position=" + body.position);
        }

        private void Tick() => typeof(PlayerController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
