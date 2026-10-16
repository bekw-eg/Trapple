using System;
using System.IO;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.Player;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Editor
{
    public static class BossPrototypeBuilder
    {
        private const string Root = "Assets/_Trapple/";
        private const string BossPrefabPath = Root + "Prefabs/Bosses/AncientTree.prefab";
        public const string ProfilePath = Root + "Settings/BossPrototype.asset";
        private const int GroundLayer = 6;

        [MenuItem("Trapple/Rebuild Boss Prototype (replaces menu, arena and boss)")]
        public static void BuildPrototype()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { "Scenes/Menu", "Scenes/BossArenas", "Prefabs/Bosses", "Settings" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Prototype/Square.png");
            if (square == null) throw new InvalidOperationException("Base project's Square sprite is missing.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Ancient Tree");
            var trunk = Visual("Trunk", root.transform, square, new Vector2(6.2f, 2.4f), new Vector2(2.2f, 4.8f), new Color(0.4f, 0.26f, 0.16f));
            Visual("Crown", root.transform, square, new Vector2(6.2f, 5.2f), new Vector2(5.5f, 2.3f), new Color(0.2f, 0.42f, 0.22f));
            Visual("Left eye", root.transform, square, new Vector2(5.7f, 3.3f), new Vector2(0.25f, 0.3f), new Color(1f, 0.8f, 0.25f));
            Visual("Right eye", root.transform, square, new Vector2(6.7f, 3.3f), new Vector2(0.25f, 0.3f), new Color(1f, 0.8f, 0.25f));
            var warning = Visual("Yellow warning (safe)", root.transform, square, Vector2.zero, new Vector2(2f, 0.18f), new Color(1f, 0.85f, 0.2f));
            warning.sortingOrder = 5;
            warning.enabled = false;
            var roots = Hazard("Roots", root.transform, square, Vector2.zero, new Vector2(1.8f, 2.5f), new Color(0.85f, 0.3f, 0.16f));
            var branch = Hazard("Falling branch", root.transform, square, Vector2.zero, new Vector2(2f, 0.65f), new Color(0.76f, 0.26f, 0.14f));
            var trap = Hazard("Platform danger", root.transform, square, new Vector2(-0.5f, 1.9f), new Vector2(3f, 0.5f), new Color(0.95f, 0.3f, 0.18f));
            root.AddComponent<BossHealth>();
            root.AddComponent<BossPhaseController>();
            var attack = root.AddComponent<BossAttack>();
            SetReference(attack, "warning", warning);
            SetReference(attack, "roots", roots);
            SetReference(attack, "branch", branch);
            SetReference(attack, "platformTrap", trap);
            SetReference(root.AddComponent<BossController>(), "trunk", trunk);
            var bossPrefab = PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera(new Color(0.045f, 0.085f, 0.085f));
            new GameObject("Game Systems", typeof(GameManager));
            SaveScene(GameManager.MenuScene);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera(new Color(0.09f, 0.16f, 0.17f));
            new GameObject("Game Systems", typeof(GameManager));
            Ground("Arena floor", square, new Vector2(-2f, -0.5f), new Vector2(15f, 1f));
            Ground("Left wall", square, new Vector2(-9.7f, 3f), new Vector2(0.4f, 7f));
            Ground("Tree barrier", square, new Vector2(4.8f, 3f), new Vector2(0.4f, 7f));
            Ground("Warning platform", square, new Vector2(-0.5f, 1.5f), new Vector2(3f, 0.35f));
            Ground("Upper platform", square, new Vector2(2.5f, 2.8f), new Vector2(2f, 0.35f));
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Player/Player.prefab");
            if (playerPrefab == null) throw new InvalidOperationException("Base player prefab is missing.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(-7f, 0.7f, 0f);
            var bossObject = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
            SetReference(bossObject.GetComponent<BossAttack>(), "playerTarget", player.transform);
            var leverVisual = Visual("Arena lever — E", null, square, new Vector2(-6f, 0.6f), new Vector2(0.45f, 1.2f), new Color(0.3f, 0.85f, 0.95f));
            Visual("Lever handle", leverVisual.transform, square, new Vector2(-6f, 1.2f), new Vector2(0.9f, 0.2f), new Color(0.3f, 0.85f, 0.95f));
            var stone = Visual("Lever stone", null, square, new Vector2(6.2f, 7f), Vector2.one, new Color(0.7f, 0.76f, 0.71f));
            stone.gameObject.SetActive(false);
            var lever = leverVisual.gameObject.AddComponent<ArenaLever>();
            SetReference(lever, "boss", bossObject.GetComponent<BossHealth>());
            SetReference(lever, "player", player.GetComponent<PlayerDeath>());
            SetReference(lever, "fallingStone", stone.transform);
            SaveScene(GameManager.BossScene);
            CreateBuildProfile();
            AssetDatabase.SaveAssets();
            Debug.Log("TRAPPLE: MainMenu, BossTest and AncientTree prefab saved by Unity " + Application.unityVersion + ". Shared ProjectSettings were not changed.");
        }

        private static void CreateBuildProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null)
            {
                foreach (var platform in BuildProfile.GetInstalledPlatformModules())
                {
                    if (platform.displayName != "Windows") continue;
                    profile = BuildProfile.CreateBuildProfile(platform.platformGuid, "Trapple Boss Prototype", null);
                    string error = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(profile), ProfilePath);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                    break;
                }
                if (profile == null) throw new InvalidOperationException("Windows Build Support is required to create the prototype profile.");
            }
            profile.overrideGlobalScenes = true;
            profile.scenes = new[] { new EditorBuildSettingsScene(GameManager.MenuScene, true),
                new EditorBuildSettingsScene(GameManager.TutorialScene, true),
                new EditorBuildSettingsScene(GameManager.BossScene, true) };
            EditorUtility.SetDirty(profile);
        }

        [MenuItem("Trapple/Build Boss Prototype (Windows)")]
        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new[] { GameManager.MenuScene, GameManager.TutorialScene, GameManager.BossScene },
                "Builds/BossPrototype/Trapple.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Boss prototype build failed: " + report.summary.result);
        }

        private static void SaveScene(string path)
        {
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path))
                throw new InvalidOperationException("Could not save " + path);
        }

        private static void Camera(Color background)
        {
            var cameraObject = new GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 3f, -10f);
            var camera = cameraObject.GetComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.backgroundColor = background;
            camera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
        }

        private static BossHazard Hazard(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var renderer = Visual(name, parent, sprite, position, size, color);
            renderer.sortingOrder = 4;
            var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            var hazard = renderer.gameObject.AddComponent<BossHazard>();
            collider.enabled = false;
            renderer.enabled = false;
            return hazard;
        }

        private static void Ground(string name, Sprite square, Vector2 position, Vector2 size)
        {
            var renderer = Visual(name, null, square, position, size, new Color(0.27f, 0.44f, 0.3f));
            renderer.gameObject.layer = GroundLayer;
            renderer.gameObject.AddComponent<BoxCollider2D>();
        }

        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
