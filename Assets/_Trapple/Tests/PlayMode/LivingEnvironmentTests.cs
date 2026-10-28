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
        public IEnumerator GroundShadowIsVisibleAroundAppiAtGameplayScale()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Graphics required.");
            environment.SetEffectsEnabled(false);
            player.GetComponent<PlayerSpriteAnimator>().enabled = false;
            var camera = Camera.main;
            camera.GetComponent<HubCameraController>().enabled = false;
            shadow.enabled = true; shadow.Refresh();
            Color32[] withShadow = ReadPixels(camera, 1600, 900);
            shadow.Presentation.enabled = false;
            Color32[] withoutShadow = ReadPixels(camera, 1600, 900);
            int visible = 0, strongest = 0;
            for (int i = 0; i < withShadow.Length; i++)
            {
                int darkening = (withoutShadow[i].r + withoutShadow[i].g + withoutShadow[i].b -
                    withShadow[i].r - withShadow[i].g - withShadow[i].b) / 3;
                if (darkening >= 10) visible++;
                strongest = Mathf.Max(strongest, darkening);
            }
            Debug.Log("CONTACT SHADOW VISIBILITY: exposed pixels=" + visible + ", max darkening=" + strongest + "/255 at 1600x900.");
            Assert.That(visible, Is.GreaterThan(80), "The shadow must remain visible after Appi covers its center.");
            Assert.That(strongest, Is.GreaterThan(20), "A technically enabled but imperceptible shadow is insufficient.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredWindMovesCanopyByVisiblePixelsAtGameplayScale()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) Assert.Ignore("Graphics required.");
            var tree = Object.FindObjectsByType<EnvironmentWindController>().Single(item => item.name == "Apple tree frame").GetComponent<SpriteRenderer>();
            var actor = new GameObject("Wind visibility render", typeof(SpriteRenderer));
            var cameraObject = new GameObject("Wind visibility camera", typeof(Camera));
            try
            {
                actor.transform.position = new Vector3(1000, 1000, 0); actor.transform.localScale = tree.transform.lossyScale;
                var renderer = actor.GetComponent<SpriteRenderer>(); renderer.sprite = tree.sprite; renderer.sharedMaterial = tree.sharedMaterial;
                var properties = new MaterialPropertyBlock(); tree.GetPropertyBlock(properties);
                Vector4 wind = properties.GetVector("_Wind"); wind.x = 0; wind.w = 0;
                properties.SetVector("_Wind", wind); renderer.SetPropertyBlock(properties);
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
                camera.transform.position = new Vector3(1000, 1000, -10); camera.orthographic = true;
                // A square crop with exactly the same pixels per world unit as the actual 900p Hub.
                camera.orthographicSize = Camera.main.orthographicSize * 512f / 900f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f, .12f, .15f);
                Color32[] first = ReadPixels(camera, 512, 512);
                wind.w = Mathf.PI; properties.SetVector("_Wind", wind); renderer.SetPropertyBlock(properties);
                Color32[] second = ReadPixels(camera, 512, 512);
                Bounds bounds = renderer.bounds;
                int left = Mathf.RoundToInt(camera.WorldToViewportPoint(bounds.min + Vector3.right * bounds.size.x * .2f).x * 512);
                int right = Mathf.RoundToInt(camera.WorldToViewportPoint(bounds.min + Vector3.right * bounds.size.x * .8f).x * 512);
                int bottom = Mathf.RoundToInt(camera.WorldToViewportPoint(bounds.min + Vector3.up * bounds.size.y * .77f).y * 512);
                int top = Mathf.RoundToInt(camera.WorldToViewportPoint(bounds.min + Vector3.up * bounds.size.y * .92f).y * 512);
                int visibleRows = 0, maxShift = 0;
                for (int y = Mathf.Max(0, bottom); y <= Mathf.Min(511, top); y++)
                {
                    long bestError = long.MaxValue; int bestShift = 0;
                    // Rendered-image correlation measures actual movement, not just a configured float.
                    for (int shift = -16; shift <= 16; shift++)
                    {
                        long error = 0;
                        for (int x = Mathf.Max(16, left); x <= Mathf.Min(495, right); x++)
                        {
                            Color32 a = first[y * 512 + x], b = second[y * 512 + x + shift];
                            error += Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                        }
                        if (error < bestError || error == bestError && Mathf.Abs(shift) < Mathf.Abs(bestShift))
                        { bestError = error; bestShift = shift; }
                    }
                    if (Mathf.Abs(bestShift) >= 2) visibleRows++;
                    maxShift = Mathf.Max(maxShift, Mathf.Abs(bestShift));
                }
                Debug.Log("WIND VISIBILITY: canopy rows moving >=2 pixels=" + visibleRows + ", max shift=" + maxShift + " at 900p Hub scale.");
                Assert.That(visibleRows, Is.GreaterThanOrEqualTo(12), "Authored wind should be visible at gameplay scale, not only at exaggerated test strength.");
            }
            finally { Object.Destroy(actor); Object.Destroy(cameraObject); }
            yield return null;
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

        private static Color32[] ReadPixels(Camera camera, int width, int height)
        {
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var target = new RenderTexture(width, height, 24);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                return texture.GetPixels32();
            }
            finally
            {
                camera.targetTexture = previous; RenderTexture.active = active;
                target.Release(); Object.Destroy(target); Object.Destroy(texture);
            }
        }
    }
}
