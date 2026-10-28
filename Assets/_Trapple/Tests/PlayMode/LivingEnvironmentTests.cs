using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Trapple.Hub;
using Trapple.Hub.LivingEnvironment;
using Trapple.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Trapple.Tests
{
    public sealed class LivingEnvironmentTests
    {
        private PlayerController player;
        private AppiGroundShadow shadow;
        private LivingEnvironmentController environment;
        private string save;
        private bool hadSave;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            hadSave = PlayerPrefs.HasKey(PreviewSaveService.Key);
            save = PlayerPrefs.GetString(PreviewSaveService.Key, "");
            if (HubSceneFlowController.Instance != null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            Time.timeScale = 1;
            yield return null;
            yield return SceneManager.LoadSceneAsync(HubSceneFlowController.HubScenePath);
            yield return new WaitForSeconds(.35f);
            player = Object.FindAnyObjectByType<PlayerController>();
            shadow = Object.FindAnyObjectByType<AppiGroundShadow>();
            environment = Object.FindAnyObjectByType<LivingEnvironmentController>();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (HubSceneFlowController.Instance != null) Object.Destroy(HubSceneFlowController.Instance.gameObject);
            if (hadSave) PlayerPrefs.SetString(PreviewSaveService.Key, save); else PlayerPrefs.DeleteKey(PreviewSaveService.Key);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InstalledEffectsPreserveGroundAndIndependentMaterials()
        {
            Assert.That(environment, Is.Not.Null);
            Assert.That(player.IsGrounded, Is.True);
            var plants = Object.FindObjectsByType<EnvironmentWindController>();
            Assert.That(plants.Length, Is.EqualTo(13));
            Material shared = plants[0].GetComponent<SpriteRenderer>().sharedMaterial;
            var offsets = new HashSet<float>();
            foreach (var plant in plants)
            {
                var renderer = plant.GetComponent<SpriteRenderer>();
                Assert.That(renderer.sharedMaterial, Is.SameAs(shared));
                Assert.That(plant.GetComponent<Collider2D>(), Is.Null);
                var properties = new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
                offsets.Add(properties.GetVector("_Wind").w);
                if (plant.name == "Apple tree frame") Assert.That(properties.GetFloat("_FixedBase"), Is.GreaterThan(.6f));
            }
            Assert.That(offsets.Count, Is.GreaterThan(8));
            var scenery = Object.FindObjectsByType<SpriteRenderer>().Single(item => item.name == "Apple Meadow scenery");
            Vector3 position = scenery.transform.position;
            Vector3 size = scenery.transform.localScale;
            yield return new WaitForSeconds(.3f);
            Assert.That(scenery.transform.position, Is.EqualTo(position));
            Assert.That(scenery.transform.localScale, Is.EqualTo(size));
            Assert.That(Object.FindObjectsByType<HubAmbience>().Count(item => item.enabled), Is.Zero);
        }

        [UnityTest]
        public IEnumerator ShadowFollowsWalkingAndFadesDuringRealJumpThenRecovers()
        {
            var body = player.GetComponent<Rigidbody2D>();
            var collider = player.GetComponent<Collider2D>();
            shadow.Refresh();
            Assert.That(shadow.Presentation.enabled, Is.True);
            float groundY = shadow.transform.position.y;
            float groundAlpha = shadow.Presentation.color.a;
            body.position = new Vector2(2, body.position.y);
            yield return new WaitForFixedUpdate(); yield return null;
            shadow.Refresh();
            Assert.That(shadow.transform.position.x, Is.EqualTo(collider.bounds.center.x).Within(.03f));
            Assert.That(shadow.transform.position.y, Is.EqualTo(groundY).Within(.02f));
            body.linearVelocity = new Vector2(0, 9);
            yield return new WaitForSeconds(.22f);
            shadow.Refresh();
            Assert.That(shadow.Height, Is.GreaterThan(.6f));
            Assert.That(shadow.transform.position.y, Is.EqualTo(groundY).Within(.02f));
            Assert.That(shadow.Presentation.color.a, Is.LessThan(groundAlpha));
            float deadline = Time.realtimeSinceStartup + 4;
            while (!player.IsGrounded && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            yield return null;
            shadow.Refresh();
            Assert.That(player.IsGrounded, Is.True);
            Assert.That(shadow.Height, Is.LessThan(.04f));
            Assert.That(shadow.Presentation.color.a, Is.EqualTo(groundAlpha).Within(.015f));
        }

        [UnityTest]
        public IEnumerator ShadowSelectsRaisedPlatformIgnoresTriggerAndClipsAtLedge()
        {
            var platform = new GameObject("Shadow test platform", typeof(BoxCollider2D));
            var trigger = new GameObject("Shadow test trigger", typeof(BoxCollider2D));
            try
            {
                platform.layer = trigger.layer = 6;
                var support = platform.GetComponent<BoxCollider2D>(); support.size = new Vector2(1, .3f);
                platform.transform.position = new Vector2(0, 1.5f);
                trigger.transform.position = new Vector2(0, 2.3f); trigger.GetComponent<BoxCollider2D>().isTrigger = true;
                var body = player.GetComponent<Rigidbody2D>(); body.gravityScale = 0; body.linearVelocity = Vector2.zero;
                body.position = new Vector2(.45f, 3);
                yield return new WaitForFixedUpdate(); yield return null;
                Physics2D.SyncTransforms(); shadow.Refresh();
                Assert.That(shadow.Surface, Is.SameAs(support));
                Assert.That(shadow.transform.position.y, Is.EqualTo(1.675f).Within(.02f));
                Assert.That(shadow.Presentation.bounds.max.x, Is.LessThanOrEqualTo(.51f));
                body.position = new Vector2(3, 3);
                yield return new WaitForFixedUpdate(); yield return null;
                Physics2D.SyncTransforms(); shadow.Refresh();
                Assert.That(shadow.Surface, Is.Not.SameAs(support));
                Assert.That(shadow.transform.position.y, Is.LessThan(.05f));
                body.position = new Vector2(40, 3);
                yield return new WaitForFixedUpdate(); yield return null;
                shadow.Refresh(); Assert.That(shadow.Presentation.enabled, Is.False, "Hide over empty space.");
            }
            finally { Object.Destroy(platform); Object.Destroy(trigger); }
        }

        [UnityTest]
        public IEnumerator HiddenOrCinematicAppiDoesNotLeaveAnOrphanShadow()
        {
            var hero = player.GetComponent<SpriteRenderer>();
            hero.enabled = false; shadow.Refresh(); Assert.That(shadow.Presentation.enabled, Is.False);
            hero.enabled = true; player.GetComponent<Rigidbody2D>().simulated = false;
            shadow.Refresh(); Assert.That(shadow.Presentation.enabled, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PoolReusesObjectsAndQualityToggleRestoresEffects()
        {
            var emitters = Object.FindObjectsByType<AmbientParticleController>();
            Assert.That(emitters.Length, Is.EqualTo(5));
            int capacity = emitters.Sum(item => item.PoolSize);
            Assert.That(capacity, Is.EqualTo(16));
            int children = emitters.Sum(item => item.transform.childCount);
            yield return new WaitForSeconds(9);
            Assert.That(emitters.Sum(item => item.ActiveCount), Is.GreaterThan(0));
            Assert.That(emitters.Sum(item => item.transform.childCount), Is.EqualTo(children));
            environment.SetEffectsEnabled(false);
            yield return null;
            Assert.That(emitters.Sum(item => item.ActiveCount), Is.Zero);
            Assert.That(shadow.Presentation.enabled, Is.False);
            foreach (var wind in Object.FindObjectsByType<EnvironmentWindController>())
            {
                var properties = new MaterialPropertyBlock(); wind.GetComponent<SpriteRenderer>().GetPropertyBlock(properties);
                Assert.That(properties.GetVector("_Wind").y, Is.Zero);
            }
            environment.SetEffectsEnabled(true);
            yield return null;
            Assert.That(shadow.Presentation.enabled, Is.True);
            Assert.That(emitters.All(item => item.enabled), Is.True);
            Assert.That(emitters.Sum(item => item.transform.childCount), Is.EqualTo(children));
        }

        [UnityTest]
        public IEnumerator VisualMotionLeavesAllColliderTransformsUntouchedAndPauses()
        {
            var colliders = Object.FindObjectsByType<Collider2D>().Where(item => item.attachedRigidbody == null).ToArray();
            var positions = colliders.Select(item => item.transform.position).ToArray();
            var scales = colliders.Select(item => item.transform.localScale).ToArray();
            var cloud = Object.FindObjectsByType<ParallaxLayer>()[0];
            Vector3 before = cloud.transform.position;
            yield return new WaitForSeconds(.5f);
            Assert.That(Vector3.Distance(before, cloud.transform.position), Is.GreaterThan(.005f));
            Time.timeScale = 0;
            // Stop camera settling before checking the independent cloud drift.
            Camera.main.GetComponent<HubCameraController>().enabled = false;
            yield return null;
            before = cloud.transform.position;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(cloud.transform.position, Is.EqualTo(before));
            for (int i = 0; i < colliders.Length; i++)
            {
                Assert.That(colliders[i].transform.position, Is.EqualTo(positions[i]));
                Assert.That(colliders[i].transform.localScale, Is.EqualTo(scales[i]));
            }
        }

        [UnityTest]
        public IEnumerator CaptureEnvironmentInPlayMode()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("A graphics device is required for visual QA.");
            Directory.CreateDirectory("Builds/EnvironmentReview");
            yield return new WaitForSeconds(6);
            Capture("hub-living");
            environment.SetEffectsEnabled(false); yield return null;
            Capture("hub-effects-disabled");
            environment.SetEffectsEnabled(true); yield return null;
            var body = player.GetComponent<Rigidbody2D>(); body.linearVelocity = new Vector2(0, 9);
            yield return new WaitForSeconds(.25f);
            Capture("hub-jump-shadow");
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var target = new RenderTexture(1600, 900, 24);
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
                File.WriteAllBytes("Builds/EnvironmentReview/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous; RenderTexture.active = active;
                target.Release(); Object.Destroy(target); Object.Destroy(texture);
            }
        }
    }
}
