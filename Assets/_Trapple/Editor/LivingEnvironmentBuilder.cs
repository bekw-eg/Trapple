using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trapple.Hub;
using Trapple.Hub.LivingEnvironment;
using Trapple.Player;
using Trapple.Workshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    /// <summary>Additive installation into the authored Hub; never invokes a scene/gameplay rebuild.</summary>
    public static class LivingEnvironmentBuilder
    {
        public const string PrefabPath = "Assets/_Trapple/Prefabs/Hub/Environment/LivingEnvironment.prefab";
        public const string MaterialRoot = "Assets/_Trapple/Art/Hub/Environment/";
        private const string ParticleRoot = "Assets/_Trapple/Prefabs/Hub/Environment/";
        private const string Name = "Living Environment";
        private const string Review = "Builds/EnvironmentReview/";

        [MenuItem("Trapple/Hub/Install Living Environment")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            // Never replace an unsaved in-memory scene, including a scene unrelated to Hub.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before installation.");
            if (GraphicsSettings.currentRenderPipeline != null || QualitySettings.renderPipeline != null)
                throw new InvalidOperationException("This installation was authored for the existing Built-in renderer.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(HubSceneFlowController.HubScenePath, OpenSceneMode.Single);
                Directory.CreateDirectory(Review);
                if (!File.Exists(Review + "SceneAudit.txt")) Audit(scene);
                var original = Components<Collider2D>(scene).ToDictionary(item => item,
                    item => EditorJsonUtility.ToJson(item) + EditorJsonUtility.ToJson(item.transform));
                var players = Components<PlayerController>(scene);
                if (players.Length != 1) throw new InvalidOperationException("Expected the existing Hub player.");
                string playerBefore = EditorJsonUtility.ToJson(players[0]);
                var stations = Components<HubInteractable>(scene).ToDictionary(item => item.transform,
                    item => EditorJsonUtility.ToJson(item.transform));
                InstallAssets();
                Dress(scene, players[0]);
                Physics2D.SyncTransforms();
                Validate(scene);
                foreach (var pair in original)
                    if (pair.Key == null || pair.Value != EditorJsonUtility.ToJson(pair.Key) + EditorJsonUtility.ToJson(pair.Key.transform))
                        throw new InvalidOperationException("Installation changed a collider or its transform.");
                foreach (var pair in stations)
                    if (pair.Value != EditorJsonUtility.ToJson(pair.Key)) throw new InvalidOperationException("Installation moved a station.");
                if (playerBefore != EditorJsonUtility.ToJson(players[0])) throw new InvalidOperationException("PlayerController settings changed.");
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Hub could not be saved.");
                AssetDatabase.SaveAssets();
                Debug.Log("LIVING ENVIRONMENT INSTALL PASS: Unity saved Hub, materials and prefabs; colliders, stations and player settings preserved.");
            }
            finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        // Batch entry point also checks that reinstalling does not duplicate objects or references.
        public static void InstallAndVerify()
        {
            Install();
            string first = File.ReadAllText(HubSceneFlowController.HubScenePath);
            Install();
            if (first != File.ReadAllText(HubSceneFlowController.HubScenePath))
                throw new InvalidOperationException("Reinstallation changed the saved Hub.");
            File.WriteAllText(Review + "Integration.txt", "PASS: installed twice; identical scene serialization; no missing references; collider and interaction invariants intact.\n");
        }

        private static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        private static Sprite Sprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()
            ?? throw new InvalidOperationException("Missing sprite: " + path);
        private static void Set(Object item, string field, Object value)
        {
            var serialized = new SerializedObject(item);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Record(item);
        }
        private static void Array(Object item, string field, IEnumerable<Object> values)
        {
            var serialized = new SerializedObject(item);
            var property = serialized.FindProperty(field);
            var items = values.ToArray(); property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); Record(item);
        }
        private static void Record(Object item)
        {
            EditorUtility.SetDirty(item);
            if (PrefabUtility.IsPartOfPrefabInstance(item)) PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }

        private static Material Material(string file, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Shader failed: " + shaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + file + ".mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialRoot + file + ".mat"); }
            return material;
        }

        private static void InstallAssets()
        {
            Directory.CreateDirectory(MaterialRoot); Directory.CreateDirectory(ParticleRoot); AssetDatabase.Refresh();
            Material("FoliageWind", "Trapple/Sprites/Environment Wind");
            Material("LakeShimmer", "Trapple/Sprites/Environment Water Shimmer");
            // Shared premultiplied sprite material for all contact shadows, clouds, particles and halos.
            Material("SoftSprites", "Sprites/Default");
            Particle("LeafParticle", Sprite("Assets/_Trapple/Art/Hub/Leaf.png"), new Vector2(.14f, .07f), Color.white, 7);
            Particle("PollenParticle", Sprite("Assets/_Trapple/Art/Hub/Glow.png"), Vector2.one * .065f, new Color(1, .91f, .55f), 7);
            Particle("SmokeParticle", Sprite("Assets/_Trapple/Art/Hub/Glow.png"), Vector2.one * .45f, new Color(.76f, .79f, .78f), 7);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            var root = new GameObject(Name);
            try
            {
                var master = root.AddComponent<LivingEnvironmentController>();
                var soft = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "SoftSprites.mat");
                var glow = Sprite("Assets/_Trapple/Art/Hub/Glow.png");
                for (int i = 0; i < 3; i++)
                {
                    var cloud = Visual("Independent cloud " + (i + 1), root.transform,
                        Sprite("Assets/_Trapple/Art/Hub/Cloud.png"), new Vector2(-16 + i * 15, 8.2f + i % 2 * .6f),
                        new Vector2(4.4f + i * .6f, .8f), -39, new Color(1, 1, 1, .4f), soft);
                    cloud.gameObject.AddComponent<ParallaxLayer>();
                }
                foreach (float x in new[] { -10.4f, -5.6f, 11.4f, 12.9f })
                {
                    var grass = Visual("Independent meadow grass", root.transform,
                        Sprite("Assets/_Trapple/Resources/AppleMeadow/Grass.png"), new Vector2(x, .14f),
                        new Vector2(.65f, .32f), 4, Color.white, AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "FoliageWind.mat"));
                    grass.gameObject.AddComponent<EnvironmentWindController>().Configure(.007f, .18f, x * 2.37f);
                }
                var shadow = Visual("Appi ground shadow", root.transform, glow, Vector2.zero, new Vector2(.85f, .14f), 8,
                    new Color(.16f, .24f, .18f, .3f), soft);
                shadow.gameObject.AddComponent<AppiGroundShadow>();
                Array(master, "foliage", root.GetComponentsInChildren<EnvironmentWindController>());
                Array(master, "cloudLayers", root.GetComponentsInChildren<ParallaxLayer>());
                Set(master, "playerShadow", shadow.GetComponent<AppiGroundShadow>());
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Particle(string name, Sprite sprite, Vector2 size, Color color, int order)
        {
            string path = ParticleRoot + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var renderer = Visual(name, null, sprite, Vector2.zero, size, order, color,
                AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "SoftSprites.mat"));
            try { PrefabUtility.SaveAsPrefabAsset(renderer.gameObject, path); }
            finally { Object.DestroyImmediate(renderer.gameObject); }
        }

        private static SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size,
            int order, Color color, Material material)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            if (parent != null) renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.color = color; renderer.sortingOrder = order;
            renderer.transform.position = position;
            renderer.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            return renderer;
        }

        private static void Dress(Scene scene, PlayerController player)
        {
            var environment = scene.GetRootGameObjects().SingleOrDefault(item => item.name == Name);
            if (environment == null) environment = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            environment.name = Name;
            Record(environment);
            var master = environment.GetComponent<LivingEnvironmentController>();
            var camera = Components<HubCameraController>(scene).Single().transform;
            var art = scene.GetRootGameObjects().Single(item => item.name == "Apple Meadow lobby art");
            var foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "FoliageWind.mat");
            var soft = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "SoftSprites.mat");
            foreach (var renderer in art.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.name != "Meadow flowers" && renderer.name != "Low foreground foliage" && renderer.name != "Apple tree frame") continue;
                if (renderer.GetComponent<Collider2D>() != null) throw new InvalidOperationException("Foliage unexpectedly owns collision.");
                var legacy = renderer.GetComponent<HubAmbience>();
                if (legacy != null) { legacy.enabled = false; Record(legacy); }
                renderer.sharedMaterial = foliageMaterial; Record(renderer);
                var wind = renderer.GetComponent<EnvironmentWindController>() ?? renderer.gameObject.AddComponent<EnvironmentWindController>();
                bool tree = renderer.name == "Apple tree frame";
                wind.Configure(tree ? .0018f : .004f, tree ? .62f : .22f, renderer.transform.position.x * 2.37f);
                Record(wind);
            }
            var scenery = art.GetComponentsInChildren<SpriteRenderer>().Single(item => item.name == "Apple Meadow scenery");
            scenery.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "LakeShimmer.mat"); Record(scenery);
            var water = scenery.GetComponent<EnvironmentWaterShimmer>() ?? scenery.gameObject.AddComponent<EnvironmentWaterShimmer>();
            var shadow = environment.GetComponentInChildren<AppiGroundShadow>();
            shadow.Configure(player.GetComponent<Collider2D>(), player.GetComponent<SpriteRenderer>()); Record(shadow);
            foreach (var layer in environment.GetComponentsInChildren<ParallaxLayer>()) { layer.Configure(camera); Record(layer); }

            // Registration follows the real cottage canvas. Lamp, gear and smoke are painted into it;
            // no transform animation is attached to the facade, door, sign or gameplay stations.
            var cottage = Components<WorkshopDoorAnimator>(scene).Single().Presentation.transform.parent.GetComponent<SpriteRenderer>();
            AddHalo(environment.transform, "Workshop lantern warmth", CanvasPoint(cottage, new Vector2(.45f, .68f)), new Vector2(1.25f, 1.45f), soft);
            AddHalo(environment.transform, "Workshop window warmth", CanvasPoint(cottage, new Vector2(.585f, .39f)), Vector2.one * 1.1f, soft);
            Emitter(environment.transform, "Rare orchard leaves", new Vector2(-11.4f, 5.1f), AmbientParticleController.Kind.Leaf,
                "LeafParticle", 3, new Vector2(3.4f, 1), new Vector2(4, 8), new Vector2(7, 9), new Vector2(.12f, -.45f), .5f, 731);
            Emitter(environment.transform, "Rare right leaves", new Vector2(12.6f, 4.2f), AmbientParticleController.Kind.Leaf,
                "LeafParticle", 2, new Vector2(1.6f, .8f), new Vector2(6, 11), new Vector2(6, 8), new Vector2(-.08f, -.4f), .4f, 829);
            Emitter(environment.transform, "Left pollen", new Vector2(-10, .7f), AmbientParticleController.Kind.Pollen,
                "PollenParticle", 4, new Vector2(3, .8f), new Vector2(2, 4), new Vector2(5, 7), new Vector2(.04f, .06f), .24f, 531);
            Emitter(environment.transform, "Right pollen", new Vector2(11.2f, .7f), AmbientParticleController.Kind.Pollen,
                "PollenParticle", 4, new Vector2(2, .7f), new Vector2(2.5f, 4), new Vector2(5, 7), new Vector2(-.03f, .05f), .24f, 419);
            Emitter(environment.transform, "Chimney smoke", CanvasPoint(cottage, new Vector2(.30f, .02f)), AmbientParticleController.Kind.Smoke,
                "SmokeParticle", 3, new Vector2(.16f, .05f), new Vector2(2.5f, 4), new Vector2(4, 6), new Vector2(.09f, .19f), .12f, 617);
            Physics2D.SyncTransforms();
            AddContact(environment.transform, "Cottage contact", cottage.transform.position.x, 5.3f, soft);
            AddContact(environment.transform, "Tree contact", -11.5f, 3.1f, soft);
            foreach (var station in Components<HubInteractable>(scene).Where(item => item.Screen != HubScreen.Workshop))
                AddContact(environment.transform, station.name + " contact", station.transform.position.x, station is WorldMapInteractable ? 2.9f : 1.9f, soft);
            Array(master, "foliage", Components<EnvironmentWindController>(scene));
            Array(master, "cloudLayers", environment.GetComponentsInChildren<ParallaxLayer>());
            Array(master, "emitters", environment.GetComponentsInChildren<AmbientParticleController>());
            Array(master, "lights", environment.GetComponentsInChildren<LightFlicker2D>());
            Array(master, "staticShadows", environment.GetComponentsInChildren<SpriteRenderer>().Where(item => item.name.EndsWith(" contact", StringComparison.Ordinal)));
            Set(master, "water", water); Set(master, "playerShadow", shadow);
        }

        // Normalized source image coordinates, top-left origin; preserves authored facade scale.
        private static Vector2 CanvasPoint(SpriteRenderer renderer, Vector2 uv) => renderer.transform.TransformPoint(
            new Vector3((uv.x - .5f) * renderer.sprite.bounds.size.x, (.5f - uv.y) * renderer.sprite.bounds.size.y));
        private static void AddHalo(Transform root, string name, Vector2 position, Vector2 size, Material soft)
        {
            if (root.Find(name) != null) return;
            var halo = Visual(name, root, Sprite("Assets/_Trapple/Art/Hub/Glow.png"), position, size, 7, new Color(1, .66f, .21f, .13f), soft);
            halo.gameObject.AddComponent<LightFlicker2D>();
        }
        private static void Emitter(Transform root, string name, Vector2 position, AmbientParticleController.Kind kind,
            string prefab, int capacity, Vector2 region, Vector2 interval, Vector2 lifetime, Vector2 velocity, float alpha, int seed)
        {
            if (root.Find(name) != null) return;
            var emitter = new GameObject(name).AddComponent<AmbientParticleController>();
            emitter.transform.SetParent(root, false); emitter.transform.position = position;
            emitter.Configure(kind, AssetDatabase.LoadAssetAtPath<GameObject>(ParticleRoot + prefab + ".prefab").GetComponent<SpriteRenderer>(),
                capacity, region, interval, lifetime, velocity, alpha, seed);
        }
        private static void AddContact(Transform root, string name, float x, float width, Material soft)
        {
            if (root.Find(name) != null) return;
            var hit = Physics2D.Raycast(new Vector2(x, .5f), Vector2.down, 2, 1 << 6);
            if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < .65f) throw new InvalidOperationException("No support under " + name);
            Visual(name, root, Sprite("Assets/_Trapple/Art/Hub/Glow.png"), hit.point + hit.normal * .025f,
                new Vector2(width, .3f), 2, new Color(.17f, .25f, .18f, .28f), soft);
        }

        public static void Validate(Scene scene)
        {
            if (Components<LivingEnvironmentController>(scene).Length != 1 || Components<EnvironmentWindController>(scene).Length != 13 ||
                Components<AppiGroundShadow>(scene).Length != 1 || Components<AmbientParticleController>(scene).Length != 5)
                throw new InvalidOperationException("Unexpected environment installation counts: masters=" + Components<LivingEnvironmentController>(scene).Length +
                    " foliage=" + Components<EnvironmentWindController>(scene).Length + " shadows=" + Components<AppiGroundShadow>(scene).Length +
                    " emitters=" + Components<AmbientParticleController>(scene).Length);
            foreach (var component in Components<Component>(scene))
            {
                if (component == null) throw new InvalidOperationException("Missing script in Hub.");
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None)
                        throw new InvalidOperationException("Missing reference: " + component.name + "." + property.propertyPath);
            }
            var shadow = Components<AppiGroundShadow>(scene).Single();
            var shadowData = new SerializedObject(shadow);
            if (shadowData.FindProperty("character").objectReferenceValue == null || shadowData.FindProperty("characterArt").objectReferenceValue == null)
                throw new InvalidOperationException("Appi shadow dependencies missing.");
            foreach (var layer in Components<ParallaxLayer>(scene))
                if (new SerializedObject(layer).FindProperty("view").objectReferenceValue == null) throw new InvalidOperationException("Cloud camera missing.");
            foreach (var renderer in Components<SpriteRenderer>(scene).Where(item => item.gameObject.activeInHierarchy && item.enabled))
                if (renderer.sprite == null || renderer.sharedMaterial == null || ShaderUtil.ShaderHasError(renderer.sharedMaterial.shader))
                    throw new InvalidOperationException("Invalid sprite/material: " + renderer.name);
        }

        private static void Audit(Scene scene)
        {
            var lines = new List<string> { "Unity " + Application.unityVersion, "Pipeline: " + (GraphicsSettings.currentRenderPipeline == null ? "Built-in" : GraphicsSettings.currentRenderPipeline.name),
                "Scene: " + scene.path, "Sorting layers: " + string.Join(", ", SortingLayer.layers.Select(item => item.name)) };
            foreach (var renderer in Components<SpriteRenderer>(scene).Where(item => item.gameObject.activeInHierarchy && item.enabled))
                lines.Add(renderer.name + " | " + AssetDatabase.GetAssetPath(renderer.sprite) + " | bounds " + renderer.bounds +
                    " | order " + renderer.sortingOrder + " | " + renderer.sharedMaterial.shader.name + " | vertices " + renderer.sprite.vertices.Length);
            foreach (var camera in Components<Camera>(scene)) lines.Add("Camera: ortho=" + camera.orthographic + " size=" + camera.orthographicSize + " pos=" + camera.transform.position);
            File.WriteAllLines(Review + "SceneAudit.txt", lines);
        }
    }
}
