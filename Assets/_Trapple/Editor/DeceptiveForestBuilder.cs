using System;
using System.IO;
using System.Linq;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Player;
using Trapple.Traps;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Trapple.Editor
{
    public static class DeceptiveForestBuilder
    {
        private const string Root = "Assets/_Trapple/";
        private const string Art = Root + "Art/DeceptiveForest/";
        private const string UI = Root + "UI/DeceptiveForest/";
        private const string Prefabs = Root + "Prefabs/DeceptiveForest/";
        public const string ProfilePath = Root + "Settings/DeceptiveForestPreview.asset";

        [MenuItem("Trapple/Deceptive Forest/Open Level 2")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ForestGoal.ScenePath);
        }

        [MenuItem("Trapple/Deceptive Forest/Create Level 2 (only if absent)")]
        public static void Create()
        {
            // Never overwrites a hand-edited saved scene or reusable prefabs.
            if (File.Exists(ForestGoal.ScenePath))
                throw new InvalidOperationException("Level 2 already exists. Open the saved scene; creation will not overwrite it.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in new[] { Path.GetDirectoryName(ForestGoal.ScenePath), Prefabs }) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles(Art, "*.png")) Import(path.Replace('\\', '/'), path.EndsWith("Stick.png"));
            foreach (string path in Directory.GetFiles(UI + "Art", "*.png")) Import(path.Replace('\\', '/'), false);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Deceptive Forest · Level 2 · False Path").transform;
            var environment = Group("Environment", root);
            var terrain = Group("Terrain", root);
            var traps = Group("Traps", root);
            var interactions = Group("Interactables", root);
            var spawn = Group("PlayerSpawn", root); spawn.position = new Vector3(0, .65f, 0);
            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Player/Player.prefab"));
            playerObject.name = "Appi"; playerObject.transform.SetParent(root); playerObject.transform.position = spawn.position;
            playerObject.GetComponent<SpriteRenderer>().sortingOrder = 10;
            var player = playerObject.GetComponent<PlayerController>();
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(ForestCamera));
            cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(root);
            var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6f;
            camera.backgroundColor = new Color(.42f, .61f, .55f);
            cameraObject.transform.position = new Vector3(6.67f, 3.4f, -10);
            Set(cameraObject.GetComponent<ForestCamera>(), "target", player.transform);
            BuildEnvironment(environment, camera.transform);

            Ground("01 · Safe entrance", -4, 9, terrain);
            var falseGround = Platform("02 · False ground", 10.2f, 0, 2.4f, traps, true);
            Set(falseGround.GetComponent<FallingPlatform>(), "fallDelay", .7f);
            SavePrefab(falseGround, "FalseGround");
            Pit("False ground pit", 10.2f, 2.4f, traps);
            Ground("02 · Landing", 11.4f, 18, terrain);

            Platform("03 · Root step", 19.1f, 1.2f, 2.2f, terrain);
            var disappearing = Platform("03 · Fading branch", 22.2f, 2.4f, 2.4f, traps);
            var vanish = disappearing.AddComponent<ForestDisappearingPlatform>();
            Set(vanish, "artwork", disappearing.GetComponentInChildren<SpriteRenderer>());
            Set(vanish, "warningDuration", .85f); SavePrefab(disappearing, "DisappearingBranch");
            Platform("03 · High branch", 25.7f, 3.4f, 3f, terrain);
            Platform("03 · Descent", 29.7f, 2f, 3f, terrain);
            Pit("Vertical route pit", 25, 14f, traps);
            Ground("04 · Stick clearing", 32, 46, terrain);
            var gate = new GameObject("04 · Weak wooden barrier", typeof(BoxCollider2D), typeof(ForestStickTarget));
            gate.transform.SetParent(interactions); gate.transform.position = new Vector3(41.5f, 0, 0);
            gate.GetComponent<BoxCollider2D>().size = new Vector2(.7f, 4.3f);
            gate.GetComponent<BoxCollider2D>().offset = new Vector2(0, 2.15f);
            var boards = Group("Breakable boards", gate.transform);
            for (int i = 0; i < 4; i++) Visual("Wood " + i, Meadow("Crate"), new Vector2(41.5f, .53f + i * 1.02f), new Vector2(.95f, 1.06f), 6, boards);
            var target = gate.GetComponent<ForestStickTarget>(); Set(target, "artwork", boards);
            SavePrefab(gate, "StickBarrier");
            var stickObject = new GameObject("04 · Recoverable stick", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(ForestStick));
            stickObject.transform.SetParent(interactions); stickObject.transform.position = new Vector3(36.5f, .65f, 0);
            var stickArt = Visual("Stick artwork", AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Stick.png"), stickObject.transform.position, new Vector2(1.05f, .7f), 12, stickObject.transform);
            var stickBody = stickObject.GetComponent<Rigidbody2D>(); stickBody.gravityScale = 1.4f;
            stickBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous; stickBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            stickObject.GetComponent<BoxCollider2D>().size = new Vector2(.7f, .24f);
            var stick = stickObject.GetComponent<ForestStick>(); Set(stick, "artwork", stickArt);
            SavePrefab(stickObject, "RecoverableStick"); Set(stick, "player", player);

            var cover = Platform("05 · Hidden pit cover", 47.5f, 0, 3f, traps);
            var hidden = cover.AddComponent<ForestDisappearingPlatform>(); Set(hidden, "warningDuration", .55f);
            Set(hidden, "artwork", cover.GetComponentInChildren<SpriteRenderer>());
            Pit("Apple bait pit", 47.5f, 3f, traps);
            Visual("05 · Apple bait (never a goal)", Meadow("Apple"), new Vector2(47.5f, .9f), new Vector2(.8f, .84f), 7, cover.transform);
            Ground("05 · Safe bank", 49, 55.4f, terrain);
            var final = Platform("06 · Unstable step", 56.6f, .5f, 2.4f, traps, true);
            Set(final.GetComponent<FallingPlatform>(), "fallDelay", .7f);
            Pit("Final gap", 56.9f, 3f, traps);
            Ground("06 · Tree and finish clearing", 58.4f, 80, terrain);
            var tree = CreateTree(traps);
            var trigger = new GameObject("06 · Tree warning area", typeof(BoxCollider2D), typeof(ForestTreeTrigger));
            trigger.transform.SetParent(traps); trigger.transform.position = new Vector3(60.5f, 2f, 0);
            trigger.GetComponent<BoxCollider2D>().size = new Vector2(.8f, 5f); trigger.GetComponent<BoxCollider2D>().isTrigger = true;
            Set(trigger.GetComponent<ForestTreeTrigger>(), "tree", tree);

            var goalObject = new GameObject("Goal · The real apple", typeof(CircleCollider2D), typeof(ForestGoal));
            goalObject.transform.SetParent(root); goalObject.transform.position = new Vector3(76f, 1f, 0);
            goalObject.GetComponent<CircleCollider2D>().isTrigger = true; goalObject.GetComponent<CircleCollider2D>().radius = .7f;
            Visual("Goal apple", Meadow("Apple"), goalObject.transform.position, new Vector2(1.1f, 1.15f), 7, goalObject.transform);
            Visual("Finish flag", Meadow("Flag"), new Vector2(74.6f, .85f), new Vector2(1.6f, 1.7f), 5, root);
            Visual("Entrance sign", Meadow("Sign"), new Vector2(2.2f, .8f), new Vector2(1.1f, 1.6f), 2, environment);
            Visual("Tree warning sign", Meadow("Warning"), new Vector2(59.1f, .65f), new Vector2(.9f, 1.3f), 2, environment);
            foreach (float x in new[] { -4.5f, 80.5f })
            {
                var wall = new GameObject("Level boundary", typeof(BoxCollider2D)); wall.transform.SetParent(terrain);
                wall.transform.position = new Vector3(x, 4, 0); wall.GetComponent<BoxCollider2D>().size = new Vector2(1, 14);
            }
            BuildUI(root, player.GetComponent<PlayerDeath>(), stick, target, tree, goalObject.GetComponent<ForestGoal>());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ForestGoal.ScenePath)) throw new Exception("Scene save failed.");
            RegisterScenes(); DeceptiveForestRework.Apply(); ValidateSavedScene(); AssetDatabase.SaveAssets();
            Debug.Log("FOREST: Level 2 saved and references validated with Unity Editor APIs.");
        }

        private static void BuildEnvironment(Transform parent, Transform camera)
        {
            var far = Group("Far background · parallax", parent);
            var sky = Visual("Forest scenery", AssetDatabase.LoadAssetAtPath<Sprite>(Art + "ForestBackdrop.png"), new Vector2(5, 3.4f), new Vector2(46, 20), -100, far);
            Motion(sky.gameObject, HubAmbience.Motion.Parallax, .9f, .1f, camera);
            var mid = Group("Midground trees", parent);
            // Independently placed tree silhouettes create depth without putting false surfaces in pits.
            for (int i = 0; i < 16; i++)
            {
                float x = -6 + i * 6.5f;
                var distant = Visual("Distant tree " + i, Meadow("Tree"), new Vector2(x, 3.7f), new Vector2(6.6f, 7.4f), -55, mid);
                distant.color = new Color(.6f, .75f, .64f, .13f); Motion(distant.gameObject, HubAmbience.Motion.Parallax, .22f, 0, camera);
            }
            foreach (float x in new[] { -3f, 5f, 14.5f, 34f, 44f, 51.5f, 70.5f, 79f })
            {
                var tree = Visual("Framing tree", Meadow("Tree"), new Vector2(x, 2.85f), new Vector2(5.6f, 5.7f), -12, mid);
                tree.color = new Color(.72f, .88f, .7f); Motion(tree.gameObject, HubAmbience.Motion.Sway, .25f, .55f);
                Visual("Bush", Meadow("Bush"), new Vector2(x + 1.4f, .43f), new Vector2(1.4f, .86f), -5, parent);
                Visual("Flowers", Meadow("Flowers"), new Vector2(x - .5f, .18f), new Vector2(.8f, .4f), 3, parent);
                var grass = Visual("Grass", Meadow("Grass"), new Vector2(x + 2f, .13f), new Vector2(.85f, .38f), 3, parent);
                Motion(grass.gameObject, HubAmbience.Motion.Sway, 1.5f, .9f);
            }
            Visual("Stick stump", Meadow("Stump"), new Vector2(35f, .65f), new Vector2(1.8f, 1.3f), -3, parent);
            for (int i = 0; i < 18; i++)
            {
                var leaf = Visual("Drifting leaf " + i, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Hub/Leaf.png"), new Vector2(i * 4.8f, 6 + i % 3), new Vector2(.12f, .16f), -6, parent);
                Motion(leaf.gameObject, HubAmbience.Motion.Leaf, .6f, .45f + i % 4 * .1f);
            }
        }
        private static ForestFallingTree CreateTree(Transform parent)
        {
            var obj = new GameObject("06 · Falling tree", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpikeTrap), typeof(ForestFallingTree));
            obj.transform.SetParent(parent); obj.transform.position = new Vector3(66, .35f, 0);
            var body = obj.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = obj.GetComponent<BoxCollider2D>(); collider.size = new Vector2(.6f, 5.5f); collider.offset = new Vector2(0, 2.75f); collider.isTrigger = true;
            Set(obj.GetComponent<SpikeTrap>(), "armed", false);
            var pivot = Group("Tree artwork pivot", obj.transform);
            Visual("Tree artwork", Meadow("Tree"), new Vector2(66, 3.1f), new Vector2(5.5f, 5.5f), 6, pivot);
            var tree = obj.GetComponent<ForestFallingTree>(); Set(tree, "artwork", pivot);
            SavePrefab(obj, "FallingTree"); return tree;
        }
        private static void Ground(string name, float left, float right, Transform parent)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D)); obj.layer = 6; obj.transform.SetParent(parent);
            obj.transform.position = new Vector3((left + right) * .5f, -2f, 0);
            obj.GetComponent<BoxCollider2D>().size = new Vector2(right - left, 4);
            var art = Visual("Grass and soil", Meadow("Ground"), obj.transform.position + Vector3.up * .1f, Vector2.one, 0, obj.transform);
            art.transform.localScale = Vector3.one; art.drawMode = SpriteDrawMode.Tiled; art.size = new Vector2(right - left, 4);
        }
        private static GameObject Platform(string name, float x, float top, float width, Transform parent, bool falling = false)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D)); obj.layer = 6; obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(x, top - .225f, 0); obj.GetComponent<BoxCollider2D>().size = new Vector2(width, .45f);
            var art = Visual("Platform artwork", Meadow(top <= 0 ? "Ground" : "Island"), new Vector2(x, top - .28f), new Vector2(width, .85f), 2, obj.transform);
            if (top <= 0)
            {
                art.transform.localScale = Vector3.one; art.drawMode = SpriteDrawMode.Tiled; art.size = new Vector2(width, .8f);
            }
            if (falling)
            {
                obj.AddComponent<Rigidbody2D>(); obj.AddComponent<FallingPlatform>();
                Set(obj.AddComponent<ForestPlatformWarning>(), "artwork", art.transform);
            }
            return obj;
        }
        private static void Pit(string name, float x, float width, Transform parent)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D), typeof(SpikeTrap)); obj.transform.SetParent(parent); obj.transform.position = new Vector3(x, -2.5f, 0);
            var collider = obj.GetComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = new Vector2(width, .65f);
            for (float offset = -width * .5f; offset < width * .5f - .01f; offset += 1.3f)
            {
                float w = Mathf.Min(1.3f, width * .5f - offset);
                Visual("Pit thorns", Meadow("Spikes"), new Vector2(x + offset + w * .5f, -2.5f), new Vector2(w, .65f), 1, obj.transform);
            }
        }
        private static void BuildUI(Transform parent, PlayerDeath player, ForestStick stick, ForestStickTarget target, ForestFallingTree tree, ForestGoal goal)
        {
            string panelPath = UI + "ForestPanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(Root + "UI/Hub/HubPanelSettings.asset"));
                panel.name = "ForestPanelSettings"; AssetDatabase.CreateAsset(panel, panelPath);
            }
            var obj = new GameObject("UI · Forest Toolkit", typeof(UIDocument), typeof(ForestUI)); obj.transform.SetParent(parent);
            var document = obj.GetComponent<UIDocument>(); document.panelSettings = panel; document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UI + "Forest.uxml");
            var controller = obj.GetComponent<ForestUI>();
            Set(controller, "player", player); Set(controller, "stick", stick); Set(controller, "goal", goal);
        }
        public static void RegisterScenes()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ForestGoal.ScenePath)) scenes.Add(new EditorBuildSettingsScene(ForestGoal.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<BuildProfile>(HubBuilder.ProfilePath);
                if (source == null) throw new InvalidOperationException("Existing Hub build profile is required.");
                profile = UnityEngine.Object.Instantiate(source); profile.name = "Deceptive Forest Preview";
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            profile.overrideGlobalScenes = true;
            profile.scenes = new[] { new EditorBuildSettingsScene(ForestGoal.ScenePath, true) }
                .Concat(scenes.Where(s => s.path != ForestGoal.ScenePath)).ToArray();
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        }
        public static void ValidateSavedScene()
        {
            EditorSceneManager.OpenScene(ForestGoal.ScenePath);
            foreach (var obj in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj.gameObject) != 0) throw new Exception("Missing script: " + obj.name);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
                if (renderer.sprite == null) throw new Exception("Missing sprite: " + renderer.name);
            foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>())
            {
                if (component == null || component.GetType().Namespace != "Trapple.DeceptiveForest") continue;
                var serialized = new SerializedObject(component); var iterator = serialized.GetIterator();
                while (iterator.NextVisible(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null &&
                        iterator.name != "concealedHazard")
                        throw new Exception("Unassigned reference: " + component.name + "." + iterator.name);
            }
            if (UnityEngine.Object.FindObjectsByType<PlayerController>().Length != 1 || UnityEngine.Object.FindAnyObjectByType<ForestGoal>() == null)
                throw new Exception("Scene player/goal invalid.");
        }
        public static void ApplyPlatformWarnings()
        {
            EditorSceneManager.OpenScene(ForestGoal.ScenePath);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
                if (renderer.name.StartsWith("Distant tree ")) renderer.color = new Color(.6f, .75f, .64f, .13f);
            foreach (var platform in UnityEngine.Object.FindObjectsByType<FallingPlatform>())
            {
                var warning = platform.GetComponent<ForestPlatformWarning>();
                if (warning == null) warning = platform.gameObject.AddComponent<ForestPlatformWarning>();
                Set(warning, "artwork", platform.GetComponentInChildren<SpriteRenderer>().transform);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }
        public static void PolishAndBuild() { ApplyPlatformWarnings(); BuildWindows(); }
        [MenuItem("Trapple/Deceptive Forest/Build Windows Preview")]
        public static void BuildWindows()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ValidateSavedScene(); RegisterScenes(); Directory.CreateDirectory("Builds/DeceptiveForest");
            var paths = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath).scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(paths, "Builds/DeceptiveForest/Trapple.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Forest build failed.");
        }
        private static void Import(string path, bool point)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
        private static Sprite Meadow(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Resources/AppleMeadow/" + name + ".png");
        private static Transform Group(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj.transform; }
        private static SpriteRenderer Visual(string name, Sprite sprite, Vector2 position, Vector2 size, int order, Transform parent)
        {
            if (sprite == null) throw new Exception("Missing artwork: " + name);
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent); obj.transform.position = position;
            var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            return renderer;
        }
        private static void Motion(GameObject obj, HubAmbience.Motion mode, float amplitude, float speed, Transform camera = null)
        {
            var motion = obj.AddComponent<HubAmbience>(); Set(motion, "motion", (int)mode); Set(motion, "amplitude", amplitude); Set(motion, "speed", speed);
            Set(motion, "phase", obj.transform.position.x * .73f); if (camera != null) Set(motion, "cameraTransform", camera);
        }
        private static void SavePrefab(GameObject obj, string name)
        {
            string path = Prefabs + name + ".prefab";
            if (File.Exists(path)) throw new InvalidOperationException("Prefab exists: " + path);
            PrefabUtility.SaveAsPrefabAssetAndConnect(obj, path, InteractionMode.AutomatedAction);
        }
        private static void Set(UnityEngine.Object obj, string name, object value)
        {
            var serialized = new SerializedObject(obj); var property = serialized.FindProperty(name);
            if (property == null) throw new Exception("Missing serialized field: " + name);
            if (value is UnityEngine.Object reference) property.objectReferenceValue = reference;
            else if (value is float number) property.floatValue = number;
            else if (value is int integer) property.intValue = integer;
            else if (value is bool boolean) property.boolValue = boolean;
            else throw new Exception("Unsupported field: " + name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
