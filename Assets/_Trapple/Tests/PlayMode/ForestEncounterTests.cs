using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Player;
using Trapple.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class ForestEncounterTests
    {
        private sealed class MemorySave : ISaveService
        {
            public string json;
            public string Read() => json;
            public void Write(string value) => json = value;
        }
        private ForestGuardianEncounter encounter;
        private PlayerDeath player;
        private IProgressionService originalProgress;
        private HubSceneFlowController flow;
        private MemorySave memory;
        private bool hadSave;
        private string savedJson;
        private static readonly FieldInfo ProgressionField = typeof(HubSceneFlowController)
            .GetField("<Progression>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            hadSave = PlayerPrefs.HasKey(PreviewSaveService.Key);
            savedJson = PlayerPrefs.GetString(PreviewSaveService.Key, "");
            Time.timeScale = 1f;
            ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Forest encounter test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(ForestGoal.ScenePath);
            yield return null; yield return null;
            encounter = Object.FindAnyObjectByType<ForestGuardianEncounter>();
            Assert.That(encounter, Is.Not.Null);
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            flow = HubSceneFlowController.Instance;
            Assert.That(flow, Is.Not.Null);
            originalProgress = flow.Progression;
            memory = new MemorySave();
            ProgressionField.SetValue(flow, new PreviewProgressionService(flow.Configuration, memory));
        }

        [UnityTearDown]
        public IEnumerator RestoreSave()
        {
            Time.timeScale = 1f;
            ForestGuardianEncounter.ClearCheckpoint();
            if (flow != null && originalProgress != null) ProgressionField.SetValue(flow, originalProgress);
            if (hadSave) PlayerPrefs.SetString(PreviewSaveService.Key, savedJson);
            else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArenaTriggerStartsIntroOnceAndSkipRestoresGameplay()
        {
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.Route));
            BossEncounterTrigger entrance = null;
            foreach (var trigger in Object.FindObjectsByType<BossEncounterTrigger>())
                if (!(bool)Get(trigger, "checkpoint")) entrance = trigger;
            Assert.That(entrance, Is.Not.Null);
            var body = player.GetComponent<Rigidbody2D>();
            body.position = entrance.GetComponent<Collider2D>().bounds.center;
            player.transform.position = body.position;
            body.simulated = true;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.Intro));
            Assert.That(encounter.Progress.Progress, Is.EqualTo(1f));
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.False);
            Set(encounter, "stateAge", 2f);
            encounter.EnterArena();
            Assert.That(encounter.IntroProgress, Is.GreaterThan(.3f), "Reentry must not restart the cutscene.");
            encounter.SkipIntro();
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.BossFight));
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.True);
            Assert.That(encounter.Guardian.IsFighting, Is.True);
            Assert.That(encounter.TimeRemaining, Is.EqualTo(60f).Within(.02f));
        }

        [UnityTest]
        public IEnumerator IntroAndPauseNeverSpendSurvivalTimeAndPauseCannotSkipIntro()
        {
            encounter.EnterArena();
            yield return new WaitForSeconds(.12f);
            Assert.That(encounter.TimeRemaining, Is.EqualTo(60f));
            GameManager.Instance.Pause();
            float age = encounter.IntroProgress;
            encounter.SkipIntro();
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.Intro));
            Assert.That(encounter.IntroProgress, Is.EqualTo(age));
            GameManager.Instance.Resume(); encounter.SkipIntro();
            yield return new WaitForSeconds(.12f);
            Assert.That(encounter.TimeRemaining, Is.LessThan(60f));
            GameManager.Instance.Pause();
            float remaining = encounter.TimeRemaining;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(encounter.TimeRemaining, Is.EqualTo(remaining));
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.BossFight));
        }

        [UnityTest]
        public IEnumerator DeathReloadRestoresCheckpointAmmoHealthAndTimerWithoutDuplicatePickup()
        {
            var pickups = Object.FindObjectsByType<StickPickup>();
            Assert.That(pickups.Length, Is.InRange(7, 10));
            Assert.That(pickups[0].TryCollect(encounter.Inventory), Is.True);
            Assert.That(pickups[1].TryCollect(encounter.Inventory), Is.True);
            string firstId = pickups[0].PickupId, secondId = pickups[1].PickupId;
            encounter.CaptureCheckpoint(); encounter.EnterArena(); encounter.SkipIntro();
            encounter.Inventory.TrySpend();
            encounter.Guardian.Health.TryTakeDamage(1);
            player.Die();
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.Failed));
            var screen = Object.FindAnyObjectByType<DeathScreenUI>();
            Assert.That(screen.IsOpen, Is.True);
            screen.RespawnButton.onClick.Invoke();
            yield return null; yield return null;
            encounter = Object.FindAnyObjectByType<ForestGuardianEncounter>();
            Assert.That(encounter.Inventory.Count, Is.EqualTo(2));
            Assert.That(encounter.Guardian.Health.CurrentHealth, Is.EqualTo(encounter.Guardian.Health.MaximumHealth));
            Assert.That(encounter.TimeRemaining, Is.EqualTo(60f));
            foreach (var pickup in Object.FindObjectsByType<StickPickup>(FindObjectsInactive.Include))
                if (pickup.PickupId == firstId || pickup.PickupId == secondId)
                {
                    Assert.That(pickup.IsCollected, Is.True);
                    Assert.That(pickup.TryCollect(encounter.Inventory), Is.False);
                }
            Assert.That(encounter.Inventory.Count, Is.EqualTo(2));
            Assert.That(flow.Progression.IsWorldComplete("forest"), Is.False);
        }

        [UnityTest]
        public IEnumerator DamageVictoryRecordsWorldAndBossAndSurvivesSaveReload()
        {
            encounter.EnterArena(); encounter.SkipIntro();
            Set(encounter, "resolutionDuration", .04f);
            Assert.That(encounter.Guardian.Health.TryTakeDamage(encounter.Guardian.Health.CurrentHealth), Is.True);
            Assert.That(encounter.Retreated, Is.False);
            yield return new WaitForSeconds(.09f);
            AssertCompletionPersists();
        }

        [UnityTest]
        public IEnumerator SixtySecondClockExpiryRetreatsAndRecordsSameWorldCompletion()
        {
            encounter.EnterArena(); encounter.SkipIntro();
            int hp = encounter.Guardian.Health.CurrentHealth;
            Set(encounter, "<TimeRemaining>k__BackingField", .04f);
            Set(encounter, "resolutionDuration", .04f);
            yield return new WaitForSeconds(.14f);
            Assert.That(encounter.Retreated, Is.True);
            Assert.That(encounter.Guardian.State, Is.EqualTo(GuardianState.Retreating));
            Assert.That(encounter.Guardian.Health.CurrentHealth, Is.EqualTo(hp));
            AssertCompletionPersists();
        }

        [Test]
        public void ProgressProjectionSupportsVerticalAndReversedSegments()
        {
            var progress = encounter.Progress;
            Set(progress, "route", new[] { new Vector2(0, 0), new Vector2(0, 10), new Vector2(-10, 10) });
            typeof(WorldProgressController).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(progress, null);
            Assert.That(progress.Project(new Vector2(0, 5)), Is.EqualTo(.25f).Within(.001f));
            Assert.That(progress.Project(new Vector2(-5, 10)), Is.EqualTo(.75f).Within(.001f));
            progress.ReachArena();
            Assert.That(progress.Progress, Is.EqualTo(1f));
        }

        private void AssertCompletionPersists()
        {
            Assert.That(encounter.State, Is.EqualTo(ForestEncounterState.Complete));
            Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Victory));
            Assert.That(flow.Progression.IsWorldComplete("forest"), Is.True);
            Assert.That(flow.Progression.IsWorldUnlocked(flow.Configuration.World("factory")), Is.True);
            var reloaded = new PreviewProgressionService(flow.Configuration, memory);
            Assert.That(reloaded.IsWorldComplete("forest"), Is.True);
            Assert.That(reloaded.IsLevelComplete("forest", 0), Is.True);
            Assert.That(ForestGuardianEncounter.HasCheckpoint, Is.False);
        }

        private static object Get(object target, string name) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
