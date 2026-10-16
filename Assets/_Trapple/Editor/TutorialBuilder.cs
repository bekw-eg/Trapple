using System;
using System.IO;
using Trapple.Level;
using Trapple.Player;
using Trapple.Traps;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Editor
{
    // All scene/prefab serialization is performed by Unity, never by handwritten YAML.
    public static class TutorialBuilder
    {
        public const string ScenePath = "Assets/_Trapple/Scenes/Levels/Tutorial.unity";
        private const string Root = "Assets/_Trapple/";
        private const int GroundLayer = 6;

        [MenuItem("Trapple/Rebuild Tutorial (replaces generated assets)")]
        public static void BuildTutorial()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            foreach (string folder in new[] { "Art/Prototype", "Physics", "Prefabs/Player", "Prefabs/Traps", "Scenes/Levels" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            ConfigureProject();

            Sprite square = CreateSprite("Square", (x, y) => Color.white);
            Sprite character = CreateSprite("Player", (x, y) =>
            {
                bool eye = (x > 38 && x < 47 && y > 35 && y < 46);
                return eye ? new Color(0.08f, 0.12f, 0.2f) : Color.white;
            });
            Sprite spikes = CreateSprite("Spikes", (x, y) =>
                y < 62 - Mathf.Abs(x % 21 - 10) * 6 ? Color.white : Color.clear);
            Sprite apple = CreateSprite("Apple", (x, y) =>
            {
                if (x >= 30 && x <= 34 && y > 46 && y < 62)
                    return new Color(0.35f, 0.18f, 0.08f);
                if ((new Vector2(x - 21, y - 31).sqrMagnitude < 330f) ||
                    (new Vector2(x - 42, y - 31).sqrMagnitude < 330f))
                    return new Color(0.94f, 0.18f, 0.21f);
                if (x > 34 && y > 51 && y < 58 && x < 51)
                    return new Color(0.3f, 0.72f, 0.25f);
                return Color.clear;
            });

            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Root + "Physics/Player.physicsMaterial2D");
            if (material == null)
            {
                material = new PhysicsMaterial2D("Player") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(material, Root + "Physics/Player.physicsMaterial2D");
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject playerPrefab = CreatePlayer(character, material);
            GameObject spikePrefab = CreateSpikes(spikes);
            GameObject platformPrefab = CreateFallingPlatform(square);
            GameObject hiddenPrefab = CreateHiddenTrap(spikePrefab);

            var player = InstantiatePrefab(playerPrefab, new Vector2(0f, 1f));
            player.name = "Player";
            MakeGround("Start", square, new Vector2(3f, -0.5f), new Vector2(14f, 1f)); // -4 .. 10
            MakeGround("After bridge", square, new Vector2(22f, -0.5f), new Vector2(16f, 1f)); // 14 .. 30
            MakeGround("Apple island", square, new Vector2(39f, -0.5f), new Vector2(10f, 1f)); // 34 .. 44
            MakeGround("Step", square, new Vector2(3.5f, 0.4f), new Vector2(1.4f, 0.8f));
            MakeGround("Jump platform", square, new Vector2(29.4f, 0.6f), new Vector2(2.6f, 0.5f));
            MakeGround("Gap platform", square, new Vector2(32f, 1.1f), new Vector2(1.8f, 0.5f));
            InstantiatePrefab(spikePrefab, new Vector2(7.5f, 0.35f)).name = "Start Spikes";
            InstantiatePrefab(platformPrefab, new Vector2(12f, -0.2f));
            InstantiatePrefab(hiddenPrefab, new Vector2(21f, 0.8f));
            InstantiatePrefab(spikePrefab, new Vector2(25.5f, 0.35f)).name = "Later Spikes";

            var goal = MakeVisual("Apple", apple, new Vector2(40f, 0.8f), Vector2.one, Color.white);
            goal.AddComponent<TutorialGoal>();
            goal.GetComponent<CircleCollider2D>().radius = 0.42f;
            goal.GetComponent<CircleCollider2D>().isTrigger = true;

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.17f, 0.25f);
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);
            SetObject(cameraObject.AddComponent<TutorialCamera>(), "target", player.transform);

            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath))
                throw new InvalidOperationException("Could not save Tutorial scene.");
            // This is the first scene in the new project. Preserve any future team scenes on rebuild.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == ScenePath))
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("TRAPPLE: Tutorial and four reusable prefabs saved by Unity " + Application.unityVersion);
        }

        private static void ConfigureProject()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            UnityEditor.VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "Trapple Team";
            PlayerSettings.productName = "Trapple";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePlayer(Sprite sprite, PhysicsMaterial2D material)
        {
            var obj = MakeVisual("Player", sprite, Vector2.zero, Vector2.one, new Color(0.35f, 0.8f, 0.95f));
            var body = obj.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = obj.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.75f, 0.95f);
            collider.sharedMaterial = material;
            SetObject(obj.AddComponent<PlayerController>(), "characterRenderer", obj.GetComponent<SpriteRenderer>());
            obj.AddComponent<PlayerDeath>();
            return SavePrefab(obj, "Player/Player");
        }

        private static GameObject CreateSpikes(Sprite sprite)
        {
            var obj = MakeVisual("SpikeTrap", sprite, Vector2.zero, new Vector2(1.6f, 0.7f), new Color(0.97f, 0.35f, 0.3f));
            obj.AddComponent<SpikeTrap>();
            var collider = obj.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.85f);
            collider.isTrigger = true;
            return SavePrefab(obj, "Traps/SpikeTrap");
        }

        private static GameObject CreateFallingPlatform(Sprite sprite)
        {
            var obj = MakeVisual("FallingPlatform", sprite, Vector2.zero, new Vector2(3.8f, 0.4f), new Color(0.92f, 0.65f, 0.24f));
            obj.layer = GroundLayer;
            obj.AddComponent<FallingPlatform>();
            obj.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            return SavePrefab(obj, "Traps/FallingPlatform");
        }

        private static GameObject CreateHiddenTrap(GameObject spikePrefab)
        {
            var obj = new GameObject("HiddenTrap", typeof(BoxCollider2D));
            var trigger = obj.GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3f, 1.6f);
            var spikes = (GameObject)PrefabUtility.InstantiatePrefab(spikePrefab);
            spikes.transform.SetParent(obj.transform, false);
            spikes.transform.localPosition = new Vector3(0.7f, -1.25f, 0f);
            spikes.SetActive(false);
            SetObject(obj.AddComponent<HiddenTrap>(), "spikes", spikes.GetComponent<SpikeTrap>());
            return SavePrefab(obj, "Traps/HiddenTrap");
        }

        private static GameObject SavePrefab(GameObject obj, string relativePath)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(obj, Root + "Prefabs/" + relativePath + ".prefab");
            UnityEngine.Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Vector2 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            return instance;
        }

        private static GameObject MakeVisual(string name, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return obj;
        }

        private static void MakeGround(string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var obj = MakeVisual(name, sprite, position, size, new Color(0.3f, 0.55f, 0.4f));
            obj.layer = GroundLayer;
            obj.AddComponent<BoxCollider2D>();
        }

        private static Sprite CreateSprite(string name, Func<int, int, Color> pixel)
        {
            string path = Root + "Art/Prototype/" + name + ".png";
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    texture.SetPixel(x, y, pixel(x, y));
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/Windows/Trapple.exe",
                BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        }
    }
}
