using System.Collections;
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
    public sealed class GuardianPhysicsTests
    {
        private ForestGuardianController guardian;
        private PlayerDeath player;
        private Sprite leaf;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator LoadForest()
        {
            Time.timeScale = 1f;
            ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Guardian physics test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(ForestGoal.ScenePath);
            yield return null; yield return null;
            guardian = Object.FindAnyObjectByType<ForestGuardianController>();
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            leaf = (Sprite)typeof(ForestGuardianController).GetField("leafSprite", Private).GetValue(guardian);
            guardian.BeginFight();
            typeof(ForestGuardianController).GetField("remaining", Private).SetValue(guardian, 100f);
        }

        [UnityTearDown]
        public IEnumerator LeaveForest()
        {
            Time.timeScale = 1f;
            guardian?.CancelFight();
            ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        [UnityTest]
        public IEnumerator StickPassesThroughChildHazardWithoutDamagingBossUntilItsBodyIsHit()
        {
            float bossX = guardian.transform.position.x;
            player.transform.position = new Vector3(bossX - 5.7f, .55f, 0f);
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            player.GetComponent<SpriteRenderer>().flipX = false;
            var hazard = guardian.GetComponentsInChildren<ForestGuardianHazard>(true)[0];
            hazard.Launch(guardian, leaf, new Vector2(bossX - 4f, .95f), new Vector2(1f, 1.3f),
                Vector2.zero, 2f, GuardianAttack.LeafStorm);
            var inventory = player.GetComponent<StickInventory>(); inventory.Restore(1);
            var throwing = player.GetComponent<StickThrowController>();
            int hp = guardian.Health.CurrentHealth;
            Physics2D.SyncTransforms();
            Assert.That(throwing.TryThrow(), Is.True);
            var shot = throwing.LastProjectile;
            float deadline = Time.realtimeSinceStartup + 1f;
            while (shot.transform.position.x < bossX - 3.25f && !shot.HasImpacted && Time.realtimeSinceStartup < deadline)
                yield return new WaitForFixedUpdate();
            Assert.That(shot.transform.position.x, Is.GreaterThan(bossX - 3.4f), "The projectile must physically cross the pooled child hazard.");
            Assert.That(shot.HasImpacted, Is.False);
            Assert.That(guardian.Health.CurrentHealth, Is.EqualTo(hp), "A child hazard cannot forward damage to ancestor BossHealth.");
            while (!shot.HasImpacted && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(shot.HasImpacted, Is.True);
            Assert.That(guardian.Health.CurrentHealth, Is.EqualTo(hp - 1));
        }

        [UnityTest]
        public IEnumerator LiveLeafColliderCallsRealPlayerDeathAndCancelsCombat()
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(guardian.transform.position.x - 7f, .55f);
            player.transform.position = body.position;
            body.simulated = true;
            var hazard = guardian.GetComponentsInChildren<ForestGuardianHazard>(true)[0];
            hazard.Launch(guardian, leaf, body.position + new Vector2(2f, .12f), new Vector2(.7f, .4f),
                Vector2.left * 6f, 1.5f, GuardianAttack.LeafStorm);
            Physics2D.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 1f;
            while (!player.IsDead && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert.That(player.IsDead, Is.True);
            yield return null;
            Assert.That(guardian.IsFighting, Is.False);
            Assert.That(guardian.ActiveHazardCount, Is.Zero);
        }
    }
}
