using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Trapple.Tests
{
    public sealed class GuardianVarietyTests
    {
        private ForestGuardianController boss;
        private GuardianSpecialAttacks special;
        private PlayerDeath player;
        private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Meadow = "Assets/_Trapple/Scenes/BossArenas/MeadowBossTest.unity";
        private const string Forest = "Assets/_Trapple/Scenes/BossArenas/ForestGuardianTest.unity";

        [UnitySetUp]
        public IEnumerator SetUp() { yield return Load(Meadow); }

        private IEnumerator Load(string scene)
        {
            Time.timeScale = 1f; MeadowBossEncounter.ClearCheckpoint(); ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Variety test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(scene); yield return null; yield return null;
            boss = Object.FindAnyObjectByType<ForestGuardianController>(); special = boss.GetComponent<GuardianSpecialAttacks>();
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            player.transform.position = new Vector3(boss.transform.position.x - 8f, .6f, 0f);
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            Assert.That(special, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureDeltaTime = 0f; Time.timeScale = 1f; boss?.CancelFight();
            MeadowBossEncounter.ClearCheckpoint(); ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu(); yield return null;
        }

        private void Prepare(int index)
        {
            boss.CancelFight(); Set(boss, "attackIndex", index); boss.BeginFight(); Set(boss, "remaining", 0f);
        }
        private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Fields).SetValue(obj, value);
        private ForestGuardianHazard[] Active() => boss.GetComponentsInChildren<ForestGuardianHazard>();

        [UnityTest]
        public IEnumerator AuthoredBossesHaveDifferentAttackSetsAndAnActualGiantBranchSprite()
        {
            foreach (bool forest in new[] { false, true })
            {
                if (forest) yield return Load(Forest);
                var expected = forest ? new[] { GuardianAttack.RootStrike, GuardianAttack.BranchSlam, GuardianAttack.LeafStorm,
                    GuardianAttack.BranchRain, GuardianAttack.RootWave, GuardianAttack.LeafFan } :
                    new[] { GuardianAttack.RootStrike, GuardianAttack.BranchSlam, GuardianAttack.LeafStorm,
                    GuardianAttack.BranchRain, GuardianAttack.AppleBarrage };
                for (int i = 0; i <= expected.Length; i++)
                {
                    Prepare(i); yield return null;
                    Assert.That(boss.CurrentAttack, Is.EqualTo(expected[i % expected.Length]));
                    Assert.That(boss.State, Is.EqualTo(GuardianState.PreparingAttack));
                }
                var sprite = (Sprite)typeof(GuardianSpecialAttacks).GetField("giantBranch", Fields).GetValue(special);
                Assert.That(sprite, Is.Not.Null); Assert.That(sprite.name, Does.Contain("Giant"));
                Assert.That(boss.GetComponentsInChildren<ForestGuardianHazard>(true).Length, Is.EqualTo(32));
            }
        }

        [UnityTest]
        public IEnumerator BothBossesDropExactlyFiftyBranchesWithoutRetargetingOrPoolOverflow()
        {
            foreach (bool forest in new[] { false, true })
            {
                if (forest) yield return Load(Forest);
                Prepare(3); yield return null; Set(boss, "remaining", 0f); yield return null;
                Assert.That(special.PendingCount, Is.EqualTo(5)); Assert.That(boss.ActiveHazardCount, Is.Zero);
                var markedX = boss.GetComponentsInChildren<SpriteRenderer>().Where(s => s.name.StartsWith("Reserved landing") && s.enabled)
                    .Select(s => s.transform.position.x).ToArray();
                player.transform.position += Vector3.right * 1.5f;
                float deadline = Time.realtimeSinceStartup + 3f;
                while (special.EmittedCount == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(special.EmittedCount, Is.EqualTo(5));
                foreach (var hazard in Active())
                {
                    Assert.That(markedX.Any(x => Mathf.Abs(x - hazard.transform.position.x) < .02f), Is.True,
                        "A launched branch must keep the location that was marked before Appi moved.");
                    Assert.That(hazard.Kind, Is.EqualTo(GuardianAttack.BranchRain));
                    Assert.That(hazard.Velocity.y, Is.LessThan(0f));
                    Assert.That(hazard.transform.Find("Animated hazard artwork").GetComponent<SpriteRenderer>().bounds.size.y, Is.GreaterThan(2.5f));
                }
                Time.timeScale = 4f; deadline = Time.realtimeSinceStartup + 5f;
                int peak = 0;
                while (special.IsRunning && Time.realtimeSinceStartup < deadline)
                { peak = Mathf.Max(peak, boss.ActiveHazardCount); yield return null; }
                Assert.That(special.IsRunning, Is.False); Assert.That(special.PlannedCount, Is.EqualTo(50));
                Assert.That(special.EmittedCount, Is.EqualTo(50)); Assert.That(peak, Is.LessThanOrEqualTo(32));
                Assert.That(boss.ActiveHazardCount, Is.Zero); boss.CancelFight(); Time.timeScale = 1f;
            }
        }

        [UnityTest]
        public IEnumerator PauseAndDeathCancelReservationsAndDoNotLeaveDelayedBranches()
        {
            Prepare(3); yield return null; Set(boss, "remaining", 0f); yield return null;
            GameManager.Instance.Pause(); yield return new WaitForSecondsRealtime(.9f);
            Assert.That(special.EmittedCount, Is.Zero); Assert.That(special.PendingCount, Is.EqualTo(5));
            GameManager.Instance.Resume(); player.Die(); yield return null; yield return new WaitForSecondsRealtime(.9f);
            Assert.That(special.PendingCount, Is.Zero); Assert.That(special.IsRunning, Is.False);
            Assert.That(boss.ActiveHazardCount, Is.Zero);
            Assert.That(boss.GetComponentsInChildren<SpriteRenderer>().Where(s => s.name.StartsWith("Reserved landing")).All(s => !s.enabled), Is.True);
        }

        [UnityTest]
        public IEnumerator DamagingTheSecondBossInterruptsTheEntireRain()
        {
            yield return Load(Forest); Prepare(3); yield return null; Set(boss, "remaining", 0f); yield return null;
            Assert.That(special.IsRunning, Is.True); Assert.That(boss.Health.TryTakeDamage(1), Is.True);
            yield return new WaitForSeconds(.9f);
            Assert.That(special.PendingCount, Is.Zero); Assert.That(boss.ActiveHazardCount, Is.Zero);
            Assert.That(special.EmittedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator OrchardApplesBounceThenRollWithRealHitboxes()
        {
            Prepare(4); yield return null; Set(boss, "remaining", 0f); yield return null;
            var apple = Active().Single();
            Assert.That(apple.Kind, Is.EqualTo(GuardianAttack.AppleBarrage));
            float deadline = Time.realtimeSinceStartup + 4f;
            while (apple.BounceCount < 3 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(apple.BounceCount, Is.EqualTo(3)); Assert.That(apple.Velocity.y, Is.EqualTo(0f));
            Assert.That(apple.Velocity.x, Is.LessThan(-3f)); Assert.That(apple.Armed, Is.True);
            var body = player.GetComponent<Rigidbody2D>();
            body.position = apple.transform.position + Vector3.left * .65f; player.transform.position = body.position;
            body.simulated = true; Physics2D.SyncTransforms();
            deadline = Time.realtimeSinceStartup + .6f;
            while (!player.IsDead && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(player.IsDead, Is.True, "A rolling apple must use the real death/respawn flow.");
        }

        [UnityTest]
        public IEnumerator RootWaveAdvancesAcrossGroundAndLeafFanUsesThreeDifferentTrajectories()
        {
            yield return Load(Forest); Prepare(4); yield return null; Set(boss, "remaining", 0f); yield return null;
            yield return new WaitForSeconds(.75f);
            float firstX = Active().First().transform.position.x;
            yield return new WaitForSeconds(.25f);
            Assert.That(Active().Any(h => h.transform.position.x < firstX - .5f), Is.True);
            Prepare(5); yield return null; Set(boss, "remaining", 0f); yield return null;
            Assert.That(Active().Length, Is.EqualTo(3));
            var speeds = Active().Select(h => h.Velocity.y).Distinct().OrderBy(v => v).ToArray();
            Assert.That(speeds.Length, Is.EqualTo(3)); Assert.That(speeds.First(), Is.LessThan(0f)); Assert.That(speeds.Last(), Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator RealMovementEvadesTheOpeningRainVolley()
        {
            var control = player.GetComponent<PlayerController>(); var body = player.GetComponent<Rigidbody2D>();
            body.simulated = true; yield return new WaitForSeconds(.15f);
            Prepare(3); yield return null; Set(boss, "remaining", 0f); yield return null;
            float finish = Time.time + 1.8f;
            while (Time.time < finish)
            {
                Set(control, "moveInput", -1f);
                if (control.IsGrounded) Set(control, "jumpRequested", true);
                typeof(PlayerController).GetMethod("FixedUpdate", Fields).Invoke(control, null);
                yield return new WaitForFixedUpdate(); Assert.That(player.IsDead, Is.False);
            }
            Assert.That(special.EmittedCount, Is.GreaterThanOrEqualTo(10));
        }

        [UnityTest]
        public IEnumerator JumpAndRunCrossesTheSecondBossRootWave()
        {
            yield return Load(Forest);
            var control = player.GetComponent<PlayerController>(); var body = player.GetComponent<Rigidbody2D>();
            body.simulated = true; yield return new WaitForSeconds(.15f);
            Prepare(4); yield return null; Set(boss, "remaining", 0f); yield return null;
            float deadline = Time.time + 4f; bool seen = false;
            while (!seen && Time.time < deadline)
            {
                seen = boss.GetComponentsInChildren<SpriteRenderer>().Any(s => s.enabled && s.name.StartsWith("Reserved landing") &&
                    Mathf.Abs(s.transform.position.x - player.transform.position.x) < .85f);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(seen, Is.True); yield return new WaitForSeconds(.25f);
            bool jump = true; float finish = Time.time + 1.1f;
            while (Time.time < finish)
            {
                Set(control, "moveInput", 1f); if (jump) { Set(control, "jumpRequested", true); jump = false; }
                typeof(PlayerController).GetMethod("FixedUpdate", Fields).Invoke(control, null);
                yield return new WaitForFixedUpdate(); Assert.That(player.IsDead, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator ARealJumpClearsTheRollingApple()
        {
            Prepare(4); yield return null; Set(boss, "remaining", 0f); yield return null;
            var apple = Active().Single();
            // Isolate one production apple to verify its actual rolling collider against the jump arc.
            Set(special, "emitted", 6);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (apple.BounceCount < 3 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(apple.BounceCount, Is.EqualTo(3));
            var control = player.GetComponent<PlayerController>(); var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(apple.transform.position.x - 2f, .6f); player.transform.position = body.position;
            body.simulated = true; Physics2D.SyncTransforms(); yield return new WaitForSeconds(.1f);
            Set(control, "jumpRequested", true);
            float finish = Time.time + .8f;
            while (Time.time < finish)
            {
                typeof(PlayerController).GetMethod("FixedUpdate", Fields).Invoke(control, null);
                yield return new WaitForFixedUpdate(); Assert.That(player.IsDead, Is.False);
            }
            Assert.That(apple.transform.position.x, Is.LessThan(player.transform.position.x - .8f));
        }

        [UnityTest]
        public IEnumerator CaptureBothBossAttackSets()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-capture-variety") < 0) Assert.Ignore("Opt-in GPU capture: -capture-variety");
            string folder = Path.Combine(Application.dataPath, "../Logs/VarietyFrames"); Directory.CreateDirectory(folder);
            for (int clip = 0; clip < 5; clip++)
            {
                bool forest = clip >= 2; yield return Load(forest ? Forest : Meadow);
                if (forest)
                {
                    var encounter = Object.FindAnyObjectByType<ForestGuardianEncounter>(); encounter.EnterArena();
                    encounter.SkipIntro();
                }
                else { var encounter = Object.FindAnyObjectByType<MeadowBossEncounter>(); encounter.EnterArena(); encounter.SkipIntro(); }
                player.GetComponent<PlayerController>().enabled = false;
                Time.captureDeltaTime = .1f;
                int index = clip == 0 || clip == 2 ? 3 : clip == 1 || clip == 3 ? 4 : 5;
                Prepare(index);
                for (int frame = 0; frame < (clip >= 3 ? 60 : 100); frame++)
                {
                    yield return null;
                    OrchardVisualTests.CaptureTo(Path.Combine(folder, $"clip-{clip}-{frame:D3}.png"), 934, 524);
                }
                Time.captureDeltaTime = 0f;
            }
        }
    }
}
