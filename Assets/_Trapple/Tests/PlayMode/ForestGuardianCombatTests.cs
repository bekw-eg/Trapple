using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class ForestGuardianCombatTests
    {
        private ForestGuardianController guardian;
        private PlayerDeath player;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance == null) new GameObject("Guardian test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(ForestGoal.ScenePath);
            yield return null;
            yield return null;
            guardian = Object.FindAnyObjectByType<ForestGuardianController>();
            Assert.That(guardian, Is.Not.Null, "Run Trapple/Build Boss Progression before combat tests.");
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            player.transform.position = new Vector3(-100f, 3f, 0f);
            guardian.Initialize(player, new Vector2(guardian.transform.position.x - 16f, guardian.transform.position.x + 2f), 0f);
        }

        [UnityTearDown]
        public IEnumerator LeaveForest()
        {
            Time.timeScale = 1f;
            guardian?.CancelFight();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        [Test]
        public void IntroCannotDamageAndFightStartsOnlyOnce()
        {
            guardian.BeginIntro();
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Intro));
            Assert.That(guardian.Health.IsVulnerable, Is.False);
            Assert.That(guardian.Health.TryTakeDamage(1), Is.False);
            guardian.BeginFight();
            guardian.BeginFight();
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Idle));
            Assert.That(guardian.Health.IsVulnerable, Is.True);
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TelegraphPausesAndHurtCancelsTheEntireAttack()
        {
            Set(guardian, "warningDuration", .15f);
            Set(guardian, "rootDuration", 1f);
            guardian.BeginFight();
            Set(guardian, "remaining", 0f);
            yield return null;
            Assert.That(guardian.State, Is.EqualTo(GuardianState.PreparingAttack));
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
            GameManager.Instance.Pause();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(guardian.State, Is.EqualTo(GuardianState.PreparingAttack));
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
            GameManager.Instance.Resume();
            yield return new WaitForSeconds(.2f);
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Attacking));
            Assert.That(guardian.ActiveHazardCount, Is.EqualTo(1));
            Assert.That(guardian.Health.TryTakeDamage(1), Is.True);
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Stunned));
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PatternContainsThreeDistinctWarnedAttacks()
        {
            Set(guardian, "warningDuration", .08f);
            Set(guardian, "rootDuration", .08f);
            Set(guardian, "slamDuration", .08f);
            Set(guardian, "minimumAttackInterval", .02f);
            Set(guardian, "maximumAttackInterval", .02f);
            Set(guardian, "leafWaves", 1);
            Set(guardian, "leafWaveInterval", .08f);
            guardian.BeginFight();
            Set(guardian, "remaining", 0f);
            var warned = new HashSet<GuardianAttack>();
            var executed = new HashSet<GuardianAttack>();
            float deadline = Time.realtimeSinceStartup + 4f;
            while (executed.Count < 3 && Time.realtimeSinceStartup < deadline)
            {
                if (guardian.State == GuardianState.PreparingAttack) warned.Add(guardian.CurrentAttack);
                if (guardian.State == GuardianState.Attacking)
                {
                    Assert.That(warned.Contains(guardian.CurrentAttack), Is.True);
                    executed.Add(guardian.CurrentAttack);
                    Assert.That(guardian.ActiveHazardCount, Is.GreaterThan(0));
                }
                yield return null;
            }
            Assert.That(executed.Count, Is.EqualTo(3));
            guardian.CancelFight();
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
        }

        [Test]
        public void DamageVictoryAndTimeoutResolveOnceAndRejectRestart()
        {
            int completions = 0;
            bool retreated = true;
            guardian.Resolved += value => { completions++; retreated = value; };
            guardian.BeginFight();
            Assert.That(guardian.Health.TryTakeDamage(guardian.Health.CurrentHealth), Is.True);
            guardian.Resolve(true);
            guardian.BeginIntro();
            guardian.BeginFight();
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(retreated, Is.False);
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Defeated));
            Assert.That(guardian.Health.IsVulnerable, Is.False);
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
        }

        [Test]
        public void SurvivalRetreatDoesNotRequireAnyDamage()
        {
            int hp = guardian.Health.CurrentHealth;
            int completions = 0;
            guardian.Resolved += retreat => { Assert.That(retreat, Is.True); completions++; };
            guardian.BeginFight();
            guardian.Resolve(true);
            guardian.Resolve(false);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(guardian.Health.CurrentHealth, Is.EqualTo(hp));
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Retreating));
        }

        [UnityTest]
        public IEnumerator DeathCancelsCombatWithoutReportingVictory()
        {
            int completions = 0;
            guardian.Resolved += _ => completions++;
            guardian.BeginFight();
            player.Die();
            yield return null;
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Inactive));
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
            Assert.That(guardian.Health.IsVulnerable, Is.False);
            guardian.Resolve(true);
            Assert.That(completions, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AuthoredCombatStickTravelsThroughPhysicsAndHitsGuardianFromBothSides()
        {
            var inventory = player.GetComponent<StickInventory>();
            var throwing = player.GetComponent<StickThrowController>();
            var artwork = player.GetComponent<SpriteRenderer>();
            guardian.BeginFight();
            Set(guardian, "remaining", 100f); // Isolate ballistics from attacks, retaining real colliders and gravity.
            int initialHealth = guardian.Health.CurrentHealth;
            foreach (int direction in new[] { 1, -1 })
            {
                int before = guardian.Health.CurrentHealth;
                player.transform.position = new Vector3(guardian.transform.position.x - direction * 4f, .55f, 0f);
                player.GetComponent<Rigidbody2D>().position = player.transform.position;
                artwork.flipX = direction < 0;
                inventory.Restore(1);
                Physics2D.SyncTransforms();
                Assert.That(throwing.TryThrow(), Is.True);
                var projectile = throwing.LastProjectile;
                Assert.That(projectile.Velocity.x * direction, Is.GreaterThan(0f));
                float deadline = Time.realtimeSinceStartup + 1.2f;
                while (guardian.Health.CurrentHealth == before && Time.realtimeSinceStartup < deadline)
                    yield return new WaitForFixedUpdate();
                Assert.That(projectile.HasImpacted, Is.True, "The saved projectile must collide with the saved guardian body.");
                Assert.That(guardian.Health.CurrentHealth, Is.EqualTo(before - 1));
                Assert.That(inventory.Count, Is.Zero);
                yield return new WaitForSeconds(.4f);
                Assert.That(guardian.Health.CurrentHealth, Is.EqualTo(before - 1), "Multiple contacts cannot repeat damage.");
                Set(guardian, "remaining", 100f);
            }
            Assert.That(guardian.Health.CurrentHealth, Is.EqualTo(initialHealth - 2));
        }

        [UnityTest]
        public IEnumerator RootWarningCanBeDodgedWithTheProductionJumpAndMovement()
        {
            PlaceOnArenaFloor(guardian.transform.position.x - 7f);
            guardian.BeginFight(); Set(guardian, "remaining", 0f);
            yield return null;
            Assert.That(guardian.CurrentAttack, Is.EqualTo(GuardianAttack.RootStrike));
            yield return new WaitForSeconds(.8f);
            yield return MoveFor(.95f, 1f, true);
            Assert.That(player.IsDead, Is.False, "A normal jump with forward movement must clear the warned root.");
            Assert.That(player.transform.position.x, Is.GreaterThan(guardian.transform.position.x - 5f));
        }

        [UnityTest]
        public IEnumerator BranchSlamLeavesEnoughTimeToRetreatOutsideItsNearZone()
        {
            PlaceOnArenaFloor(guardian.transform.position.x - 4.2f);
            Set(guardian, "attackIndex", 1);
            guardian.BeginFight(); Set(guardian, "remaining", 0f);
            yield return null;
            Assert.That(guardian.CurrentAttack, Is.EqualTo(GuardianAttack.BranchSlam));
            yield return MoveFor(1.65f, -1f, false);
            Assert.That(player.IsDead, Is.False);
            Assert.That(player.transform.position.x, Is.LessThan(guardian.transform.position.x - 6f));
        }

        [UnityTest]
        public IEnumerator PredictableLeafStreamCanBeJumpedUsingNormalControllerPhysics()
        {
            PlaceOnArenaFloor(guardian.transform.position.x - 7f);
            Set(guardian, "attackIndex", 2);
            guardian.BeginFight(); Set(guardian, "remaining", 0f);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (guardian.State != GuardianState.Attacking && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(guardian.CurrentAttack, Is.EqualTo(GuardianAttack.LeafStorm));
            Assert.That(guardian.State, Is.EqualTo(GuardianState.Attacking));
            // Jump before the leading leaf is inside the player's horizontal collision radius.
            yield return new WaitForSeconds(.5f);
            yield return MoveFor(.92f, 1f, true);
            Assert.That(player.IsDead, Is.False, "Jumping toward a readable leaf stream must clear its three spaced leaves.");
            Assert.That((int)Get(guardian, "wavesSent"), Is.EqualTo(3));
        }

        private void PlaceOnArenaFloor(float x)
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(x, .56f);
            player.transform.position = body.position;
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        private IEnumerator MoveFor(float seconds, float direction, bool jump)
        {
            var controller = player.GetComponent<PlayerController>();
            float finish = Time.time + seconds;
            bool first = true;
            while (Time.time < finish)
            {
                Set(controller, "moveInput", direction);
                if (first && jump) Set(controller, "jumpRequested", true);
                first = false;
                typeof(PlayerController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
                yield return new WaitForFixedUpdate();
                Assert.That(player.IsDead, Is.False);
            }
            Set(controller, "moveInput", 0f);
        }

        private static object Get(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
