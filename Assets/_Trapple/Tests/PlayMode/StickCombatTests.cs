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
    public sealed class StickCombatTests
    {
        private GameObject actor, template, target, pickupObject;
        private StickInventory inventory;
        private StickThrowController throwing;
        private SpriteRenderer renderer;
        private static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance == null) new GameObject("Stick test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(GameManager.TutorialScene);
            yield return null;
            actor = new GameObject("Stick test player", typeof(Rigidbody2D), typeof(CapsuleCollider2D),
                typeof(SpriteRenderer), typeof(PlayerController), typeof(PlayerDeath), typeof(StickInventory));
            actor.transform.position = new Vector3(-100, 2, 0);
            actor.GetComponent<Rigidbody2D>().simulated = false;
            actor.GetComponent<PlayerController>().enabled = false;
            renderer = actor.GetComponent<SpriteRenderer>();
            inventory = actor.GetComponent<StickInventory>();
            template = new GameObject("Stick test template", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(StickProjectile));
            template.SetActive(false);
            throwing = actor.AddComponent<StickThrowController>();
            Set(throwing, "projectilePrefab", template.GetComponent<StickProjectile>());
            Set(throwing, "cooldown", .05f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            Object.Destroy(actor); Object.Destroy(template); Object.Destroy(target); Object.Destroy(pickupObject);
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        [Test]
        public void CapacityAndCheckpointRestoreClampWithoutDuplicatingAmmunition()
        {
            Assert.That(inventory.TrySpend(), Is.False);
            Assert.That(inventory.TryAdd(50), Is.True);
            Assert.That(inventory.Count, Is.EqualTo(inventory.MaximumCapacity));
            Assert.That(inventory.TryAdd(), Is.False);
            Assert.That(inventory.TrySpend(), Is.True);
            inventory.Restore(4); inventory.Restore(4);
            Assert.That(inventory.Count, Is.EqualTo(4));
            inventory.Restore(-2);
            Assert.That(inventory.Count, Is.Zero);
        }

        [Test]
        public void ACollectedPickupCannotGrantTwiceAndRestoredPickupGrantsNothing()
        {
            pickupObject = new GameObject("Checkpoint stick", typeof(BoxCollider2D), typeof(StickPickup));
            var pickup = pickupObject.GetComponent<StickPickup>();
            Assert.That(pickup.TryCollect(inventory), Is.True);
            Assert.That(pickup.TryCollect(inventory), Is.False);
            Assert.That(inventory.Count, Is.EqualTo(1));
            pickup.MarkCollected();
            Assert.That(pickup.TryCollect(inventory), Is.False);
            Assert.That(inventory.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ThrowsBothDirectionsRejectsCooldownPauseAndEmptyInventory()
        {
            Assert.That(throwing.TryThrow(), Is.False);
            inventory.Restore(2);
            renderer.flipX = false;
            Assert.That(throwing.TryThrow(), Is.True);
            Assert.That(throwing.LastProjectile.Velocity.x, Is.GreaterThan(0));
            Assert.That(throwing.TryThrow(), Is.False);
            yield return new WaitForSeconds(.07f);
            Time.timeScale = 0;
            Assert.That(throwing.TryThrow(), Is.False);
            Time.timeScale = 1;
            renderer.flipX = true;
            Assert.That(throwing.TryThrow(), Is.True);
            Assert.That(throwing.LastProjectile.Velocity.x, Is.LessThan(0));
            Assert.That(inventory.Count, Is.Zero);
            Assert.That(throwing.TryThrow(), Is.False);
        }

        [Test]
        public void OneProjectileCannotDamageBossTwiceThroughDifferentColliders()
        {
            target = new GameObject("Multi collider boss", typeof(BossHealth), typeof(BoxCollider2D));
            var child = new GameObject("Extra collider", typeof(CircleCollider2D)); child.transform.SetParent(target.transform);
            var health = target.GetComponent<BossHealth>(); health.SetVulnerable(true);
            inventory.Restore(1);
            Assert.That(throwing.TryThrow(), Is.True);
            var shot = throwing.LastProjectile;
            Assert.That(shot.TryHit(target.GetComponent<Collider2D>()), Is.True);
            Assert.That(shot.TryHit(child.GetComponent<Collider2D>()), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(99));
            Assert.That(shot.HasImpacted, Is.True);
        }

        [Test]
        public void HeldPuzzleStickHasPriorityOverCombatAmmunition()
        {
            pickupObject = new GameObject("Existing puzzle stick");
            pickupObject.SetActive(false);
            pickupObject.transform.position = actor.transform.position;
            var puzzle = pickupObject.AddComponent<ForestStick>();
            Set(puzzle, "player", actor.GetComponent<PlayerController>());
            pickupObject.SetActive(true);
            Set(throwing, "puzzleSticks", new[] { puzzle });
            inventory.Restore(1);
            Assert.That(puzzle.TryPickup(), Is.True);
            Assert.That(throwing.TryThrow(), Is.False);
            Assert.That(inventory.Count, Is.EqualTo(1));
        }
    }
}
