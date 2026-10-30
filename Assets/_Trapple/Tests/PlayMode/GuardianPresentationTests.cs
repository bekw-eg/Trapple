using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Player;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Trapple.Tests
{
    /// <summary>Uses the saved artwork and prefabs so transparent sprite padding cannot hide scale regressions.</summary>
    public sealed class GuardianPresentationTests
    {
        private PlayerDeath player;
        private ForestEncounterHUD hud;
        private StickProjectile projectile;
        private GameObject impactSurface;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator LoadAuthoredForest()
        {
            Time.timeScale = 1;
            ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance == null) new GameObject("Guardian presentation test systems", typeof(GameManager));
            GameManager.Instance.LoadScene(ForestGoal.ScenePath);
            yield return null; yield return null;
            player = Object.FindAnyObjectByType<PlayerDeath>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            hud = Object.FindAnyObjectByType<ForestEncounterHUD>();
            Assert.That(hud, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator LeaveForest()
        {
            Time.timeScale = 1;
            if (projectile != null) Object.Destroy(projectile.gameObject);
            if (impactSurface != null) Object.Destroy(impactSurface);
            ForestGuardianEncounter.ClearCheckpoint();
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            yield return null;
        }

        [Test]
        public void AuthoredGroundBranchesHaveReadableArtworkWithoutAnamorphicFlattening()
        {
            var pickups = Object.FindObjectsByType<StickPickup>();
            Assert.That(pickups.Length, Is.InRange(7, 10));
            foreach (var pickup in pickups)
            {
                var artwork = pickup.GetComponentInChildren<SpriteRenderer>();
                Assert.That(artwork, Is.Not.Null, pickup.name);
                Assert.That(artwork.sprite, Is.Not.Null, pickup.name);
                Vector2 size = ArtworkSize(artwork);
                Assert.That(size.x, Is.InRange(1.15f, 1.5f), pickup.name + " width");
                Assert.That(size.y, Is.InRange(.45f, .8f), pickup.name + " height");
                Assert.That(pickup.GetComponent<Collider2D>().isTrigger, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator AuthoredProjectileStaysPlayerSizedAcrossFlightImpactAndReuseInBothDirections()
        {
            var throwing = player.GetComponent<StickThrowController>();
            var prefab = (StickProjectile)typeof(StickThrowController).GetField("projectilePrefab", Private).GetValue(throwing);
            Assert.That(prefab, Is.Not.Null, "The saved controller must reference the production projectile prefab.");
            projectile = Object.Instantiate(prefab);
            projectile.gameObject.SetActive(false);
            var artwork = projectile.GetComponentInChildren<SpriteRenderer>(true);
            Assert.That(artwork, Is.Not.Null);
            Assert.That(artwork.sprite, Is.Not.Null, "A sprite-free stand-in would miss the reported giant projectile bug.");
            Assert.That(artwork.transform, Is.Not.SameAs(projectile.transform), "Physics root and visual size are independent.");
            impactSurface = new GameObject("Presentation impact surface", typeof(BoxCollider2D));
            impactSurface.transform.position = new Vector3(-200, -100, 0);
            int released = 0;
            foreach (int direction in new[] { 1, -1, 1 })
            {
                // Relaunch this exact object, as the production pool does, after its impact animation.
                int previousRelease = released;
                projectile.Launch(player.transform, new Vector2(-200, 5), new Vector2(direction * 12, 2), .65f, 1,
                    value => { Assert.That(value, Is.SameAs(projectile)); released++; });
                Assert.That(projectile.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(ArtworkSize(artwork).x, Is.EqualTo(1.25f).Within(.02f));
                Assert.That(ArtworkSize(artwork).y, Is.EqualTo(.65f).Within(.02f));
                Assert.That(projectile.GetComponent<BoxCollider2D>().size.x, Is.InRange(.8f, 1.15f));
                Assert.That(projectile.Velocity.x * direction, Is.GreaterThan(0));
                for (int frame = 0; frame < 8; frame++)
                {
                    yield return new WaitForFixedUpdate();
                    Assert.That(artwork.bounds.size.x, Is.LessThan(1.6f), "Rotation cannot make a twig as large as the guardian.");
                    Assert.That(artwork.bounds.size.y, Is.LessThan(1.6f));
                    Assert.That(projectile.transform.localScale, Is.EqualTo(Vector3.one));
                }
                Assert.That(projectile.TryHit(impactSurface.GetComponent<Collider2D>()), Is.True);
                Assert.That(projectile.TryHit(impactSurface.GetComponent<Collider2D>()), Is.False);
                float deadline = Time.realtimeSinceStartup + .6f;
                while (projectile.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(projectile.transform.localScale, Is.EqualTo(Vector3.one), "Impact affects the visual, never the physics root.");
                    Assert.That(artwork.bounds.size.x, Is.LessThan(1.7f));
                    Assert.That(artwork.bounds.size.y, Is.LessThan(1.7f));
                    yield return null;
                }
                Assert.That(projectile.gameObject.activeSelf, Is.False);
                Assert.That(released, Is.EqualTo(previousRelease + 1));
            }
        }

        [TestCase(934, 524)]
        [TestCase(1280, 720)]
        public void ForestLabelsRemainReadableAndSeparateAtGameWindowSizes(int width, int height)
        {
            var canvas = hud.GetComponent<Canvas>();
            var scaler = hud.GetComponent<CanvasScaler>();
            var route = hud.transform.Find("World route/Route label").GetComponent<TMP_Text>();
            var boss = hud.transform.Find("Boss combat/Boss name").GetComponent<TMP_Text>();
            var timer = hud.transform.Find("Boss combat/Survival countdown").GetComponent<TMP_Text>();
            var counter = hud.transform.Find("Stick inventory").GetComponent<TMP_Text>();
            float scale = CanvasScale(scaler, new Vector2(width, height));
            foreach (var label in new[] { route, boss, timer, counter })
            {
                float capitals = label.fontSize * label.font.faceInfo.capLine / label.font.faceInfo.pointSize * scale;
                Assert.That(capitals, Is.GreaterThanOrEqualTo(14), label.name + " visible capital pixels at " + width + "×" + height);
                Assert.That(label.enableAutoSizing, Is.False, "Readability must not silently shrink to fit.");
                string content = label == route ? "DECEPTIVE FOREST  /  100%" : label == counter ? "x 10" : label.text;
                Assert.That(label.GetPreferredValues(content).x, Is.LessThanOrEqualTo(label.rectTransform.rect.width + 2),
                    label.name + " has enough authored width for the largest normal value.");
            }
            Vector2 viewport = new Vector2(width, height);
            Rect inventory = LayoutRect(counter.rectTransform, canvas, scaler, viewport);
            foreach (var label in new[] { route, boss, timer })
                Assert.That(inventory.Overlaps(LayoutRect(label.rectTransform, canvas, scaler, viewport)), Is.False,
                    "The stick counter must not overlap " + label.name + " at " + width + "×" + height);
        }

        private static Vector2 ArtworkSize(SpriteRenderer artwork)
        {
            Vector3 size = Vector3.Scale(artwork.sprite.bounds.size, artwork.transform.lossyScale);
            return new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        private static float CanvasScale(CanvasScaler scaler, Vector2 viewport)
        {
            float x = viewport.x / scaler.referenceResolution.x, y = viewport.y / scaler.referenceResolution.y;
            if (scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand) return Mathf.Min(x, y);
            if (scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink) return Mathf.Max(x, y);
            return Mathf.Pow(2, Mathf.Lerp(Mathf.Log(x, 2), Mathf.Log(y, 2), scaler.matchWidthOrHeight));
        }

        // Evaluate the actual authored anchor/size chain at the requested viewport without changing the user's GameView.
        private static Rect LayoutRect(RectTransform target, Canvas canvas, CanvasScaler scaler, Vector2 viewport)
        {
            var chain = new List<RectTransform>();
            for (var current = target; current != null && current != canvas.transform; current = current.parent as RectTransform)
                chain.Add(current);
            float scale = CanvasScale(scaler, viewport);
            var bounds = new Rect(Vector2.zero, viewport / scale);
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                var rect = chain[i];
                Vector2 size = Vector2.Scale(bounds.size, rect.anchorMax - rect.anchorMin) + rect.sizeDelta;
                Vector2 anchor = rect.anchorMin + Vector2.Scale(rect.anchorMax - rect.anchorMin, rect.pivot);
                Vector2 origin = bounds.min + Vector2.Scale(bounds.size, anchor) + rect.anchoredPosition - Vector2.Scale(size, rect.pivot);
                bounds = new Rect(origin, size);
            }
            return bounds;
        }
    }
}
