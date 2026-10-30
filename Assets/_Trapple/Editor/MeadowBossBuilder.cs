using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using Trapple.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    /// <summary>Rebuilds only the authored first-boss clearing, leaving the full tutorial route in place.</summary>
    public static class MeadowBossBuilder
    {
        private const string Root = "Assets/_Trapple/";
        private const string Art = Root + "Art/OrchardWarden/";
        private const string Prefab = Root + "Prefabs/OrchardWarden/OrchardWarden.prefab";
        private const string EncounterName = "Meadow boss encounter";
        public const string TestScene = Root + "Scenes/BossArenas/MeadowBossTest.unity";
        private static Sprite trunk, crown, arm, root, eyes, leaf, square;
        private static TMP_FontAsset font;
        private static Color32[] kitPixels;
        [Serializable] private sealed class Route { public float finishX; }

        [MenuItem("Trapple/Boss progression/Open Orchard Warden test arena")]
        public static void OpenTestArena()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(TestScene);
        }

        [MenuItem("Trapple/Boss progression/Rebuild Orchard Warden survival trial")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(GameManager.TutorialScene); AppendToOpenScene();
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new Exception("Meadow save failed");
            CreateTestScene(); AssetDatabase.SaveAssets();
            Debug.Log("ORCHARD: rebuilt tutorial arena, survival-only rig, HUD, checkpoint and separate test scene.");
        }
        public static void AppendToOpenScene()
        {
            if (SceneManager.GetActiveScene().path != GameManager.TutorialScene) throw new InvalidOperationException("Open Tutorial first.");
            PrepareArt();
            float finish = JsonUtility.FromJson<Route>(File.ReadAllText("Docs/LevelExpansion/meadow-route.json")).finishX;
            if (finish < 100) throw new InvalidOperationException("Authored tutorial route is missing.");
            var previous = GameObject.Find(EncounterName); if (previous != null) Object.DestroyImmediate(previous);
            var goal = Object.FindAnyObjectByType<TutorialGoal>();
            var oldArt = goal.transform.Find("Orchard arena apple"); if (oldArt != null) Object.DestroyImmediate(oldArt.gameObject);
            var player = Object.FindAnyObjectByType<PlayerDeath>();
            var course = Object.FindAnyObjectByType<LevelCourse>();
            var camera = Camera.main;
            foreach (var wall in Object.FindObjectsByType<BoxCollider2D>().Where(c => c.name == "Level boundary" && c.transform.position.x > finish).ToArray()) Object.DestroyImmediate(wall.gameObject);
            var obj = new GameObject(EncounterName); obj.transform.SetParent(course.transform, false);
            var encounter = obj.AddComponent<MeadowBossEncounter>();
            var configuration = AssetDatabase.LoadAssetAtPath<HubConfiguration>(WorldCompletionSetup.ConfigurationPath);
            obj.AddComponent<WorldCompletion>().Configure(configuration);
            configuration.World("meadow").boss = "Orchard Warden"; configuration.World("meadow").completionRequiresBoss = true;
            EditorUtility.SetDirty(configuration);
            float left = finish + 6, right = finish + 30;
            Ground("Orchard clearing", obj.transform, (finish - 3 + right + 6) * .5f, -5f, right + 6 - (finish - 3), 10f);
            Ground("Low root step A", obj.transform, left + 7.5f, .34f, 2.1f, .4f);
            Ground("Low root step B", obj.transform, left + 13.5f, .44f, 1.8f, .4f);
            var gate = Barrier("Living root gate", obj.transform, left - .9f);
            Barrier("Old orchard border", obj.transform, right + .9f);
            var boss = CreateGuardian(obj.transform, new Vector2(right - 4.4f, 0));
            Decorate(obj.transform, left, right);
            Set(encounter, "goal", goal); Set(encounter, "player", player); Set(encounter, "boss", boss);
            Set(encounter, "arenaCamera", camera); Set(encounter, "follow", camera.GetComponent<TutorialCamera>());
            Set(encounter, "entranceBarrier", gate); Set(encounter, "arenaBounds", new Vector2(left, right));
            Set(encounter, "checkpointPosition", new Vector3(left - 2, .65f, 0));
            Set(encounter, "cameraPosition", new Vector3((left + right) * .5f, 3, -10));
            Set(encounter, "cameraSize", 6f); Set(encounter, "survivalDuration", 45f); Set(encounter, "introDuration", 4.5f);
            var audio = obj.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.volume = .25f;
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "Art/ForestGuardian/GuardianAwakens.wav"); Set(encounter, "atmosphere", audio);
            var checkpoint = new GameObject("Orchard checkpoint", typeof(BoxCollider2D), typeof(MeadowArenaCheckpoint));
            checkpoint.transform.SetParent(obj.transform); checkpoint.transform.position = new Vector3(left - 2, 1.5f, 0);
            checkpoint.GetComponent<BoxCollider2D>().isTrigger = true; checkpoint.GetComponent<BoxCollider2D>().size = new Vector2(1.2f, 4);
            Set(checkpoint.GetComponent<MeadowArenaCheckpoint>(), "encounter", encounter);
            Visual("Checkpoint lantern", obj.transform, Resource("Flag"), new Vector2(left - 2.8f, .8f), new Vector2(1.2f, 1.6f), 4);
            goal.transform.position = new Vector3(left + 2, .8f, 0); Set(goal, "encounter", encounter);
            goal.GetComponent<SpriteRenderer>().enabled = false;
            var apple = Visual("Orchard arena apple", goal.transform, Resource("Apple"), Vector2.zero, new Vector2(.7f, .7f), 5); Set(goal, "artwork", apple);
            Set(course, "worldBounds", new Vector2(course.WorldBounds.x, right + 6));
            var data = new SerializedObject(course); var sections = data.FindProperty("sections");
            sections.GetArrayElementAtIndex(sections.arraySize - 1).FindPropertyRelative("endX").floatValue = right + 6; data.ApplyModifiedPropertiesWithoutUndo();
            Set(camera.GetComponent<TutorialCamera>(), "levelBounds", new Vector2(-4, right + 6));
            BuildHUD(encounter, obj.transform); BuildLeaves(encounter, obj.transform, new Vector2(left, right));
            gate.SetActive(false); Physics2D.SyncTransforms(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        private static void PrepareArt()
        {
            Directory.CreateDirectory(Art); Directory.CreateDirectory(Path.GetDirectoryName(Prefab)); AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "OrchardKit.png");
            if (importer == null) throw new FileNotFoundException("Generated OrchardKit.png is required.");
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "OrchardKit.png");
            kitPixels = texture.GetPixels32();
            trunk = Slice(texture, "Torso", new Rect(0, 550, 440, 650));
            crown = Slice(texture, "AppleCrown", new Rect(390, 570, 560, 570));
            arm = Slice(texture, "BranchArm", new Rect(950, 530, 315, 650));
            root = Slice(texture, "RootFoot", new Rect(0, 130, 460, 355));
            eyes = Slice(texture, "Eyes", new Rect(465, 205, 460, 180));
            leaf = Slice(texture, "GoldenLeaf", new Rect(980, 125, 260, 350));
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Prototype/Square.png");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "UI/Storybook/Fonts/Thaleah SDF.asset");
        }
        private static Sprite Slice(Texture2D texture, string name, Rect rect)
        {
            string path = Art + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            float scale = texture.width / 1280f; rect = new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale);
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100 * scale, 0, SpriteMeshType.FullRect);
            IsolateSpriteMesh(sprite, texture.width, rect, 100 * scale, name == "Eyes" ? 2 : 1);
            sprite.name = name;
            if (existing != null) { EditorUtility.CopySerialized(sprite, existing); Object.DestroyImmediate(sprite); EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(sprite, path); return sprite;
        }
        // Build a native Sprite mesh around the intended alpha islands; the source PNG stays untouched.
        // This removes neighbouring atlas fragments when their bounding rectangles overlap.
        private static void IsolateSpriteMesh(Sprite sprite, int textureWidth, Rect rect, float ppu, int islands)
        {
            int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height), ox = (int)rect.x, oy = (int)rect.y;
            var visited = new bool[w * h]; var queue = new Queue<int>(); var groups = new List<List<Vector2>>();
            bool Solid(int x, int y) => x >= 0 && x < w && y >= 0 && y < h && kitPixels[(oy + y) * textureWidth + ox + x].a > 40;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int start = y * w + x; if (visited[start] || !Solid(x, y)) continue;
                var points = new List<Vector2>(); queue.Enqueue(start); visited[start] = true;
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue(), px = p % w, py = p / w; points.Add(new Vector2(px + .5f, py + .5f));
                    void Visit(int nx, int ny)
                    {
                        if (!Solid(nx, ny)) return; int n = ny * w + nx;
                        if (visited[n]) return; visited[n] = true; queue.Enqueue(n);
                    }
                    Visit(px - 1, py); Visit(px + 1, py); Visit(px, py - 1); Visit(px, py + 1);
                }
                groups.Add(points);
            }
            var selected = groups.OrderByDescending(g => g.Count).Take(islands).ToArray();
            if (selected.Length == 0) throw new InvalidOperationException("Empty sprite slice: " + sprite.name);
            var vertices = new List<Vector2>(); var triangles = new List<ushort>();
            foreach (var group in selected)
            {
                // Only extrema per row are needed for the convex hull, keeping authoring deterministic and compact.
                var points = group.GroupBy(p => p.y).SelectMany(row => new[] { row.OrderBy(p => p.x).First(), row.OrderBy(p => p.x).Last() })
                    .Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
                float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                var hull = new List<Vector2>();
                foreach (var p in points) { while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
                int lower = hull.Count;
                for (int i = points.Length - 2; i >= 0; i--) { var p = points[i]; while (hull.Count > lower && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
                if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
                int first = vertices.Count; vertices.AddRange(hull.Select(p => (p - new Vector2(w, h) * .5f) / ppu));
                for (int i = 1; i < hull.Count - 1; i++) { triangles.Add((ushort)first); triangles.Add((ushort)(first + i)); triangles.Add((ushort)(first + i + 1)); }
            }
            sprite.OverrideGeometry(vertices.ToArray(), triangles.ToArray());
        }
        private static ForestGuardianController CreateGuardian(Transform parent, Vector2 position)
        {
            var obj = PrefabUtility.LoadPrefabContents(Root + "Prefabs/ForestGuardian/ForestGuardian.prefab");
            obj.name = "Orchard Warden"; obj.transform.position = Vector3.zero;
            var body = obj.transform.Find("Animated torso");
            Reskin(body.Find("Textured trunk"), trunk, new Vector2(3.7f, 5.5f));
            Reskin(body.Find("Small leafy crown"), crown, new Vector2(4.8f, 3.2f));
            body.Find("Small leafy crown").localPosition = new Vector3(0, 5.6f, 0);
            Reskin(body.Find("Left branch fist"), arm, new Vector2(1.6f, 3.2f));
            Reskin(body.Find("Right branch fist"), arm, new Vector2(1.6f, 3.2f));
            var rig = obj.GetComponent<ForestGuardianAnimation>();
            Set(rig, "leftArm", Shoulder(body, "Left branch fist", new Vector2(-1.6f, 4.5f)));
            Set(rig, "rightArm", Shoulder(body, "Right branch fist", new Vector2(1.6f, 4.5f)));
            Set(rig, "crown", body.Find("Small leafy crown")); Set(rig, "articulatedArms", true);
            Reskin(obj.transform.Find("Left root foot"), root, new Vector2(2.5f, 1.4f));
            Reskin(obj.transform.Find("Right root foot"), root, new Vector2(2.5f, 1.4f));
            Reskin(body.Find("Amber eyes"), eyes, new Vector2(2.1f, .83f));
            body.Find("Amber eyes").localPosition = new Vector3(.15f, 3.62f, 0);
            var boss = obj.GetComponent<ForestGuardianController>();
            Set(boss, "survivalOnly", true); Set(boss, "minimumAttackInterval", 2.1f); Set(boss, "maximumAttackInterval", 2.8f);
            Set(boss, "warningDuration", 1.3f); Set(boss, "leafSpeed", 5f); Set(boss, "leafWaves", 2);
            Set(boss, "rootDuration", .55f); Set(boss, "slamDuration", .45f);
            Set(boss, "impactDelay", .2f);
            Set(boss, "rootSprite", root); Set(boss, "branchSprite", arm); Set(boss, "leafSprite", leaf);
            var effects = obj.AddComponent<GuardianAttackEffects>();
            Set(effects, "dustSprite", AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/ForestGuardian/ContactShadow.png"));
            Set(effects, "debrisSprite", Resource("Rocks")); Set(effects, "leafSprite", leaf);
            Set(boss, "attackEffects", effects);
            var asset = PrefabUtility.SaveAsPrefabAsset(obj, Prefab); PrefabUtility.UnloadPrefabContents(obj);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent); instance.transform.position = position;
            return instance.GetComponent<ForestGuardianController>();
        }
        private static Transform Shoulder(Transform body, string artworkName, Vector2 anchor)
        {
            var artwork = body.Find(artworkName);
            var joint = new GameObject(artworkName + " shoulder").transform;
            joint.SetParent(body, false); joint.localPosition = anchor;
            artwork.SetParent(joint, true);
            return joint;
        }
        private static void Reskin(Transform target, Sprite sprite, Vector2 size)
        { var art = target.GetComponent<SpriteRenderer>(); art.sprite = sprite; target.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1); }
        private static Sprite Resource(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Resources/AppleMeadow/" + name + ".png");
        private static void Ground(string name, Transform parent, float x, float y, float width, float height)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D)); obj.layer = 6; obj.transform.SetParent(parent); obj.transform.position = new Vector3(x, y, 0);
            obj.GetComponent<BoxCollider2D>().size = new Vector2(width, height);
            var art = Visual("Orchard soil", obj.transform, Resource("Ground"), Vector2.zero, Vector2.one, 1);
            art.transform.localScale = Vector3.one; art.drawMode = SpriteDrawMode.Tiled; art.size = new Vector2(width, height);
            if (name != "Orchard clearing") art.color = new Color(.8f, .9f, .63f);
        }
        private static GameObject Barrier(string name, Transform parent, float x)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D)); obj.layer = 6; obj.transform.SetParent(parent); obj.transform.position = new Vector3(x, 3, 0);
            obj.GetComponent<BoxCollider2D>().size = new Vector2(.65f, 7);
            Visual("Rising wooden roots", obj.transform, arm, Vector2.zero, new Vector2(1.2f, 6), 6);
            Visual("Leafy hedge", obj.transform, crown, new Vector2(0, 2.4f), new Vector2(2.3f, 1.7f), 7); return obj;
        }
        private static void Decorate(Transform parent, float left, float right)
        {
            foreach (float x in new[] { left - 4, left + 7, right + 4 })
            {
                var tree = Visual("Old apple orchard", parent, Resource("Tree"), new Vector2(x, 3.2f), new Vector2(7, 6.5f), -5); tree.color = new Color(.9f, .95f, .75f);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = Mathf.Lerp(left - 3, right + 5, i / 11f);
                Visual("Flowers in clearing", parent, Resource(i % 2 == 0 ? "Flowers" : "Grass"), new Vector2(x, .13f), new Vector2(.9f, .45f), 2);
            }
            foreach (float x in new[] { left - 1, left + 5, right + 3 })
                Visual("Meadow stones", parent, Resource("Rocks"), new Vector2(x, .25f), new Vector2(1.2f, .6f), 0);
        }
        private static void BuildLeaves(MeadowBossEncounter encounter, Transform parent, Vector2 bounds)
        {
            var obj = new GameObject("Golden orchard leaves", typeof(GuardianLeaves)); obj.transform.SetParent(parent);
            Set(obj.GetComponent<GuardianLeaves>(), "meadow", encounter); Set(obj.GetComponent<GuardianLeaves>(), "bounds", bounds);
            var so = new SerializedObject(obj.GetComponent<GuardianLeaves>()); var leaves = so.FindProperty("leaves"); leaves.arraySize = 16;
            for (int i = 0; i < 16; i++)
            {
                var art = Visual("Golden drifting leaf " + i, obj.transform, leaf, new Vector2(Mathf.Lerp(bounds.x, bounds.y, i / 16f), .4f + (i * 1.73f) % 7), new Vector2(.14f, .23f), -1);
                art.color = new Color(1, .93f, .61f, .75f); leaves.GetArrayElementAtIndex(i).objectReferenceValue = art.transform;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void CreateTestScene()
        {
            var scene = SceneManager.GetActiveScene(); EditorSceneManager.SaveScene(scene, TestScene, true); scene = EditorSceneManager.OpenScene(TestScene);
            var encounter = Object.FindAnyObjectByType<MeadowBossEncounter>(); var player = Object.FindAnyObjectByType<PlayerDeath>(); var camera = Camera.main; var goal = Object.FindAnyObjectByType<TutorialGoal>();
            foreach (var value in new[] { encounter.transform, player.transform, camera.transform, goal.transform }) value.SetParent(null, true);
            // Retain the authored repeating sky/backdrop, while removing the long route and unrelated traps.
            var keep = Object.FindAnyObjectByType<RepeatingLevelBackdrop>(); if (keep != null) keep.transform.SetParent(null, true);
            foreach (var obj in scene.GetRootGameObjects())
                if (obj != encounter.gameObject && obj != player.gameObject && obj != camera.gameObject && obj != goal.gameObject && (keep == null || obj != keep.gameObject)) Object.DestroyImmediate(obj);
            player.transform.position = encounter.CheckpointPosition + Vector3.left * 1.2f;
            Set(camera.GetComponent<TutorialCamera>(), "levelBounds", new Vector2(encounter.CheckpointPosition.x - 4, encounter.CheckpointPosition.x + 34));
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Orchard test scene save failed");
        }

        private static void BuildHUD(MeadowBossEncounter encounter, Transform parent)
        {
            var obj = new GameObject("Orchard Trial HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MeadowBossHUD)); obj.transform.SetParent(parent);
            obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; obj.GetComponent<Canvas>().sortingOrder = 25;
            var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var hud = obj.GetComponent<MeadowBossHUD>(); Set(hud, "encounter", encounter);
            var approach = Group("Trial ahead", obj.transform, Vector2.zero, new Vector2(1920, 1080));
            Text("Approach title", approach.transform, "THE ORCHARD TRIAL", new Vector2(0, 400), new Vector2(1000, 100), 80);
            Text("Approach lesson", approach.transform, "CHECKPOINT AHEAD  /  DODGE FOR 45 SECONDS", new Vector2(0, 320), new Vector2(1500, 80), 60);
            var intro = Group("Warden introduction", obj.transform, Vector2.zero, new Vector2(1920, 1080));
            Image("Intro top shade", intro.transform, square, new Vector2(0, 475), new Vector2(3000, 130), new Color(.06f, .09f, .02f, .86f));
            Image("Intro lower shade", intro.transform, square, new Vector2(0, -420), new Vector2(3000, 240), new Color(.06f, .09f, .02f, .86f));
            Text("Warden intro name", intro.transform, "ORCHARD WARDEN", new Vector2(0, 285), new Vector2(1500, 130), 112);
            Text("Intro subtitle", intro.transform, "The First Trial of Apple Meadow", new Vector2(0, 205), new Vector2(1400, 90), 64);
            Text("Intro instructions", intro.transform, "SURVIVE 45 SECONDS\nA / D  MOVE     SPACE  JUMP", new Vector2(0, -390), new Vector2(1500, 160), 64);
            Text("Skip", intro.transform, "ENTER  /  SKIP INTRO", new Vector2(0, -510), new Vector2(1100, 70), 56);
            var fight = Group("Survival HUD", obj.transform, Vector2.zero, new Vector2(1920, 1080));
            Image("Trial banner", fight.transform, UI("Panels/Banner"), new Vector2(0, 450), new Vector2(1050, 130), Color.white);
            Text("Boss name", fight.transform, "ORCHARD WARDEN", new Vector2(0, 450), new Vector2(950, 110), 88);
            var timer = Text("Survival timer", fight.transform, "45 s", new Vector2(0, 330), new Vector2(700, 140), 128);
            var track = Image("Survival progress track", fight.transform, square, new Vector2(0, 242), new Vector2(720, 26), new Color(.16f, .12f, .045f, .95f));
            var fill = Image("Time survived", track.transform, square, Vector2.zero, new Vector2(708, 16), new Color(.94f, .68f, .19f)); fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var stage = Text("Trial stage", fight.transform, "I  /  FIND YOUR RHYTHM", new Vector2(0, 183), new Vector2(1100, 75), 56);
            Image("Dodge hint backing", fight.transform, square, new Vector2(0, -445), new Vector2(1450, 105), new Color(.06f, .1f, .025f, .88f));
            var hint = Text("Attack advice", fight.transform, "DODGE UNTIL THE TIMER ENDS", new Vector2(0, -445), new Vector2(1400, 100), 72);
            var complete = Group("Trial completed", obj.transform, Vector2.zero, new Vector2(1920, 1080));
            Image("Completion shade", complete.transform, square, Vector2.zero, new Vector2(4000, 2600), new Color(.025f, .05f, .02f, .78f));
            Image("Storybook result", complete.transform, UI("Panels/Window"), Vector2.zero, new Vector2(1060, 780), Color.white);
            Text("World complete", complete.transform, "WORLD COMPLETED!", new Vector2(0, 240), new Vector2(960, 120), 88);
            Text("Meadow title", complete.transform, "APPLE MEADOW", new Vector2(0, 140), new Vector2(920, 100), 80);
            var result = Text("Trial result", complete.transform, "The old guardian lets you pass.", new Vector2(0, 20), new Vector2(920, 150), 64);
            var next = Button("Continue", complete.transform, new Vector2(-205, -215), UI("Buttons/Continue"));
            var hub = Button("Go to Hub", complete.transform, new Vector2(205, -215), UI("Buttons/CompleteLobby"));
            Set(hud, "approach", approach); Set(hud, "introduction", intro); Set(hud, "fight", fight); Set(hud, "completion", complete);
            Set(hud, "timer", timer); Set(hud, "attackHint", hint); Set(hud, "stage", stage); Set(hud, "result", result); Set(hud, "progressFill", fill);
            Set(hud, "continueButton", next); Set(hud, "hubButton", hub);
        }
        private static Sprite UI(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "UI/Storybook/" + name + ".png");
        private static CanvasGroup Group(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); obj.transform.SetParent(parent, false); Place(obj.GetComponent<RectTransform>(), position, size);
            var group = obj.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false; return group;
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static Image Image(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>(); image.sprite = sprite; image.color = color; image.raycastTarget = false; Place(image.rectTransform, position, size); return image;
        }
        private static TMP_Text Text(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<TextMeshProUGUI>(); text.font = font; text.text = content; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1, .95f, .77f); text.raycastTarget = false; Place(text.rectTransform, position, size); return text;
        }
        private static Button Button(string name, Transform parent, Vector2 position, Sprite sprite)
        {
            var image = Image(name, parent, sprite, position, new Vector2(350, 110), Color.white); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); image.gameObject.AddComponent<UIButtonAnimator>(); return button;
        }
        private static SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, int order)
        {
            if (sprite == null) throw new InvalidOperationException("Missing artwork for " + name);
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var art = obj.GetComponent<SpriteRenderer>(); art.sprite = sprite; art.sortingOrder = order; return art;
        }
        private static void Set(Object target, string name, object value)
        { ForestGuardianBuilder.Set(target, name, value); if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target); }
    }
}
