using System;
using System.IO;
using System.Linq;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    [InitializeOnLoad]
    public static class WorldMapBuilder
    {
        public const string ArtPath = "Assets/Art/WorldMap/WorldMapBoard.png";
        public const string PrefabPath = "Assets/_Trapple/Prefabs/Hub/WorldMapBoard.prefab";
        private const string Request = "Temp/world-map-request.txt";
        private const float PixelsPerUnit = 100;
        private const float Width = 9.4f;
        private static double next;

        static WorldMapBuilder()
        {
            EditorApplication.update += Tick;
            ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Results());
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            next = EditorApplication.timeSinceStartup + 1;
            string command;
            try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
            catch (IOException) { return; } // A local writer may still be finishing its request.
            try
            {
                if (command == "tests")
                {
                    SessionState.SetBool("Trapple.WorldMapTests", true);
                    ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter
                    {
                        testMode = TestMode.PlayMode,
                        testNames = new[]
                        {
                            "Trapple.Tests.WorldMapTests",
                            "Trapple.Tests.HubTests.PlayerSpawnsGroundedAndCameraFollows",
                            "Trapple.Tests.HubTests.PromptInteractionModalAndControlRestoration",
                            "Trapple.Tests.HubTests.RapidReopenAndDisableNeverStrandControls"
                        }
                    }));
                }
                else if (command == "status")
                    File.WriteAllText("Temp/world-map-result.txt", string.Join("\n",
                        Enumerable.Range(0, SceneManager.sceneCount).Select(i =>
                        {
                            var scene = SceneManager.GetSceneAt(i);
                            return scene.path + " dirty=" + scene.isDirty;
                        })));
                else Apply();
            }
            catch (Exception exception)
            {
                File.WriteAllText("Temp/world-map-result.txt", "FAILED\n" + exception);
                Debug.LogException(exception);
            }
        }

        [MenuItem("Trapple/Hub/Install World Map")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before installing the map.");
            BuildPrefab();
            var active = SceneManager.GetActiveScene();
            foreach (string path in new[] { HubSceneFlowController.HubScenePath, TrappleUIManager.ScenePath })
            {
                var scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    if (scene.isDirty) throw new InvalidOperationException("Save the hub's unsaved edits first: " + path);
                    InstallInScene(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            // These retired prefabs have no remaining scene references after migration.
            for (int i = 0; i < 3; i++) AssetDatabase.DeleteAsset("Assets/_Trapple/Prefabs/Hub/Portal" + i + ".prefab");
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/world-map-result.txt", "APPLIED: one world map in Hub and TrappleUI; four polygon hotspots; portals removed. Other scenes unchanged.");
        }

        public static WorldMapInteractable CreateMap(Scene scene)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) BuildPrefab();
            return ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene))
                .GetComponent<WorldMapInteractable>();
        }

        internal static void InstallInScene(Scene scene)
        {
            var targets = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HubInteractable>(true)).ToArray();
            foreach (var target in targets.Where(value => value.Screen == HubScreen.Worlds))
                Object.DestroyImmediate(target.gameObject);
            foreach (var root in scene.GetRootGameObjects().Where(value =>
                value.name == "Portal aura" || value.name == "Portal label")) Object.DestroyImmediate(root);
            var map = CreateMap(scene);
            var interaction = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HubInteractionController>(true)).Single();
            var serialized = new SerializedObject(interaction);
            var array = serialized.FindProperty("interactables");
            var remaining = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HubInteractable>(true)).ToArray();
            array.arraySize = remaining.Length;
            for (int i = 0; i < remaining.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = remaining[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (remaining.Count(value => value.Screen == HubScreen.Worlds) != 1 || map.GetComponentsInChildren<PolygonCollider2D>().Length != 4)
                throw new InvalidOperationException("World map migration did not produce one map and four hotspots.");
        }

        public static void BuildPrefab()
        {
            if (!File.Exists(ArtPath)) throw new FileNotFoundException("The supplied map artwork is required; no replacement is generated.", ArtPath);
            AssetDatabase.ImportAsset(ArtPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, 15f / 568f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            var root = new GameObject("World Map Board");
            try
            {
                var renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
                renderer.sortingOrder = 6; // Above the scenery, behind Appi (order 9).
                root.transform.position = new Vector3(6.7f, .02f, 0);
                root.transform.localScale = Vector3.one * (Width / renderer.sprite.bounds.size.x);
                var proximity = root.AddComponent<BoxCollider2D>();
                proximity.isTrigger = true;
                proximity.offset = new Vector2(0, .65f);
                proximity.size = new Vector2(5.1f, 1.8f);
                var map = root.AddComponent<WorldMapInteractable>();
                var serialized = new SerializedObject(map);
                serialized.FindProperty("screen").enumValueIndex = (int)HubScreen.Worlds;
                serialized.FindProperty("prompt").stringValue = "Открыть карту миров";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                map.Configure(proximity, new[]
                {
                    Zone(root, "meadow", new[] { P(196,253),P(231,218),P(264,185),P(304,169),P(337,181),P(365,163),P(400,185),P(434,236),P(418,271),P(388,283),P(389,306),P(355,320),P(323,302),P(273,291),P(236,274),P(202,269) }),
                    Zone(root, "forest", new[] { P(446,236),P(470,210),P(502,201),P(516,174),P(553,172),P(575,192),P(605,195),P(635,244),P(630,276),P(600,311),P(560,328),P(505,313),P(473,292),P(458,268) }),
                    Zone(root, "factory", new[] { P(155,389),P(173,348),P(208,316),P(244,324),P(266,310),P(292,318),P(306,350),P(329,352),P(337,384),P(371,376),P(386,409),P(354,443),P(314,465),P(258,461),P(212,442),P(173,425) }),
                    Zone(root, "lava", new[] { P(448,403),P(467,376),P(500,368),P(516,342),P(546,337),P(573,357),P(587,373),P(616,380),P(625,410),P(614,443),P(581,465),P(539,474),P(500,462),P(467,446) })
                });
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        // Source coordinates use the original 778 x 568 image, origin at its top left.
        private static Vector2 P(float x, float y) => new Vector2((x - 389) / PixelsPerUnit, (553 - y) / PixelsPerUnit);
        private static WorldMapInteractable.WorldZone Zone(GameObject root, string world, Vector2[] outline)
        {
            var child = new GameObject("World zone " + world);
            child.transform.SetParent(root.transform, false);
            var collider = child.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;
            collider.SetPath(0, outline);
            return new WorldMapInteractable.WorldZone { worldId = world, area = collider };
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool("Trapple.WorldMapTests", false)) return;
                SessionState.SetBool("Trapple.WorldMapTests", false);
                TestRunnerApi.SaveResultToFile(result, "Logs/world-map-play-tests.xml");
                File.WriteAllText("Temp/world-map-tests-result.txt", result.TestStatus + " " + result.PassCount + " passed, " + result.FailCount + " failed");
            }
        }
    }
}
