using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using Trapple.Traps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Trapple.Tests
{
    public sealed class DeceptiveForestTests
    {
        private PlayerController player;
        private PlayerDeath death;
        private Rigidbody2D body;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator Load()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ForestGoal.ScenePath);
            player = Object.FindAnyObjectByType<PlayerController>(); death = player.GetComponent<PlayerDeath>();
            body = player.GetComponent<Rigidbody2D>(); player.enabled = false;
            yield return new WaitForSeconds(.25f);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(GameManager.TutorialScene);
        }
        [Test]
        public void SavedSceneHasPlayerArtPrefabsAndSafeSpawn()
        {
            Tick(); Assert.That(player.IsGrounded, Is.True); Assert.That(death.IsDead, Is.False);
            Assert.That(Object.FindObjectsByType<PlayerController>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<FallingPlatform>().Length, Is.Zero);
            Assert.That(Object.FindObjectsByType<ForestDisappearingPlatform>().Length, Is.EqualTo(6));
            Assert.That(Object.FindObjectsByType<ForestFallingTree>().Length, Is.EqualTo(3));
            Assert.That(Object.FindAnyObjectByType<ForestGoal>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ForestCamera>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<TutorialGoal>(), Is.Null);
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>()) Assert.That(renderer.sprite, Is.Not.Null, renderer.name);
            Assert.That(body.gravityScale, Is.EqualTo(3));
            Assert.That(player.GetComponent<CapsuleCollider2D>().size, Is.EqualTo(new Vector2(.75f, .95f)));
        }
        [UnityTest]
        public IEnumerator HiddenGroundOnlyActivatesOnTopAndRevealsRealPit()
        {
            var platform = GameObject.Find("01 · Hidden ground").GetComponent<ForestDisappearingPlatform>();
            var pit = platform.transform.Find("Concealed pit").gameObject;
            Assert.That(pit.activeSelf, Is.False);
            body.gravityScale = 0; body.position = new Vector2(8.6f, -.22f); body.linearVelocity = Vector2.right;
            yield return new WaitForSeconds(.15f); Assert.That(platform.IsTriggered, Is.False);
            body.gravityScale = 3; body.position = new Vector2(10.2f, .49f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(.08f);
            Assert.That(platform.IsTriggered, Is.True); Assert.That(platform.HasVanished, Is.False);
            Assert.That(platform.GetComponent<Collider2D>().enabled, Is.True); Assert.That(pit.activeSelf, Is.False);
            body.position = new Vector2(13, .6f);
            yield return new WaitForSeconds(.3f);
            Assert.That(platform.HasVanished, Is.True);
            Assert.That(platform.GetComponent<Collider2D>().enabled, Is.False);
            Assert.That(platform.transform.Find("Surface").gameObject.activeSelf, Is.False);
            Assert.That(pit.activeSelf, Is.True);
        }
        [UnityTest]
        public IEnumerator TrustedBranchMatchesSafeBranchesAndRemovesAllDecorWithSupport()
        {
            var platform = GameObject.Find("02 · Trusted branch C").GetComponent<ForestDisappearingPlatform>();
            var safe = GameObject.Find("02 · Trusted branch B").GetComponentInChildren<SpriteRenderer>();
            var art = platform.GetComponentInChildren<SpriteRenderer>();
            Assert.That(art.sprite, Is.EqualTo(safe.sprite)); Assert.That(art.color, Is.EqualTo(safe.color));
            Assert.That(art.transform.lossyScale, Is.EqualTo(safe.transform.lossyScale));
            body.position = new Vector2(25.5f, 2.89f); body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(.08f); Assert.That(platform.IsTriggered, Is.True);
            Assert.That(platform.GetComponent<Collider2D>().enabled, Is.True);
            body.position = new Vector2(29f, 1.8f);
            yield return new WaitForSeconds(.35f);
            Assert.That(platform.HasVanished, Is.True);
            Assert.That(platform.GetComponent<Collider2D>().enabled, Is.False);
            Assert.That(art.gameObject.activeInHierarchy, Is.False);
            platform.Trigger(death); yield return new WaitForSeconds(.4f);
            Assert.That(platform.State, Is.EqualTo(ForestDisappearingPlatform.Phase.Resolved));
        }
        [UnityTest]
        public IEnumerator BaitProximityTriggersWithoutLandingAndIsNotGoal()
        {
            Set(death, "restartDelay", 10f);
            var cover = GameObject.Find("03 · Apple clearing").GetComponent<ForestDisappearingPlatform>();
            body.gravityScale = 0; body.position = new Vector2(30.7f, .65f);
            yield return new WaitForSeconds(.1f);
            Assert.That(cover.IsTriggered, Is.True);
            Assert.That(Object.FindAnyObjectByType<ForestGoal>().IsComplete, Is.False);
            body.gravityScale = 3; body.position = new Vector2(31.6f, .6f);
            yield return new WaitForSeconds(1f);
            Assert.That(death.IsDead, Is.True);
            Assert.That(Object.FindAnyObjectByType<ForestUI>().IsModalOpen, Is.True);
        }
        [UnityTest]
        public IEnumerator DeathRestartsAndResetsEveryTrapItemsAndTime()
        {
            foreach (var trap in Object.FindObjectsByType<ForestDisappearingPlatform>()) trap.Trigger(death);
            foreach (var tree in Object.FindObjectsByType<ForestFallingTree>()) tree.Trigger(death);
            yield return new WaitForSeconds(1.3f);
            death.Die(); death.Die(); GameManager.Instance.Pause();
            yield return new WaitForSecondsRealtime(.9f);
            Assert.That(death == null, Is.True);
            var fresh = Object.FindAnyObjectByType<PlayerDeath>(); Assert.That(fresh.IsDead, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<ForestStick>().IsHeld, Is.False);
            Assert.That(Object.FindAnyObjectByType<ForestStickTarget>().IsOpen, Is.False);
            foreach (var trap in Object.FindObjectsByType<ForestDisappearingPlatform>())
            {
                Assert.That(trap.State, Is.EqualTo(ForestDisappearingPlatform.Phase.Dormant));
                Assert.That(trap.GetComponent<Collider2D>().enabled, Is.True);
                Assert.That(trap.transform.Find("Surface").gameObject.activeSelf, Is.True);
                var pit = trap.transform.Find("Concealed pit"); if (pit != null) Assert.That(pit.gameObject.activeSelf, Is.False);
            }
            foreach (var tree in Object.FindObjectsByType<ForestFallingTree>())
            {
                Assert.That(tree.State, Is.EqualTo(ForestFallingTree.Phase.Dormant));
                Assert.That(tree.GetComponent<SpikeTrap>().IsArmed, Is.False);
                Assert.That(tree.transform.eulerAngles.z, Is.Zero);
            }
        }
        [Test]
        public void HiddenCoversMatchGroundAndHudContainsNoSpoilers()
        {
            var safe = GameObject.Find("01 · Safe entrance").GetComponentInChildren<SpriteRenderer>();
            foreach (var trap in Object.FindObjectsByType<ForestDisappearingPlatform>())
            {
                var pit = trap.transform.Find("Concealed pit"); if (pit == null) continue;
                var art = trap.GetComponentInChildren<SpriteRenderer>();
                Assert.That(art.sprite, Is.EqualTo(safe.sprite)); Assert.That(art.color, Is.EqualTo(safe.color));
                Assert.That(art.size.y, Is.EqualTo(safe.size.y)); Assert.That(art.transform.position.y, Is.EqualTo(safe.transform.position.y));
                Assert.That(pit.gameObject.activeSelf, Is.False);
                Assert.That(trap.GetComponent<BoxCollider2D>().bounds.max.y, Is.EqualTo(0).Within(.001));
            }
            var root = Object.FindAnyObjectByType<ForestUI>().Root;
            Assert.That(root.Q("information-card"), Is.Null); Assert.That(root.Q("intro"), Is.Null);
            Assert.That(root.Q<Label>("section").text, Is.EqualTo("ОБМАНЧИВЫЙ\nЛЕС"));
        }
        [UnityTest]
        public IEnumerator NonPlayerCannotActivateBaitOrTrees()
        {
            var probe = new GameObject("Non-player probe", typeof(Rigidbody2D), typeof(CircleCollider2D));
            probe.GetComponent<Rigidbody2D>().gravityScale = 0;
            foreach (Vector2 position in new[] { new Vector2(31.6f, .7f), new Vector2(43.1f, 2) })
            {
                probe.GetComponent<Rigidbody2D>().position = position;
                yield return new WaitForSeconds(.1f);
            }
            Assert.That(GameObject.Find("03 · Apple clearing").GetComponent<ForestDisappearingPlatform>().IsTriggered, Is.False);
            foreach (var tree in Object.FindObjectsByType<ForestFallingTree>()) Assert.That(tree.IsTriggered, Is.False);
            Object.Destroy(probe);
        }
        [UnityTest]
        public IEnumerator StickRangePickupThrowBarrierAndHudWork()
        {
            var stick = Object.FindAnyObjectByType<ForestStick>(); var target = Object.FindAnyObjectByType<ForestStickTarget>();
            var ui = Object.FindAnyObjectByType<ForestUI>();
            Assert.That(stick.TryPickup(), Is.False);
            body.position = new Vector2(36.3f, .6f); yield return new WaitForFixedUpdate();
            Assert.That(stick.TryPickup(), Is.True); yield return null;
            Assert.That(ui.Root.Q("stick-icon").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            body.position = new Vector2(38.8f, .6f); yield return new WaitForFixedUpdate();
            Assert.That(stick.TryThrow(), Is.True);
            Assert.That(stick.GetComponent<Rigidbody2D>().linearVelocity.x, Is.GreaterThan(0));
            yield return new WaitForSeconds(.5f);
            Assert.That(target.IsOpen, Is.True);
            Assert.That(ui.Root.Q("stick-icon").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(death.IsDead, Is.False);
        }
        [UnityTest]
        public IEnumerator StickCanThrowLeftAndAlwaysRecoversAfterMiss()
        {
            var stick = Object.FindAnyObjectByType<ForestStick>();
            Vector2 spawn = stick.transform.position;
            body.position = new Vector2(36.4f, .6f); yield return new WaitForFixedUpdate();
            Assert.That(stick.TryPickup(), Is.True);
            body.linearVelocity = Vector2.left * 2f; yield return null;
            Assert.That(stick.TryThrow(), Is.True);
            Assert.That(stick.GetComponent<Rigidbody2D>().linearVelocity.x, Is.LessThan(0));
            body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(4.3f);
            Assert.That(stick.IsFlying, Is.False); Assert.That(stick.IsHeld, Is.False);
            Assert.That(Vector2.Distance(stick.transform.position, spawn), Is.LessThan(.01f));
            body.position = new Vector2(36.4f, .6f); yield return new WaitForFixedUpdate();
            Assert.That(stick.TryPickup(), Is.True);
        }
        [UnityTest]
        public IEnumerator TreeWarnsRotatesKillsAndSettlesSafely()
        {
            var tree = GameObject.Find("04 · Clearing tree").GetComponent<ForestFallingTree>(); Set(death, "restartDelay", 10f);
            body.position = new Vector2(42.7f, .6f); yield return new WaitForSeconds(.1f);
            Assert.That(tree.IsTriggered, Is.True); Assert.That(tree.GetComponent<SpikeTrap>().IsArmed, Is.False);
            body.position = new Vector2(45.2f, .6f);
            yield return new WaitForSeconds(.45f);
            Assert.That(tree.transform.eulerAngles.z, Is.InRange(1f, 89f));
            yield return new WaitForSeconds(.7f);
            Assert.That(death.IsDead, Is.True); Assert.That(tree.HasFallen, Is.True);
            Assert.That(tree.GetComponent<SpikeTrap>().IsArmed, Is.False);
            Assert.That(tree.GetComponent<BoxCollider2D>().isTrigger, Is.False);
        }
        [UnityTest]
        public IEnumerator LandingAmbushWaitsForSupportAndPauseFreezesTrap()
        {
            var tree = GameObject.Find("06 · Landing tree").GetComponent<ForestFallingTree>();
            body.gravityScale = 0; body.position = new Vector2(57.5f, 2.6f); Tick();
            yield return new WaitForSeconds(.3f); Assert.That(tree.IsTriggered, Is.False);
            body.gravityScale = 3; body.position = new Vector2(57.5f, 1.3f);
            for (int i = 0; i < 8; i++) { Tick(); yield return new WaitForFixedUpdate(); }
            Assert.That(tree.IsTriggered, Is.True);
            GameManager.Instance.Pause(); float angle = tree.GetComponent<Rigidbody2D>().rotation;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(tree.GetComponent<Rigidbody2D>().rotation, Is.EqualTo(angle));
            GameManager.Instance.Resume();
            yield return new WaitForSeconds(1.2f); Assert.That(tree.HasFallen, Is.True);
        }
        [UnityTest]
        public IEnumerator ItemHintsExpireAndDoNotRepeatOnReapproach()
        {
            typeof(ForestUI).GetField("pickupShown", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
            typeof(ForestUI).GetField("throwShown", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
            var ui = Object.FindAnyObjectByType<ForestUI>();
            body.position = new Vector2(36.3f, .6f); yield return new WaitForFixedUpdate(); yield return null; yield return null;
            Assert.That(ui.Root.Q("prompt").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            yield return new WaitForSeconds(3.2f);
            Assert.That(ui.Root.Q("prompt").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            body.position = new Vector2(34f, .6f); yield return null;
            body.position = new Vector2(36.3f, .6f); yield return new WaitForFixedUpdate(); yield return null; yield return null;
            Assert.That(ui.Root.Q("prompt").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(Object.FindAnyObjectByType<ForestStick>().TryPickup(), Is.True);
            yield return null; yield return null;
            Assert.That(ui.Root.Q<Label>("hint").text, Does.StartWith("F"));
            yield return new WaitForSeconds(3.2f);
            Assert.That(ui.Root.Q("prompt").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }
        [UnityTest]
        public IEnumerator PauseUsesSharedManagerAndDoesNotSubmitFromGameplay()
        {
            var ui = Object.FindAnyObjectByType<ForestUI>(); ui.Pause(); yield return null;
            Assert.That(Time.timeScale, Is.Zero); Assert.That(ui.IsModalOpen, Is.True);
            Assert.That(player.ControlEnabled, Is.False);
            using (var submit = NavigationSubmitEvent.GetPooled()) ui.Root.Q<Button>("resume").SendEvent(submit);
            yield return null; Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(ui.IsModalOpen, Is.False);
            using (var submit = NavigationSubmitEvent.GetPooled()) ui.Root.Q<Button>("pause").SendEvent(submit);
            yield return null; Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Playing));
            Assert.That(ui.Root.panel.focusController.focusedElement, Is.Null);
        }
        [UnityTest]
        public IEnumerator GoalCompletesOnceAndReturnToHubPreservesSave()
        {
            string save = PlayerPrefs.GetString(PreviewSaveService.Key, "");
            var goal = Object.FindAnyObjectByType<ForestGoal>(); body.position = goal.transform.position;
            yield return new WaitForSeconds(.1f);
            Assert.That(goal.IsComplete, Is.True); Assert.That(body.simulated, Is.False);
            Assert.That(player.ControlEnabled, Is.False); Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Victory));
            GameManager.Instance.Pause(); Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Victory));
            var ui = Object.FindAnyObjectByType<ForestUI>();
            Assert.That(Application.CanStreamedLevelBeLoaded(HubSceneFlowController.HubScenePath), Is.True, "Hub registered in active build");
            Assert.That(ui.Root.Q<Button>("hub").enabledInHierarchy, Is.True, "Hub button enabled");
            ui.Root.Q<Button>("hub").Focus();
            yield return null;
            using (var submit = NavigationSubmitEvent.GetPooled()) ui.Root.Q<Button>("hub").SendEvent(submit);
            yield return new WaitForSeconds(.4f);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(Object.FindAnyObjectByType<PlayerController>().ControlEnabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetString(PreviewSaveService.Key, ""), Is.EqualTo(save));
            var flow = HubSceneFlowController.Instance;
            Assert.That(flow.CanLaunch(flow.Configuration.World("forest"), 1), Is.False);
        }
        [UnityTest]
        public IEnumerator CameraStaysBoundedWhenPlayerFalls()
        {
            var camera = Object.FindAnyObjectByType<ForestCamera>();
            body.simulated = false; body.transform.position = new Vector3(79, -6, 0);
            yield return new WaitForSeconds(1);
            Assert.That(camera.transform.position.y, Is.EqualTo(3.4f).Within(.01f));
            float half = Camera.main.orthographicSize * Camera.main.aspect;
            Assert.That(camera.transform.position.x, Is.LessThanOrEqualTo(80f - half + .01f));
        }
        [UnityTest]
        public IEnumerator WholeLevelCanBeCompletedWithBaseMovementAndStick()
        {
            Set(death, "restartDelay", 30f);
            yield return Walk(8.2f); yield return Hop(12.3f);
            yield return Walk(17.5f); yield return Hop(19.1f); yield return Hop(22.2f);
            yield return Hop(25.5f); yield return Hop(29f); yield return Hop(33.4f);
            yield return Walk(36.2f);
            var stick = Object.FindAnyObjectByType<ForestStick>(); Assert.That(stick.TryPickup(), Is.True);
            yield return Walk(38.7f); Assert.That(stick.TryThrow(), Is.True);
            yield return new WaitForSeconds(.5f); Assert.That(Object.FindAnyObjectByType<ForestStickTarget>().IsOpen, Is.True);
            yield return Walk(42.6f); yield return Walk(42f);
            yield return new WaitForSeconds(1.2f);
            Assert.That(GameObject.Find("04 · Clearing tree").GetComponent<ForestFallingTree>().HasFallen, Is.True);
            yield return Hop(45.8f); yield return Walk(48.1f); yield return Hop(50.1f);
            yield return Walk(50.8f); yield return Hop(55f);
            yield return Walk(55.5f); yield return Hop(57.5f);
            yield return new WaitForSeconds(1.3f);
            Assert.That(GameObject.Find("06 · Landing tree").GetComponent<ForestFallingTree>().HasFallen, Is.True);
            yield return Hop(60.8f); yield return Walk(64.2f); yield return Hop(68.1f);
            yield return new WaitForSeconds(1.2f);
            Assert.That(GameObject.Find("07 · Final tree").GetComponent<ForestFallingTree>().HasFallen, Is.True);
            yield return Hop(71.7f); yield return Walk(75.8f);
            Assert.That(Object.FindAnyObjectByType<ForestGoal>().IsComplete, Is.True); Assert.That(death.IsDead, Is.False);
        }
        private IEnumerator Walk(float target)
        {
            float direction = Mathf.Sign(target - body.position.x);
            for (int i = 0; i < 400; i++)
            {
                if (!player.ControlEnabled) yield break;
                float delta = target - body.position.x;
                Set(player, "moveInput", delta * direction > .15f ? direction : 0f);
                Tick(); yield return new WaitForFixedUpdate();
                Assert.That(death.IsDead, Is.False, "Died walking to " + target + " at " + body.position);
                if ((target - body.position.x) * direction < .25f && Mathf.Abs(body.linearVelocity.x) < .01f) yield break;
            }
            Assert.Fail("Walk could not reach " + target + "; " + body.position);
        }
        private IEnumerator Hop(float target)
        {
            Tick();
            for (int settle = 0; !player.IsGrounded && settle < 20; settle++)
            {
                Set(player, "moveInput", 0f); yield return new WaitForFixedUpdate(); Tick();
            }
            Assert.That(player.IsGrounded, Is.True, "No support before jumping to " + target + "; " + body.position);
            Set(player, "jumpRequested", true);
            for (int i = 0; i < 120; i++)
            {
                Set(player, "moveInput", body.position.x < target - .15f ? 1f : 0f);
                Tick(); yield return new WaitForFixedUpdate();
                Assert.That(death.IsDead, Is.False, "Died jumping to " + target + "; " + body.position);
                if (i > 4 && player.IsGrounded && Mathf.Abs(body.linearVelocity.x) < .01f)
                {
                    Assert.That(body.position.x, Is.GreaterThan(target - .4f), "Landed short of " + target);
                    yield break;
                }
            }
            Assert.Fail("Jump could not reach " + target + "; " + body.position);
        }
        private void Tick() => typeof(PlayerController).GetMethod("FixedUpdate", Private).Invoke(player, null);
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    }
}
