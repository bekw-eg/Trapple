using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class MeadowBossEncounterTests
    {
        private MeadowBossEncounter encounter;
        private PlayerDeath player;
        private TutorialGoal goal;
        private HubSceneFlowController flow;
        private IProgressionService originalProgress;
        private sealed class MemorySave : ISaveService
        {
            private string data;
            public string Read() => data;
            public void Write(string json) => data = json;
        }

        [UnitySetUp]
        public IEnumerator LoadMeadow()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance == null) new GameObject("Meadow encounter test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(GameManager.TutorialScene);
            yield return null; yield return null;
            encounter = Object.FindAnyObjectByType<MeadowBossEncounter>();
            Assert.That(encounter, Is.Not.Null);
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            goal = Object.FindAnyObjectByType<TutorialGoal>();
            flow = HubSceneFlowController.Instance;
            originalProgress = flow.Progression;
            typeof(HubSceneFlowController).GetField("<Progression>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(flow, new PreviewProgressionService(flow.Configuration, new MemorySave()));
        }

        [UnityTearDown]
        public IEnumerator LeaveMeadow()
        {
            Time.timeScale = 1f;
            if (flow != null && originalProgress != null)
                typeof(HubSceneFlowController).GetField("<Progression>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(flow, originalProgress);
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        [Test]
        public void ExistingTreeWaitsAtEndOfRouteAndMeadowHasNoCombatSticks()
        {
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Route));
            Assert.That(encounter.Boss.gameObject.activeSelf, Is.False);
            Assert.That(goal.IsComplete, Is.False);
            Assert.That(goal.transform.position.x, Is.GreaterThan(100f));
            Assert.That(Object.FindObjectsByType<StickInventory>().Length, Is.Zero);
            Assert.That(Object.FindObjectsByType<StickPickup>().Length, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AppleTriggerIntroducesExistingBossInsteadOfPrematureVictory()
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.position = goal.transform.position;
            player.transform.position = goal.transform.position;
            body.simulated = true;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Intro));
            Assert.That(encounter.Boss.gameObject.activeSelf, Is.True);
            Assert.That(encounter.Boss.enabled, Is.False);
            Assert.That(goal.IsComplete, Is.False);
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Playing));
            GameManager.Instance.Pause(); encounter.SkipIntro();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Intro));
            GameManager.Instance.Resume(); encounter.SkipIntro();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Fighting));
            Assert.That(encounter.Boss.GetComponent<BossHealth>().MaximumHealth, Is.EqualTo(100));
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator FourOriginalLeverHitsResolveFightWithoutImmediateLevelCompletion()
        {
            encounter.EnterArena(); encounter.SkipIntro();
            yield return null;
            var boss = encounter.Boss;
            // Keep the test at its explicit vulnerability openings, using the production E-lever API.
            boss.enabled = false;
            var health = boss.GetComponent<BossHealth>();
            var lever = Object.FindAnyObjectByType<ArenaLever>();
            player.transform.position = lever.transform.position;
            // Re-enable controller so the production Died subscription receives the final hit.
            Set(boss, "initialDelay", 100f); boss.enabled = true;
            for (int i = 0; i < 4; i++)
            {
                health.SetVulnerable(false); health.SetVulnerable(true);
                Assert.That(lever.TryActivate(), Is.True);
            }
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Resolving));
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Playing));
            Assert.That(goal.IsComplete, Is.False);
            foreach (var hazard in boss.GetComponentsInChildren<BossHazard>()) Assert.That(hazard.Armed, Is.False);
            Set(encounter, "resolutionDuration", .04f);
            yield return new WaitForSeconds(.09f);
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Complete));
            Assert.That(goal.IsComplete, Is.True);
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Victory));
            Assert.That(flow.Progression.IsWorldComplete("meadow"), Is.True);
        }

        [UnityTest]
        public IEnumerator DeathDuringIntroReleasesCameraAndNeverStartsTheFight()
        {
            encounter.EnterArena();
            player.Die(); yield return null;
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Failed));
            Assert.That(encounter.Boss.enabled, Is.False);
            Assert.That(Camera.main.GetComponent<TutorialCamera>().enabled, Is.True);
            encounter.SkipIntro();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Failed));
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
