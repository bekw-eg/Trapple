using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Trapple.Tests
{
    public sealed class GuardianAttackAnimationTests
    {
        private ForestGuardianController boss;
        private PlayerDeath player;
        private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            Time.timeScale = 1f; MeadowBossEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Attack animation systems", typeof(GameManager));
            GameManager.Instance.LoadScene("Assets/_Trapple/Scenes/BossArenas/MeadowBossTest.unity");
            yield return null; yield return null;
            boss = Object.FindAnyObjectByType<ForestGuardianController>();
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
        }

        [UnityTearDown]
        public IEnumerator LeaveArena()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f; boss?.CancelFight(); MeadowBossEncounter.ClearCheckpoint();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        private void StartAttack(int index)
        {
            boss.CancelFight();
            typeof(ForestGuardianController).GetField("attackIndex", Fields).SetValue(boss, index);
            boss.BeginFight(); SetRemaining(0f);
        }
        private void SetRemaining(float value) => typeof(ForestGuardianController).GetField("remaining", Fields).SetValue(boss, value);

        [UnityTest]
        public IEnumerator SlamHasShoulderWindupThenContactAndRecovery()
        {
            StartAttack(1); yield return null;
            var shoulder = boss.transform.Find("Animated torso/Left branch fist shoulder");
            Assert.That(shoulder, Is.Not.Null);
            yield return new WaitForSeconds(1f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, shoulder.localEulerAngles.z)), Is.GreaterThan(80f));
            Assert.That(boss.ActiveHazardCount, Is.Zero);
            SetRemaining(0f); yield return null;
            Assert.That(boss.State, Is.EqualTo(GuardianState.Attacking));
            Assert.That(boss.ActiveHazardCount, Is.Zero, "Damage must wait for the visible swing to land.");
            Assert.That(boss.IsTelegraphing, Is.True);
            yield return new WaitForSeconds(.25f);
            Assert.That(boss.ActiveHazardCount, Is.EqualTo(1));
            Assert.That(boss.IsTelegraphing, Is.False);
            Assert.That(boss.GetComponent<GuardianAttackEffects>().ActiveParticleCount, Is.GreaterThan(0));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, shoulder.localEulerAngles.z)), Is.LessThan(35f));
            yield return new WaitForSeconds(.44f);
            Assert.That(boss.GetComponent<ForestGuardianAnimation>().State, Is.EqualTo(GuardianAnimationState.Recovery));
        }

        [UnityTest]
        public IEnumerator RootEmergesFromGroundAndVisibleHitboxGrowsWithIt()
        {
            StartAttack(0); yield return null; SetRemaining(0f); yield return null;
            float deadline = Time.realtimeSinceStartup + 1f;
            while (boss.ActiveHazardCount == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            var hazard = boss.GetComponentInChildren<ForestGuardianHazard>();
            Assert.That(hazard, Is.Not.Null);
            var box = hazard.GetComponent<BoxCollider2D>();
            float initialHeight = box.size.y;
            float baseY = hazard.transform.position.y - .86f;
            var art = hazard.transform.Find("Animated hazard artwork").GetComponent<SpriteRenderer>();
            Assert.That(art.bounds.min.y, Is.EqualTo(baseY).Within(.12f));
            yield return new WaitForSeconds(.17f);
            Assert.That(box.size.y, Is.GreaterThan(initialHeight + .3f));
            Assert.That(hazard.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [UnityTest]
        public IEnumerator PauseFreezesSwingAndParticlesAndCancelRemovesAllEffects()
        {
            StartAttack(1); yield return null; SetRemaining(0f); yield return null;
            Time.timeScale = 0f;
            var arm = boss.transform.Find("Animated torso/Left branch fist shoulder");
            Quaternion frozen = arm.localRotation;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(boss.ActiveHazardCount, Is.Zero);
            Assert.That(arm.localRotation, Is.EqualTo(frozen));
            Time.timeScale = 1f; yield return new WaitForSeconds(.24f);
            var effects = boss.GetComponent<GuardianAttackEffects>();
            Assert.That(effects.ActiveParticleCount, Is.GreaterThan(0));
            var mote = boss.transform.Find("Attack mote 0"); Vector3 position = mote.position;
            Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.2f);
            Assert.That(mote.position, Is.EqualTo(position));
            boss.CancelFight(); Time.timeScale = 1f; yield return new WaitForSeconds(.3f);
            Assert.That(effects.ActiveParticleCount, Is.Zero);
            Assert.That(boss.ActiveHazardCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CancelDuringSwingPreventsDelayedDamageAndDebris()
        {
            StartAttack(0); yield return null; SetRemaining(0f); yield return null;
            Assert.That(boss.IsTelegraphing, Is.True);
            boss.CancelFight(); yield return new WaitForSeconds(.35f);
            Assert.That(boss.ActiveHazardCount, Is.Zero);
            Assert.That(boss.GetComponent<GuardianAttackEffects>().ActiveParticleCount, Is.Zero);
            Assert.That(boss.IsTelegraphing, Is.False);
        }

        [UnityTest]
        public IEnumerator LeafWavesFlutterWithoutRotatingTheirPhysicsRoot()
        {
            StartAttack(2); yield return null; SetRemaining(0f); yield return null;
            var hazard = boss.GetComponentInChildren<ForestGuardianHazard>();
            Assert.That(hazard, Is.Not.Null);
            var art = hazard.transform.Find("Animated hazard artwork"); Quaternion rotation = art.localRotation;
            yield return new WaitForSeconds(.12f);
            Assert.That(Quaternion.Angle(rotation, art.localRotation), Is.GreaterThan(15f));
            Assert.That(hazard.GetComponent<Rigidbody2D>().rotation, Is.EqualTo(0f).Within(.01f));
            Assert.That(boss.GetComponent<GuardianAttackEffects>().ActiveParticleCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator CaptureAnimatedAttacks()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-capture-attacks") < 0)
                Assert.Ignore("Opt-in GPU sequence capture: -capture-attacks");
            var encounter = Object.FindAnyObjectByType<MeadowBossEncounter>(); encounter.EnterArena(); encounter.SkipIntro();
            player.transform.position = new Vector3(boss.transform.position.x - 8f, .6f, 0f);
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            Time.captureDeltaTime = .05f;
            string folder = Path.Combine(Application.dataPath, "../Logs/AttackFrames"); Directory.CreateDirectory(folder);
            for (int attack = 0; attack < 3; attack++)
            {
                StartAttack(attack);
                for (int frame = 0; frame < 42; frame++)
                {
                    yield return null;
                    OrchardVisualTests.CaptureTo(Path.Combine(folder, $"attack-{attack}-{frame:D3}.png"), 934, 524);
                }
            }
            Assert.That(Directory.GetFiles(folder, "attack-*.png").Length, Is.EqualTo(126));
        }
    }
}
