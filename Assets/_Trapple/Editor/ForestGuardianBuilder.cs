using System;
using System.IO;
using System.Linq;
using TMPro;
using Trapple.Bosses;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    /// <summary>Repeatable Unity API migration. Never edits scene/prefab YAML.</summary>
    public static class ForestGuardianBuilder
    {
        private const string Root = "Assets/_Trapple/";
        private const string Art = Root + "Art/ForestGuardian/";
        private const string Prefabs = Root + "Prefabs/ForestGuardian/";
        private const string EncounterName = "Forest Guardian Encounter";
        private static Sprite trunk, crown, arm, root, eyes, leaf, square, stick;
        private static TMP_FontAsset font;
        private static AudioClip pickupSound, introSound;

        [MenuItem("Trapple/Boss progression/Build and integrate both worlds")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Prefabs); Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            PrepareArt(); PrepareSounds();
            WorldCompletionSetup.ApplyConfiguration();
            BuildForest();
            BuildTestArena();
            MeadowBossBuilder.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("GUARDIAN BUILD: both existing worlds integrated, prefabs and modular sprites saved by Unity.");
        }

        [MenuItem("Trapple/Boss progression/Fix stick sizes and enlarge forest HUD")]
        public static void ApplyReadabilityFix()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareArt();
            BuildStickProjectile();
            var pickup = PrefabUtility.LoadPrefabContents(Prefabs + "StickPickup.prefab");
            var art = pickup.GetComponentInChildren<SpriteRenderer>();
            Resize(art, new Vector2(1.25f, .65f));
            pickup.GetComponent<BoxCollider2D>().size = new Vector2(1.2f, .9f);
            PrefabUtility.SaveAsPrefabAsset(pickup, Prefabs + "StickPickup.prefab");
            PrefabUtility.UnloadPrefabContents(pickup);
            foreach (string path in new[] { ForestGoal.ScenePath, Root + "Scenes/BossArenas/ForestGuardianTest.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var encounter = Object.FindAnyObjectByType<ForestGuardianEncounter>();
                var player = Object.FindAnyObjectByType<PlayerDeath>();
                var hand = player.transform.Find("Throwing hand")?.GetComponent<SpriteRenderer>();
                if (hand != null) Resize(hand, new Vector2(.85f, .44f));
                var hud = encounter.GetComponentInChildren<ForestEncounterHUD>();
                if (hud != null) Object.DestroyImmediate(hud.gameObject);
                BuildHUD(encounter, encounter.transform);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Readability save failed: " + path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("GUARDIAN READABILITY: world-unit sticks, separate visual/physics sizes, enlarged HUD saved in both forest scenes.");
        }

        private static GameObject BuildStickProjectile()
        {
            var projectile = new GameObject("StickProjectile", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(StickProjectile));
            var art = Visual("Projectile artwork", projectile.transform, stick, Vector2.zero, new Vector2(1.25f, .65f), 12);
            var collider = projectile.GetComponent<BoxCollider2D>(); collider.size = new Vector2(1, .24f); collider.offset = new Vector2(0, -.12f);
            projectile.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Set(projectile.GetComponent<StickProjectile>(), "artwork", art);
            Set(projectile.GetComponent<StickProjectile>(), "visualSize", new Vector2(1.25f, .65f));
            var asset = PrefabUtility.SaveAsPrefabAsset(projectile, Prefabs + "StickProjectile.prefab");
            Object.DestroyImmediate(projectile); return asset;
        }

        private static void Resize(SpriteRenderer art, Vector2 size)
        {
            if (art == null || art.sprite == null) throw new InvalidOperationException("Stick artwork is missing.");
            Vector2 bounds = art.sprite.bounds.size;
            art.transform.localScale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1);
        }

        private static void PrepareArt()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art + "GuardianKit.png");
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "GuardianKit.png");
            // The generated sheet is hand-sliced against the inspected 1280px output, not assumed grid cells.
            trunk = Slice(texture, "Torso", new Rect(0, 550, 460, 730));
            crown = Slice(texture, "Crown", new Rect(455, 650, 420, 450));
            arm = Slice(texture, "Arm", new Rect(955, 520, 310, 670));
            root = Slice(texture, "Root", new Rect(15, 20, 500, 500));
            eyes = Slice(texture, "Eyes", new Rect(525, 175, 420, 195));
            leaf = Slice(texture, "Leaf", new Rect(995, 20, 250, 410));
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Prototype/Square.png");
            stick = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/DeceptiveForest/Stick.png");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "UI/Storybook/Fonts/Thaleah SDF.asset");
        }
        private static Sprite Slice(Texture2D texture, string name, Rect rect)
        {
            string path = Art + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            float scale = texture.width / 1280f;
            rect = new Rect(rect.x * scale, rect.y * scale, rect.width * scale, rect.height * scale);
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100 * scale, 0, SpriteMeshType.FullRect);
            sprite.name = name; AssetDatabase.CreateAsset(sprite, path); return sprite;
        }
        private static void PrepareSounds()
        {
            pickupSound = Tone("StickPickup", .18f, false);
            introSound = Tone("GuardianAwakens", 4.8f, true);
        }
        private static AudioClip Tone(string name, float duration, bool rumble)
        {
            string path = Art + name + ".wav";
            if (!File.Exists(path))
            {
                const int rate = 22050; int samples = Mathf.CeilToInt(rate * duration);
                using (var writer = new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                    writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                    writer.Write((short)2); writer.Write((short)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                    for (int i = 0; i < samples; i++)
                    {
                        float t = (float)i / rate, u = t / duration;
                        float envelope = rumble ? Mathf.Sin(Mathf.PI * u) * .28f : (1 - u) * .35f;
                        float wave = rumble ? Mathf.Sin(t * 2 * Mathf.PI * 62) + .3f * Mathf.Sin(t * 2 * Mathf.PI * 91) : Mathf.Sin(t * 2 * Mathf.PI * (650 + 900 * u));
                        writer.Write((short)(Mathf.Clamp(wave * envelope, -1, 1) * short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void BuildForest()
        {
            EditorSceneManager.OpenScene(ForestGoal.ScenePath);
            var old = GameObject.Find(EncounterName);
            float finish = old != null ? new SerializedObject(old.GetComponent<ForestGuardianEncounter>()).FindProperty("checkpointPosition").vector3Value.x + 1 : Object.FindAnyObjectByType<ForestGoal>().transform.position.x;
            if (old != null) Object.DestroyImmediate(old);
            foreach (var value in Object.FindObjectsByType<BossArenaCameraController>()) Object.DestroyImmediate(value);
            var player = Object.FindAnyObjectByType<PlayerDeath>();
            foreach (var value in player.GetComponents<StickThrowController>()) Object.DestroyImmediate(value);
            foreach (var value in player.GetComponents<StickInventory>()) Object.DestroyImmediate(value);
            var previousHand = player.transform.Find("Throwing hand"); if (previousHand != null) Object.DestroyImmediate(previousHand.gameObject);
            foreach (var wall in Object.FindObjectsByType<Transform>().Where(t => t.name == "Level boundary" && t.position.x > finish).ToArray()) Object.DestroyImmediate(wall.gameObject);
            var parent = new GameObject(EncounterName);
            var encounter = parent.AddComponent<ForestGuardianEncounter>();
            parent.AddComponent<WorldCompletion>().Configure(AssetDatabase.LoadAssetAtPath<HubConfiguration>(Root + "Settings/HubConfiguration.asset"));
            var inventory = player.gameObject.AddComponent<StickInventory>();
            var throwing = player.gameObject.AddComponent<StickThrowController>();
            Set(throwing, "inventory", inventory); Set(throwing, "characterRenderer", player.GetComponent<SpriteRenderer>());
            var projectileAsset = BuildStickProjectile();
            Set(throwing, "projectilePrefab", projectileAsset.GetComponent<StickProjectile>());
            var hand = Visual("Throwing hand", player.transform, stick, new Vector2(.3f, .1f), new Vector2(.85f, .44f), 12);
            Set(throwing, "handVisual", hand.transform);
            var pickup = new GameObject("StickPickup", typeof(BoxCollider2D), typeof(StickPickup));
            pickup.GetComponent<BoxCollider2D>().isTrigger = true; pickup.GetComponent<BoxCollider2D>().size = new Vector2(1.2f, .9f);
            var pickupArt = Visual("Wooden branch", pickup.transform, stick, Vector2.zero, new Vector2(1.25f, .65f), 5);
            Set(pickup.GetComponent<StickPickup>(), "artwork", pickupArt); Set(pickup.GetComponent<StickPickup>(), "pickupSound", pickupSound);
            var pickupAsset = PrefabUtility.SaveAsPrefabAsset(pickup, Prefabs + "StickPickup.prefab"); Object.DestroyImmediate(pickup);
            // Safe authored banks in each course section; raycasts keep pickups on actual walkable surfaces.
            var course = Object.FindAnyObjectByType<LevelCourse>();
            float[] preferred = new[] { 4f, 15f, 36f, 55f, 77f, 170f, 310f, 465f, 610f };
            Physics2D.SyncTransforms();
            for (int i = 0; i < preferred.Length; i++)
            {
                float x = preferred[i]; RaycastHit2D hit = default; bool validBank = false;
                for (int n = 0; n < 20; n++)
                {
                    hit = Physics2D.Raycast(new Vector2(x, 8), Vector2.down, 12, 1 << 6);
                    if (hit.collider != null && hit.normal.y > .9f && hit.point.y >= -.1f && hit.point.y < .4f && hit.collider.GetComponent<ForestDisappearingPlatform>() == null) { validBank = true; break; }
                    x += .7f;
                }
                if (!validBank) throw new InvalidOperationException("No safe stick bank near " + preferred[i]);
                var item = (GameObject)PrefabUtility.InstantiatePrefab(pickupAsset, parent.transform);
                item.name = "Collectible branch " + (i + 1).ToString("00"); item.transform.position = new Vector3(x, hit.point.y + .3f, 0);
                Set(item.GetComponent<StickPickup>(), "pickupId", "forest-route-" + i);
            }
            float left = finish + 2, right = finish + 24;
            var groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Resources/AppleMeadow/Ground.png");
            var floor = new GameObject("Guardian clearing floor", typeof(BoxCollider2D)); floor.layer = 6;
            floor.transform.SetParent(parent.transform); floor.transform.position = new Vector3(finish + 11, -1.5f, 0);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(28, 3);
            var floorArt = Visual("Mossy clearing", floor.transform, groundSprite, Vector2.zero, Vector2.one, 0);
            floorArt.transform.localScale = Vector3.one; floorArt.drawMode = SpriteDrawMode.Tiled; floorArt.size = new Vector2(28, 3); floorArt.color = new Color(.58f, .75f, .56f);
            var barrier = Barrier("Roots seal the entrance", parent.transform, left - 1, root);
            Barrier("Ancient roots border", parent.transform, right + 1, root);
            var backdrop = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/DeceptiveForest/ForestBackdrop.png");
            var background = Visual("Deep forest arena", parent.transform, backdrop, new Vector2((left + right) / 2, 4), new Vector2(31, 16), -20);
            background.color = new Color(.54f, .67f, .64f);
            var guardian = BuildGuardian(parent.transform, new Vector2(right - 4.5f, 0));
            var follow = Object.FindAnyObjectByType<ForestCamera>();
            Set(follow, "levelBounds", new Vector2(course.WorldBounds.x, right + 2));
            var arenaCamera = follow.gameObject.AddComponent<BossArenaCameraController>(); Set(arenaCamera, "follow", follow);
            var progress = parent.AddComponent<WorldProgressController>(); Set(progress, "player", player);
            var route = new Vector2[course.Sections.Length + 2]; route[0] = new Vector2(0, .5f);
            for (int i = 0; i < course.Sections.Length; i++) route[i + 1] = new Vector2(Mathf.Min(course.Sections[i].endX, left + 1), .5f);
            route[route.Length - 1] = new Vector2(left + 1, .5f);
            var serialized = new SerializedObject(progress); var points = serialized.FindProperty("route"); points.arraySize = route.Length;
            for (int i = 0; i < route.Length; i++) points.GetArrayElementAtIndex(i).vector2Value = route[i]; serialized.ApplyModifiedPropertiesWithoutUndo();
            Set(course, "worldBounds", new Vector2(course.WorldBounds.x, right + 2));
            var courseData = new SerializedObject(course); var sections = courseData.FindProperty("sections");
            if (sections.arraySize > 0) sections.GetArrayElementAtIndex(sections.arraySize - 1).FindPropertyRelative("endX").floatValue = right + 2;
            courseData.ApplyModifiedPropertiesWithoutUndo();
            Set(encounter, "player", player); Set(encounter, "guardian", guardian); Set(encounter, "inventory", inventory);
            Set(encounter, "progress", progress); Set(encounter, "arenaCamera", arenaCamera); Set(encounter, "entranceBarrier", barrier);
            Set(encounter, "arenaBounds", new Vector2(left, right)); Set(encounter, "checkpointPosition", new Vector3(finish - 1, .65f, 0));
            var atmosphere = parent.AddComponent<AudioSource>(); atmosphere.clip = introSound; atmosphere.playOnAwake = false; atmosphere.volume = .45f;
            Set(encounter, "atmosphere", atmosphere);
            Trigger("Checkpoint before guardian", parent.transform, encounter, finish - 1, true);
            Trigger("Enter guardian clearing", parent.transform, encounter, left + 1, false);
            var goal = Object.FindAnyObjectByType<ForestGoal>(); goal.GetComponent<Collider2D>().enabled = false;
            var flag = GameObject.Find("Finish flag"); if (flag != null) flag.transform.position = new Vector3(finish - 2.5f, .85f, 0);
            BuildHUD(encounter, parent.transform);
            BuildLeaves(encounter, parent.transform, new Vector2(left, right));
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new Exception("Forest save failed");
            Debug.Log("GUARDIAN: 9 pickups, checkpoint x=" + (finish - 1) + ", arena " + left + ".." + right);
        }

        private static void BuildTestArena()
        {
            const string testScene = Root + "Scenes/BossArenas/ForestGuardianTest.unity";
            var scene = SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene, testScene, true)) throw new Exception("Guardian test copy failed");
            scene = EditorSceneManager.OpenScene(testScene);
            var encounter = Object.FindAnyObjectByType<ForestGuardianEncounter>();
            var player = Object.FindAnyObjectByType<PlayerDeath>();
            var camera = Object.FindAnyObjectByType<BossArenaCameraController>();
            player.transform.SetParent(null, true); camera.transform.SetParent(null, true);
            foreach (var obj in scene.GetRootGameObjects())
                if (obj != encounter.gameObject && obj != player.gameObject && obj != camera.gameObject) Object.DestroyImmediate(obj);
            var serialized = new SerializedObject(encounter);
            Vector3 checkpoint = serialized.FindProperty("checkpointPosition").vector3Value;
            Vector2 bounds = serialized.FindProperty("arenaBounds").vector2Value;
            player.transform.position = checkpoint + Vector3.left * 1.5f;
            Set(camera.GetComponent<ForestCamera>(), "levelBounds", new Vector2(checkpoint.x - 3, bounds.y + 2));
            var pickups = Object.FindObjectsByType<StickPickup>();
            for (int i = 0; i < pickups.Length; i++) pickups[i].transform.position = new Vector3(checkpoint.x - .7f + i * .4f, .3f, 0);
            var progress = new SerializedObject(encounter.Progress);
            var route = progress.FindProperty("route"); route.arraySize = 2;
            route.GetArrayElementAtIndex(0).vector2Value = player.transform.position;
            route.GetArrayElementAtIndex(1).vector2Value = new Vector2(bounds.x + 1, .5f);
            progress.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Guardian test scene save failed");
        }

        private static ForestGuardianController BuildGuardian(Transform parent, Vector2 position)
        {
            var obj = new GameObject("ForestGuardian", typeof(BossHealth), typeof(ForestGuardianAnimation), typeof(ForestGuardianController), typeof(BoxCollider2D));
            obj.transform.SetParent(parent); obj.transform.position = position;
            obj.GetComponent<BoxCollider2D>().size = new Vector2(3.4f, 5.1f); obj.GetComponent<BoxCollider2D>().offset = new Vector2(0, 2.7f);
            var rig = obj.GetComponent<ForestGuardianAnimation>(); var guardian = obj.GetComponent<ForestGuardianController>();
            Set(obj.GetComponent<BossHealth>(), "maximumHealth", 5);
            var body = new GameObject("Animated torso").transform; body.SetParent(obj.transform, false);
            Visual("Textured trunk", body, trunk, new Vector2(0, 3), new Vector2(3.7f, 5.6f), 7);
            Visual("Small leafy crown", body, crown, new Vector2(0, 5.8f), new Vector2(4, 2.7f), 8);
            var leftArm = Visual("Left branch fist", body, arm, new Vector2(-2.1f, 3.2f), new Vector2(1.7f, 3.6f), 6); leftArm.flipX = true;
            var rightArm = Visual("Right branch fist", body, arm, new Vector2(2.1f, 3.2f), new Vector2(1.7f, 3.6f), 9);
            var leftRoot = Visual("Left root foot", obj.transform, root, new Vector2(-1.1f, .65f), new Vector2(2.6f, 1.5f), 8); leftRoot.flipX = true;
            var rightRoot = Visual("Right root foot", obj.transform, root, new Vector2(1.1f, .65f), new Vector2(2.6f, 1.5f), 8);
            var eyeArt = Visual("Amber eyes", body, eyes, new Vector2(0, 3.75f), new Vector2(2, .95f), 10);
            Set(rig, "body", body); Set(rig, "leftArm", leftArm.transform); Set(rig, "rightArm", rightArm.transform);
            Set(rig, "leftRoot", leftRoot.transform); Set(rig, "rightRoot", rightRoot.transform); Set(rig, "eyes", eyeArt.transform);
            var shadowTexture = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 64; x++)
            {
                float d = new Vector2((x - 31.5f) / 31.5f, (y - 15.5f) / 15.5f).magnitude;
                shadowTexture.SetPixel(x, y, new Color(.04f, .08f, .05f, Mathf.Pow(Mathf.Clamp01(1 - d), 1.5f) * .65f));
            }
            shadowTexture.Apply(); string shadowPath = Art + "ContactShadow.png"; File.WriteAllBytes(shadowPath, shadowTexture.EncodeToPNG()); Object.DestroyImmediate(shadowTexture);
            AssetDatabase.ImportAsset(shadowPath); var importer = (TextureImporter)AssetImporter.GetAtPath(shadowPath); importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true; importer.SaveAndReimport();
            Visual("Soft contact shadow", obj.transform, AssetDatabase.LoadAssetAtPath<Sprite>(shadowPath), new Vector2(0, .06f), new Vector2(6, .8f), 1);
            var warning = Visual("Amber attack warning", obj.transform, square, Vector2.zero, new Vector2(2, .12f), 14); warning.color = new Color(1, .75f, .12f, .75f); warning.enabled = false;
            var hazard = new GameObject("GuardianHazard", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D), typeof(ForestGuardianHazard));
            hazard.GetComponent<SpriteRenderer>().sprite = leaf; hazard.GetComponent<SpriteRenderer>().sortingOrder = 12;
            hazard.GetComponent<BoxCollider2D>().isTrigger = true; hazard.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var hazardAsset = PrefabUtility.SaveAsPrefabAsset(hazard, Prefabs + "GuardianHazard.prefab"); Object.DestroyImmediate(hazard);
            Set(guardian, "animationRig", rig); Set(guardian, "hazardPrefab", hazardAsset.GetComponent<ForestGuardianHazard>()); Set(guardian, "warning", warning);
            Set(guardian, "rootSprite", root); Set(guardian, "branchSprite", arm); Set(guardian, "leafSprite", leaf);
            Set(guardian, "leafWaveInterval", .4f);
            // Keep the reusable asset at the origin; the scene instance owns its arena placement.
            obj.transform.localPosition = Vector3.zero;
            PrefabUtility.SaveAsPrefabAssetAndConnect(obj, Prefabs + "ForestGuardian.prefab", InteractionMode.AutomatedAction);
            obj.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
            return guardian;
        }
        private static GameObject Barrier(string name, Transform parent, float x, Sprite sprite)
        {
            var value = new GameObject(name, typeof(BoxCollider2D)); value.layer = 6; value.transform.SetParent(parent);
            value.transform.position = new Vector3(x, 3, 0); value.GetComponent<BoxCollider2D>().size = new Vector2(.8f, 8);
            Visual("Visible tangled roots", value.transform, sprite, Vector2.zero, new Vector2(2, 7), 11);
            return value;
        }
        private static void Trigger(string name, Transform parent, ForestGuardianEncounter encounter, float x, bool checkpoint)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D), typeof(BossEncounterTrigger)); obj.transform.SetParent(parent); obj.transform.position = new Vector3(x, 1.5f, 0);
            obj.GetComponent<BoxCollider2D>().isTrigger = true; obj.GetComponent<BoxCollider2D>().size = new Vector2(1, 4);
            Set(obj.GetComponent<BossEncounterTrigger>(), "encounter", encounter); Set(obj.GetComponent<BossEncounterTrigger>(), "checkpoint", checkpoint);
        }
        private static void BuildLeaves(ForestGuardianEncounter encounter, Transform parent, Vector2 bounds)
        {
            var obj = new GameObject("Ambient falling leaves", typeof(GuardianLeaves)); obj.transform.SetParent(parent);
            Set(obj.GetComponent<GuardianLeaves>(), "encounter", encounter); Set(obj.GetComponent<GuardianLeaves>(), "bounds", bounds);
            var so = new SerializedObject(obj.GetComponent<GuardianLeaves>()); var items = so.FindProperty("leaves"); items.arraySize = 18;
            for (int i = 0; i < 18; i++)
            {
                var art = Visual("Drifting leaf " + i, obj.transform, leaf, new Vector2(Mathf.Lerp(bounds.x, bounds.y, i / 18f), .4f + (i * 1.73f) % 7), new Vector2(.16f, .25f), -1);
                art.color = new Color(.67f, .8f, .45f, .7f); items.GetArrayElementAtIndex(i).objectReferenceValue = art.transform;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHUD(ForestGuardianEncounter encounter, Transform parent)
        {
            var obj = new GameObject("Forest Encounter HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ForestEncounterHUD)); obj.transform.SetParent(parent);
            var canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 25;
            var scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var hud = obj.GetComponent<ForestEncounterHUD>(); Set(hud, "encounter", encounter);
            var routeGroup = Group("World route", obj.transform, new Vector2(0, -92), new Vector2(960, 150), new Vector2(.5f, 1));
            var track = Image("Vine track", routeGroup.transform, square, new Vector2(0, -24), new Vector2(680, 22), new Color(.08f, .18f, .13f, .95f));
            var fill = Image("Smooth progress", track.transform, square, Vector2.zero, new Vector2(680, 22), new Color(.45f, .76f, .3f)); fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            Image("Forest start", routeGroup.transform, crown, new Vector2(-390, -18), new Vector2(68, 62), Color.white);
            var icon = Image("Guardian destination", routeGroup.transform, trunk, new Vector2(390, -18), new Vector2(48, 76), Color.white);
            var routeText = Text("Route label", routeGroup.transform, "DECEPTIVE FOREST", new Vector2(0, 32), new Vector2(840, 90), 72);
            var combatGroup = Group("Boss combat", obj.transform, new Vector2(0, -108), new Vector2(960, 180), new Vector2(.5f, 1)); combatGroup.alpha = 0;
            Text("Boss name", combatGroup.transform, "FOREST GUARDIAN", new Vector2(0, 45), new Vector2(900, 110), 88);
            var hpTrack = Image("Bark health track", combatGroup.transform, square, new Vector2(0, -17), new Vector2(760, 26), new Color(.12f, .09f, .06f, .95f));
            var hpFill = Image("Guardian health", hpTrack.transform, square, Vector2.zero, new Vector2(748, 16), new Color(.77f, .34f, .16f)); hpFill.type = UnityEngine.UI.Image.Type.Filled; hpFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var timer = Text("Survival countdown", combatGroup.transform, "SURVIVE  60 s", new Vector2(0, -82), new Vector2(620, 90), 72);
            Image("Collected stick", obj.transform, stick, new Vector2(100, -170), new Vector2(84, 62), Color.white, new Vector2(0, 1));
            var counter = Text("Stick inventory", obj.transform, "x 0", new Vector2(235, -170), new Vector2(180, 110), 88, new Vector2(0, 1));
            Text("Throw control", obj.transform, "F  THROW", new Vector2(195, -242), new Vector2(300, 65), 50, new Vector2(0, 1));
            var title = Group("Guardian introduction", obj.transform, Vector2.zero, new Vector2(1920, 1080), new Vector2(.5f, .5f)); title.alpha = 0;
            Image("Upper cinema border", title.transform, square, new Vector2(0, 480), new Vector2(2500, 120), new Color(.01f, .025f, .02f, .85f));
            Image("Lower cinema border", title.transform, square, new Vector2(0, -480), new Vector2(2500, 120), new Color(.01f, .025f, .02f, .85f));
            Text("Guardian title", title.transform, "FOREST GUARDIAN", new Vector2(0, 285), new Vector2(1400, 100), 76);
            Text("Guardian subtitle", title.transform, "The Keeper of Deceptive Forest", new Vector2(0, 217), new Vector2(1200, 55), 34);
            Text("Skip intro hint", title.transform, "ENTER  /  SKIP INTRO", new Vector2(0, -480), new Vector2(900, 40), 26);
            var victory = Group("World completed", obj.transform, Vector2.zero, new Vector2(900, 650), new Vector2(.5f, .5f)); victory.blocksRaycasts = true;
            Image("Victory backdrop", victory.transform, square, Vector2.zero, new Vector2(4000, 2500), new Color(.02f, .06f, .045f, .83f));
            Image("Storybook victory window", victory.transform, LoadUI("Panels/Window"), Vector2.zero, new Vector2(900, 650), Color.white);
            Text("Completion title", victory.transform, "WORLD COMPLETED!", new Vector2(0, 185), new Vector2(800, 80), 62);
            Text("Completed world", victory.transform, "DECEPTIVE FOREST", new Vector2(0, 104), new Vector2(800, 65), 48);
            var subtitle = Text("Victory outcome", victory.transform, "The forest is free.", new Vector2(0, 10), new Vector2(780, 70), 30);
            var next = Button("Continue", victory.transform, new Vector2(-175, -160), LoadUI("Buttons/Continue"));
            var hub = Button("Go to Hub", victory.transform, new Vector2(175, -160), LoadUI("Buttons/CompleteLobby"));
            Set(hud, "routeGroup", routeGroup); Set(hud, "combatGroup", combatGroup); Set(hud, "titleGroup", title);
            Set(hud, "routeFill", fill); Set(hud, "healthFill", hpFill); Set(hud, "bossIcon", icon.rectTransform);
            Set(hud, "routeText", routeText); Set(hud, "timerText", timer); Set(hud, "stickText", counter); Set(hud, "subtitle", subtitle);
            Set(hud, "victory", victory.gameObject); Set(hud, "continueButton", next); Set(hud, "hubButton", hub);
        }
        private static Sprite LoadUI(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "UI/Storybook/" + path + ".png");
        private static CanvasGroup Group(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); obj.transform.SetParent(parent, false);
            Place(obj.GetComponent<RectTransform>(), position, size, anchor); var group = obj.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = true; return group;
        }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
        { rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static Image Image(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, Vector2? anchor = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var value = obj.GetComponent<Image>(); value.sprite = sprite; value.color = color; value.raycastTarget = false; Place(value.rectTransform, position, size, anchor ?? new Vector2(.5f, .5f)); return value;
        }
        private static TMP_Text Text(string name, Transform parent, string content, Vector2 position, Vector2 size, float fontSize, Vector2? anchor = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
            var value = obj.GetComponent<TextMeshProUGUI>(); value.font = font; value.text = content; value.fontSize = fontSize; value.alignment = TextAlignmentOptions.Center;
            value.color = new Color(1, .94f, .75f); value.raycastTarget = false; Place(value.rectTransform, position, size, anchor ?? new Vector2(.5f, .5f)); return value;
        }
        private static Button Button(string name, Transform parent, Vector2 position, Sprite sprite)
        {
            var image = Image(name, parent, sprite, position, new Vector2(300, 94), Color.white); image.raycastTarget = true;
            var value = image.gameObject.AddComponent<UnityEngine.UI.Button>(); var colors = value.colors; colors.highlightedColor = new Color(1, 1, .72f); colors.pressedColor = new Color(.8f, .85f, .65f); value.colors = colors; return value;
        }
        private static SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, int order)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent, false); obj.transform.localPosition = position;
            var value = obj.GetComponent<SpriteRenderer>(); value.sprite = sprite; value.sortingOrder = order;
            if (sprite != null) obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1); return value;
        }
        public static void Set(Object target, string name, object value)
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(name);
            if (property == null) throw new MissingFieldException(target.GetType().Name, name);
            if (value is Object obj) property.objectReferenceValue = obj;
            else if (value is Vector3 v3) property.vector3Value = v3;
            else if (value is Vector2 v2) property.vector2Value = v2;
            else if (value is bool b) property.boolValue = b;
            else if (value is int i) property.intValue = i;
            else if (value is float f) property.floatValue = f;
            else if (value is string s) property.stringValue = s;
            else throw new ArgumentException(name);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
