using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class BossSystemsTests
    {
        private BossController boss;
        private BossHealth health;
        private BossAttack attacks;
        private BossPhaseController phases;
        private PlayerDeath player;
        private ArenaLever lever;

        [UnitySetUp]
        public IEnumerator LoadArena()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance == null) new GameObject("Test Game Systems", typeof(GameManager));
            GameManager.Instance.StartBossTest();
            yield return null;
            boss = Object.FindAnyObjectByType<BossController>();
            health = boss.GetComponent<BossHealth>();
            attacks = boss.GetComponent<BossAttack>();
            phases = boss.GetComponent<BossPhaseController>();
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            lever = Object.FindAnyObjectByType<ArenaLever>();
            boss.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            Time.timeScale = 1f;
            GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        [Test]
        public void HealthRejectsClosedAndInvalidHitsAndDiesOnlyOnce()
        {
            int deaths = 0;
            health.Died += () => deaths++;
            Assert.That(health.CurrentHealth, Is.EqualTo(100));
            Assert.That(health.TryTakeDamage(25), Is.False);
            health.SetVulnerable(true);
            Assert.That(health.TryTakeDamage(0), Is.False);
            Assert.That(health.TryTakeDamage(-25), Is.False);
            Assert.That(health.TryTakeDamage(999), Is.True);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.IsVulnerable, Is.False);
            health.SetVulnerable(true);
            Assert.That(health.TryTakeDamage(25), Is.False);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void PhaseTwoStartsStrictlyBelowHalfHealth()
        {
            health.SetVulnerable(true);
            health.TryTakeDamage(50);
            Assert.That(phases.CurrentPhase, Is.EqualTo(1));
            Assert.That(phases.DurationMultiplier, Is.EqualTo(1f));
            health.TryTakeDamage(1);
            Assert.That(phases.CurrentPhase, Is.EqualTo(2));
            Assert.That(phases.DurationMultiplier, Is.LessThan(1f));
        }

        [UnityTest]
        public IEnumerator LeverRequiresRangeAndVulnerabilityAndAllowsOneHitPerOpening()
        {
            player.transform.position = new Vector3(2f, 0.6f, 0f);
            health.SetVulnerable(true);
            Assert.That(lever.TryActivate(), Is.False);
            player.transform.position = new Vector3(-6f, 0.6f, 0f);
            health.SetVulnerable(false);
            Assert.That(lever.TryActivate(), Is.False);
            health.SetVulnerable(true);
            Assert.That(lever.TryActivate(), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(75));
            Assert.That(lever.TryActivate(), Is.False);
            health.SetVulnerable(false);
            health.SetVulnerable(true);
            Assert.That(lever.TryActivate(), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(50));
            Time.timeScale = 0f;
            health.SetVulnerable(false);
            health.SetVulnerable(true);
            Assert.That(lever.TryActivate(), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryAttackTelegraphsBeforeArmingAndCleansUp()
        {
            // Move the real player outside the arena so this test can observe every attack.
            player.transform.position = new Vector3(-20f, 1f, 0f);
            player.GetComponent<Rigidbody2D>().simulated = false;
            foreach (BossAttackKind kind in System.Enum.GetValues(typeof(BossAttackKind)))
            {
                bool attackStarted = false;
                var running = attacks.StartCoroutine(attacks.Execute(kind, 0.65f, () => attackStarted = true));
                yield return null;
                Assert.That(attacks.IsTelegraphing, Is.True, kind.ToString());
                Assert.That(attackStarted, Is.False);
                foreach (var hazard in boss.GetComponentsInChildren<BossHazard>()) Assert.That(hazard.Armed, Is.False);
                yield return new WaitForSeconds(0.4f);
                Assert.That(attackStarted, Is.False, "Minimum readable warning must survive phase two.");
                yield return running;
                Assert.That(attackStarted, Is.True);
                Assert.That(attacks.IsTelegraphing, Is.False);
                Assert.That(attacks.IsExecuting, Is.False);
                foreach (var hazard in boss.GetComponentsInChildren<BossHazard>()) Assert.That(hazard.Armed, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator HazardCallsRealPlayerDeathAndReloadRestoresTime()
        {
            var hazard = boss.GetComponentsInChildren<BossHazard>()[0];
            hazard.transform.position = player.transform.position;
            hazard.SetArmed(true);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(player.IsDead, Is.True);
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.False);
            GameManager.Instance.Pause();
            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameManager.BossScene));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Object.FindAnyObjectByType<PlayerDeath>().IsDead, Is.False);
        }

        [UnityTest]
        public IEnumerator PauseFreezesAttackTimerAndResumeRestoresControl()
        {
            Set(boss, "initialDelay", 0.02f);
            boss.enabled = true;
            yield return new WaitForSeconds(0.05f);
            Assert.That(boss.State, Is.EqualTo(BossState.Telegraphing));
            GameManager.Instance.Pause();
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.False);
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.That(boss.State, Is.EqualTo(BossState.Telegraphing));
            GameManager.Instance.Resume();
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator FourEnvironmentalHitsCompleteEncounterAndCancelAllHazards()
        {
            int completions = 0;
            boss.FightFinished += () => completions++;
            Set(boss, "initialDelay", 0.02f);
            boss.enabled = true;
            player.transform.position = new Vector3(-6f, 0.6f, 0f);
            player.GetComponent<Rigidbody2D>().simulated = false;
            // Drive openings explicitly; damage still goes through the production lever and health API.
            for (int i = 0; i < 4; i++)
            {
                health.SetVulnerable(false);
                health.SetVulnerable(true);
                Assert.That(lever.TryActivate(), Is.True);
            }
            yield return null;
            Assert.That(health.IsDead, Is.True);
            Assert.That(phases.CurrentPhase, Is.EqualTo(2));
            Assert.That(boss.State, Is.EqualTo(BossState.Dead));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Victory));
            Assert.That(attacks.IsTelegraphing, Is.False);
            foreach (var hazard in boss.GetComponentsInChildren<BossHazard>()) Assert.That(hazard.Armed, Is.False);
            Assert.That(lever.TryActivate(), Is.False);
        }

        [UnityTest]
        public IEnumerator ArenaStartsEncounterWithoutReenablingController()
        {
            GameManager.Instance.StartBossTest();
            yield return null;
            var initialBoss = Object.FindAnyObjectByType<BossController>();
            var initialPlayer = Object.FindAnyObjectByType<PlayerDeath>();
            initialPlayer.GetComponent<PlayerController>().enabled = false;
            initialPlayer.GetComponent<Rigidbody2D>().simulated = false;
            initialPlayer.transform.position = new Vector3(-20f, 1f, 0f);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (initialBoss.State == BossState.Waiting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(initialBoss.enabled, Is.True);
            Assert.That(initialBoss.State, Is.EqualTo(BossState.Telegraphing));
            Assert.That(initialBoss.GetComponent<BossAttack>().IsTelegraphing, Is.True);
        }

        [UnityTest]
        public IEnumerator EncounterOpensAndClosesVulnerabilityAfterItsAttack()
        {
            player.transform.position = new Vector3(-20f, 1f, 0f);
            player.GetComponent<Rigidbody2D>().simulated = false;
            Set(boss, "initialDelay", 0.02f);
            Set(boss, "vulnerabilityDuration", 0.3f);
            Set(attacks, "warningDuration", 0.1f);
            Set(attacks, "minimumWarningDuration", 0.1f);
            Set(attacks, "rootDuration", 0.1f);
            boss.enabled = true;
            float deadline = Time.realtimeSinceStartup + 4f;
            while (boss.State != BossState.Vulnerable && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(boss.State, Is.EqualTo(BossState.Vulnerable));
            Assert.That(health.IsVulnerable, Is.True);
            Assert.That(health.TryTakeDamage(25), Is.True);
            yield return new WaitForSeconds(0.4f);
            Assert.That(health.IsVulnerable, Is.False);
            Assert.That(health.TryTakeDamage(25), Is.False);
            Assert.That(boss.State, Is.EqualTo(BossState.Waiting));
        }

        [UnityTest]
        public IEnumerator MainMenuPlayRestartAndReturnKeepOneManager()
        {
            GameManager.Instance.ReturnToMenu();
            yield return null;
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.MainMenu));
            GameManager.Instance.StartGame();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameManager.TutorialScene));
            GameManager.Instance.Pause();
            GameManager.Instance.RestartLevel();
            yield return null;
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Object.FindObjectsByType<GameManager>().Length, Is.EqualTo(1));
            GameManager.Instance.ReturnToMenu();
            yield return null;
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.MainMenu));
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
