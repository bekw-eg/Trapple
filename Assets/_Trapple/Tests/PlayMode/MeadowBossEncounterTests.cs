using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using Trapple.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Trapple.Tests
{
    public sealed class MeadowBossEncounterTests
    {
        private MeadowBossEncounter encounter;
        private PlayerDeath player;
        private TutorialGoal goal;
        private HubSceneFlowController flow;
        private IProgressionService originalProgress;
        private MemorySave memory;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private sealed class MemorySave : ISaveService
        {
            private string data;
            public string Read() => data;
            public void Write(string json) => data = json;
        }
        [UnitySetUp]
        public IEnumerator LoadMeadow()
        {
            Time.timeScale = 1; MeadowBossEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Meadow test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(GameManager.TutorialScene); yield return null; yield return null;
            encounter = Object.FindAnyObjectByType<MeadowBossEncounter>(); Assert.That(encounter, Is.Not.Null);
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false; player.GetComponent<Rigidbody2D>().simulated = false;
            goal = Object.FindAnyObjectByType<TutorialGoal>(); flow = HubSceneFlowController.Instance;
            originalProgress = flow.Progression; memory = new MemorySave();
            typeof(HubSceneFlowController).GetField("<Progression>k__BackingField", Private)
                .SetValue(flow, new PreviewProgressionService(flow.Configuration, memory));
        }
        [UnityTearDown]
        public IEnumerator LeaveMeadow()
        {
            Time.timeScale = 1; MeadowBossEncounter.ClearCheckpoint();
            if (flow != null && originalProgress != null)
                typeof(HubSceneFlowController).GetField("<Progression>k__BackingField", Private).SetValue(flow, originalProgress);
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu(); yield return null;
        }
        [Test]
        public void NewOrchardArenaReusesAnimatedCombatAndHasNoWeaponsOrLever()
        {
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Route));
            Assert.That(encounter.Boss.State, Is.EqualTo(GuardianState.Inactive));
            Assert.That(encounter.Boss.SurvivalOnly, Is.True);
            Assert.That(encounter.Duration, Is.EqualTo(45)); Assert.That(encounter.TimeRemaining, Is.EqualTo(45));
            Assert.That(goal.IsComplete, Is.False); Assert.That(goal.transform.position.x, Is.GreaterThan(700));
            Assert.That(Object.FindAnyObjectByType<ArenaLever>(), Is.Null);
            Assert.That(Object.FindAnyObjectByType<StickInventory>(), Is.Null);
            Assert.That(Object.FindAnyObjectByType<StickPickup>(), Is.Null);
            Assert.That(Object.FindAnyObjectByType<MeadowBossHUD>(), Is.Not.Null);
            Assert.That(encounter.Boss.GetComponent<ForestGuardianAnimation>(), Is.Not.Null);
        }
        [UnityTest]
        public IEnumerator PhysicalEntranceStartsOnceAndSkipResumesOnlyWhenUnpaused()
        {
            var body = player.GetComponent<Rigidbody2D>(); body.position = goal.transform.position;
            player.transform.position = goal.transform.position; body.simulated = true; Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Intro));
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.False);
            Assert.That(goal.IsComplete, Is.False);
            Set(encounter, "age", 2f); encounter.EnterArena(); Assert.That(Get<float>(encounter, "age"), Is.EqualTo(2));
            GameManager.Instance.Pause(); encounter.SkipIntro();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Intro));
            GameManager.Instance.Resume(); encounter.SkipIntro();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Fighting));
            Assert.That(player.GetComponent<PlayerController>().ControlEnabled, Is.True);
            Assert.That(encounter.Boss.IsFighting, Is.True);
            Assert.That(encounter.TimeRemaining, Is.EqualTo(45));
        }
        [UnityTest]
        public IEnumerator IntroAndPauseCannotAdvanceSurvivalAndDamageCannotWin()
        {
            encounter.EnterArena(); yield return new WaitForSeconds(.12f);
            Assert.That(encounter.TimeRemaining, Is.EqualTo(45)); encounter.SkipIntro();
            int health = encounter.Boss.Health.CurrentHealth;
            Assert.That(encounter.Boss.Health.TryTakeDamage(999), Is.False);
            encounter.Boss.Resolve(false);
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Fighting));
            Assert.That(encounter.Boss.Health.CurrentHealth, Is.EqualTo(health));
            yield return new WaitForSeconds(.12f); GameManager.Instance.Pause();
            float remaining = encounter.TimeRemaining; yield return new WaitForSecondsRealtime(.2f);
            Assert.That(encounter.TimeRemaining, Is.EqualTo(remaining));
            Assert.That(flow.Progression.IsWorldComplete("meadow"), Is.False);
        }
        [UnityTest]
        public IEnumerator FortyFiveScaledCombatSecondsRetreatAndPersistTheWorldOnce()
        {
            encounter.EnterArena(); encounter.SkipIntro();
            int events = 0; encounter.Boss.Resolved += _ => events++;
            // Real encounter clock, accelerated wall time. Physics avoidance is tested separately.
            Time.timeScale = 8; yield return new WaitForSeconds(48.2f);
            Assert.That(player.IsDead, Is.False);
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Complete));
            Assert.That(encounter.TimeRemaining, Is.Zero); Assert.That(encounter.Progress, Is.EqualTo(1));
            Assert.That(encounter.Boss.State, Is.EqualTo(GuardianState.Retreating));
            Assert.That(encounter.Boss.ActiveHazardCount, Is.Zero); Assert.That(events, Is.EqualTo(1));
            Assert.That(goal.IsComplete, Is.True); Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Victory));
            Assert.That(flow.Progression.IsWorldComplete("meadow"), Is.True);
            var restored = new PreviewProgressionService(flow.Configuration, memory);
            Assert.That(restored.IsWorldComplete("meadow"), Is.True);
            Assert.That(MeadowBossEncounter.HasCheckpoint, Is.False);
            encounter.EnterArena(); encounter.Boss.BeginFight(); encounter.Boss.Resolve(true);
            Assert.That(events, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator DeathNearTimeoutCannotWinAndRetryRestoresCheckpointAndClock()
        {
            encounter.CaptureCheckpoint(); encounter.EnterArena(); encounter.SkipIntro();
            Set(encounter, "<TimeRemaining>k__BackingField", .01f); player.Die();
            yield return new WaitForSecondsRealtime(.85f);
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Failed));
            Assert.That(encounter.Boss.IsFighting, Is.False); Assert.That(encounter.Boss.ActiveHazardCount, Is.Zero);
            Assert.That(flow.Progression.IsWorldComplete("meadow"), Is.False);
            var screen = Object.FindAnyObjectByType<DeathScreenUI>(); Assert.That(screen.IsOpen, Is.True);
            screen.RespawnButton.onClick.Invoke(); yield return null; yield return null;
            encounter = Object.FindAnyObjectByType<MeadowBossEncounter>(); player = Object.FindAnyObjectByType<PlayerDeath>();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Route)); Assert.That(encounter.TimeRemaining, Is.EqualTo(45));
            Assert.That(Vector2.Distance(player.transform.position, encounter.CheckpointPosition), Is.LessThan(.4f));
            Assert.That(player.IsDead, Is.False); Assert.That(Camera.main.GetComponent<TutorialCamera>().enabled, Is.True);
        }
        [UnityTest]
        public IEnumerator LeavingTheTrialClearsCheckpointAndContinueLoadsRealForest()
        {
            encounter.CaptureCheckpoint(); Assert.That(MeadowBossEncounter.HasCheckpoint, Is.True);
            encounter.GoToHub(); yield return null; yield return null;
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, Is.EqualTo(HubSceneFlowController.HubScenePath));
            Assert.That(MeadowBossEncounter.HasCheckpoint, Is.False);
            // Continue routing is owned by the existing configuration/service.
            Assert.That(WorldCompletion.TryGetNext(GameManager.TutorialScene, out var world, out int level), Is.False);
            flow.Progression.CompleteTutorial(); flow.Progression.CompleteBoss("meadow");
            Assert.That(WorldCompletion.TryGetNext(GameManager.TutorialScene, out world, out level), Is.True);
            Assert.That(world.id, Is.EqualTo("forest")); Assert.That(level, Is.Zero);
        }
        [UnityTest]
        public IEnumerator CompletionButtonsReceivePointerAndContinueLaunchesForest()
        {
            encounter.EnterArena(); encounter.SkipIntro();
            Set(encounter, "<TimeRemaining>k__BackingField", .01f); Set(encounter, "resolutionDuration", .02f);
            yield return new WaitForSeconds(.45f); Canvas.ForceUpdateCanvases();
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Complete));
            var hud = Object.FindAnyObjectByType<MeadowBossHUD>();
            foreach (var button in hud.GetComponentsInChildren<Button>())
            {
                var rect = (RectTransform)button.transform;
                var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
                var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Any(hit => hit.gameObject.GetComponentInParent<Button>() == button), Is.True, button.name);
            }
            var next = hud.GetComponentsInChildren<Button>().Single(b => b.name == "Continue"); next.onClick.Invoke();
            yield return null; yield return null;
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, Is.EqualTo(ForestGoal.ScenePath));
            Assert.That(MeadowBossEncounter.HasCheckpoint, Is.False);
        }
        [UnityTest]
        public IEnumerator DeathDuringIntroReleasesCameraAndNeverStartsTheFight()
        {
            encounter.EnterArena(); player.Die(); yield return null;
            Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Failed));
            Assert.That(encounter.Boss.IsFighting, Is.False); Assert.That(Camera.main.GetComponent<TutorialCamera>().enabled, Is.True);
            encounter.SkipIntro(); Assert.That(encounter.State, Is.EqualTo(MeadowEncounterState.Failed));
        }
        [UnityTest]
        public IEnumerator AuthoredOrchardAllowsNormalMovementToEvadeEachAttack()
        {
            var boss = encounter.Boss; var body = player.GetComponent<Rigidbody2D>(); var controller = player.GetComponent<PlayerController>();
            foreach (int attack in new[] { 0, 1, 2 })
            {
                boss.CancelFight();
                body.simulated = true; body.linearVelocity = Vector2.zero;
                body.position = new Vector2(boss.transform.position.x - (attack == 1 ? 4.2f : 8f), .56f);
                player.transform.position = body.position; Physics2D.SyncTransforms();
                yield return new WaitForSeconds(.12f);
                Set(boss, "attackIndex", attack); boss.BeginFight(); Set(boss, "remaining", 0f); yield return null;
                Assert.That(boss.State, Is.EqualTo(GuardianState.PreparingAttack));
                Assert.That(boss.CurrentAttack, Is.EqualTo((GuardianAttack)attack));
                if (attack == 0) yield return new WaitForSeconds(.8f);
                if (attack == 2)
                {
                    while (boss.State != GuardianState.Attacking) yield return null;
                    yield return new WaitForSeconds(.55f);
                }
                float finish = Time.time + (attack == 1 ? 1.9f : 1f); bool jump = attack != 1;
                while (Time.time < finish)
                {
                    Set(controller, "moveInput", attack == 1 ? -1f : 1f);
                    if (jump) { Set(controller, "jumpRequested", true); jump = false; }
                    typeof(PlayerController).GetMethod("FixedUpdate", Private).Invoke(controller, null);
                    yield return new WaitForFixedUpdate();
                    Assert.That(player.IsDead, Is.False, "Production movement must evade " + (GuardianAttack)attack + " in the new arena.");
                }
                Set(controller, "moveInput", 0f); boss.CancelFight();
            }
        }
        [Test]
        public void ImportantSurvivalTextIsLargeAtUsersWindowSize()
        {
            var hud = Object.FindAnyObjectByType<MeadowBossHUD>();
            foreach (string name in new[] { "Boss name", "Survival timer", "Attack advice" })
            {
                var text = hud.GetComponentsInChildren<TMP_Text>().Single(t => t.name == name);
                Assert.That(text.fontSize * text.font.faceInfo.capLine / text.font.faceInfo.pointSize * (524f / 1080), Is.GreaterThan(14));
                Assert.That(text.enableAutoSizing, Is.False);
            }
        }
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    }
}
