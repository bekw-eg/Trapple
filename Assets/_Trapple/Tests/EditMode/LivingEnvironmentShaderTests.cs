using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Trapple.Tests
{
    public sealed class LivingEnvironmentShaderTests
    {
        [Test]
        public void SavedMaterialsUseSupportedBuiltInShadersAndParticlePrefabsHaveNoCollision()
        {
            foreach (string name in new[] { "FoliageWind", "LakeShimmer", "SoftSprites", "ContactShadow" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Trapple/Art/Hub/Environment/" + name + ".mat");
                Assert.That(material, Is.Not.Null);
                Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False);
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) Assert.That(material.shader.isSupported, Is.True);
            }
            foreach (string name in new[] { "LeafParticle", "PollenParticle", "SmokeParticle" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Trapple/Prefabs/Hub/Environment/" + name + ".prefab");
                Assert.That(prefab.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
                Assert.That(prefab.GetComponentsInChildren<Collider2D>(), Is.Empty);
                Assert.That(prefab.GetComponentsInChildren<Rigidbody2D>(), Is.Empty);
            }
        }

        [Test]
        public void WindChangesCanopyPixelsWhileTrunkPixelsStayIdentical()
        {
            RequireGraphics();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Resources/AppleMeadow/Tree.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Trapple/Art/Hub/Environment/FoliageWind.mat");
            var properties = new MaterialPropertyBlock();
            properties.SetVector("_SpriteUV", new Vector4(0, 0, 1, 1));
            properties.SetFloat("_FixedBase", .62f);
            properties.SetVector("_Wind", new Vector4(0, .02f, 5, 0));
            Color32[] first = Render(sprite, material, properties);
            properties.SetVector("_Wind", new Vector4(0, .02f, 5, 2.4f));
            Color32[] second = Render(sprite, material, properties);
            int changes = 0, trunkChanges = 0;
            for (int i = 0; i < first.Length; i++)
            {
                if (i / 512 < 256) { if (!second[i].Equals(first[i])) trunkChanges++; }
                else if (!second[i].Equals(first[i])) changes++;
            }
            Assert.That(trunkChanges, Is.Zero, "The fixed tree trunk moved.");
            Assert.That(changes, Is.GreaterThan(50), "Wind must visibly animate the canopy even on an undeformed sprite mesh.");
        }

        [Test]
        public void WaterShimmerChangesOnlyTheMeasuredLakeInterior()
        {
            RequireGraphics();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Art/Hub/AppleMeadow/SceneryPlate.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Trapple/Art/Hub/Environment/LakeShimmer.mat");
            var properties = new MaterialPropertyBlock();
            properties.SetVector("_Shimmer", Vector4.zero);
            Color32[] first = Render(sprite, material, properties);
            properties.SetVector("_Shimmer", new Vector4(.06f, 0, 0, 0));
            Color32[] second = Render(sprite, material, properties);
            Vector4 region = material.GetVector("_WaterRegion");
            int changes = 0;
            float halfHeight = sprite.bounds.size.y * .5f + .02f;
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i].Equals(second[i])) continue;
                float x = (((i % 512 + .5f) / 512f - .5f) * halfHeight * 2) / sprite.bounds.size.x + .5f;
                float y = (((i / 512 + .5f) / 512f - .5f) * halfHeight * 2) / sprite.bounds.size.y + .5f;
                Assert.That(x, Is.InRange(region.x - .003f, region.x + region.z + .003f));
                Assert.That(y, Is.InRange(region.y - .003f, region.y + region.w + .003f));
                changes++;
            }
            Assert.That(changes, Is.GreaterThan(5));
        }

        private static void RequireGraphics()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Run without -nographics for shader pixel verification.");
        }
        private static Color32[] Render(Sprite sprite, Material material, MaterialPropertyBlock properties)
        {
            var actor = new GameObject("Environment shader test", typeof(SpriteRenderer));
            var cameraObject = new GameObject("Environment shader test camera", typeof(Camera));
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var target = new RenderTexture(512, 512, 24);
            RenderTexture previous = RenderTexture.active;
            try
            {
                actor.transform.position = new Vector3(1000, 1000, 0);
                var renderer = actor.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.SetPropertyBlock(properties);
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
                camera.transform.position = new Vector3(1000, 1000, -10);
                camera.orthographic = true; camera.orthographicSize = sprite.bounds.size.y * .5f + .02f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f, .12f, .15f);
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply();
                return texture.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous; target.Release();
                Object.DestroyImmediate(actor); Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
            }
        }
    }
}
