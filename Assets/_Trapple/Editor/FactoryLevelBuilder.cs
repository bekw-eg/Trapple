using System;
using System.IO;
using System.Collections.Generic;
using Trapple.Level;
using Trapple.Player;
using Trapple.Traps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Editor
{
    /// <summary>Creates the playable factory level and its reusable physical gear/scrap assets in Unity.</summary>
    public static class FactoryLevelBuilder
    {
        public const string ScenePath = "Assets/_Trapple/Scenes/Levels/Factory.unity";
        private const string Root = "Assets/_Trapple/";
        private const int GroundLayer = 6;

        [MenuItem("Trapple/Build Factory World")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "Art/Factory");
            Directory.CreateDirectory(Root + "Resources/Factory");
            Directory.CreateDirectory(Root + "Prefabs/Factory");
            Directory.CreateDirectory(Root + "Scenes/Levels");

            Sprite square = SpriteAsset("Steel", (x, y) =>
            {
                bool stripe = ((x + y) / 8) % 2 == 0;
                return y > 56 ? new Color(.92f, .39f, .1f) : stripe && y < 10 ? new Color(.9f, .43f, .12f) :
                    new Color(.24f + y / 450f, .21f + y / 500f, .25f + y / 420f);
            });
            Sprite gear = SpriteAsset("Gear", (x, y) => GearPixel(x, y, new Color(1f, .59f, .17f)));
            Sprite gearDark = SpriteAsset("GearWall", (x, y) => GearPixel(x, y, new Color(.58f, .3f, .16f)));
            Sprite scrapSprite = SpriteAsset("FallingScrap", (x, y) =>
                y > 55 || x < 7 || x > 56 || y < 7 ? new Color(.94f, .39f, .12f) :
                ((x / 8 + y / 10) % 2 == 0 ? new Color(.43f, .38f, .39f) : new Color(.28f, .26f, .29f)));
            Sprite warning = SpriteAsset("DropWarning", (x, y) =>
            {
                float edge = Mathf.Abs(x - 32) + Mathf.Abs(y - 32) * .9f;
                return edge < 31 ? edge > 21 || (x > 27 && x < 37 && y > 20) ? new Color(.98f, .28f, .08f) : new Color(1f, .8f, .25f) : Color.clear;
            });
            Sprite apple = SpriteAsset("FactoryApple", (x, y) =>
            {
                if (x > 31 && x < 38 && y > 49 && y < 63) return new Color(.27f, .55f, .19f);
                bool fruit = Vector2.Distance(new Vector2(x, y), new Vector2(23, 32)) < 21 ||
                    Vector2.Distance(new Vector2(x, y), new Vector2(42, 32)) < 21;
                return fruit ? new Color(.95f, .13f, .11f) : Color.clear;
            });

            GameObject gearPrefab = BuildGearPrefab(gear);
            GameObject scrapPrefab = BuildScrapPrefab(scrapSprite);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Player/Player.prefab");
            if (playerPrefab == null) throw new InvalidOperationException("Build the player prefab before building the factory.");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Appi";
            player.transform.position = new Vector2(1f, 1.2f);
            player.AddComponent<FactoryGearInteractor>();

            var backdrop = Box("Factory interior", square, new Vector2(325f, 4.4f), new Vector2(670f, 12f), -30, false);
            backdrop.GetComponent<SpriteRenderer>().color = new Color(.27f, .13f, .14f);
            for (int i = 0; i < 34; i++)
            {
                float x = i * 20f + 8f;
                Box("Back wall support", square, new Vector2(x, 4.2f), new Vector2(.75f, 9.5f), -20, false,
                    new Color(.2f, .13f, .17f));
                Box("Boiler window", square, new Vector2(x + 6f, 4.1f), new Vector2(3.8f, 2.5f), -18, false,
                    new Color(.93f, .26f, .08f, .78f));
                Box("Window mullion", square, new Vector2(x + 6f, 4.1f), new Vector2(.18f, 2.7f), -17, false,
                    new Color(.25f, .16f, .19f));
                Box("Overhead pipe", square, new Vector2(x + 9f, 8.4f), new Vector2(16f, .28f), -16, false,
                    new Color(.41f, .29f, .31f));
                Box("Conveyor rail", square, new Vector2(x + 8f, -.04f), new Vector2(12f, .19f), 2, false,
                    new Color(.78f, .32f, .11f));
                Box("Gear housing", square, new Vector2(x + 13f, 1.6f), new Vector2(2.7f, 2.7f), -2, false,
                    new Color(.22f, .15f, .19f));
                SpriteRenderer gearMark = Visual("Turning machine cog", gearDark, new Vector2(x + 13f, 1.8f),
                    new Vector2(1.7f, 1.7f), -1, new Color(.63f, .32f, .17f));
            }

            var holes = new[] { new Vector2(34f, 4.6f), new Vector2(88f, 4.5f), new Vector2(210f, 4.5f),
                new Vector2(285f, 4.5f), new Vector2(385f, 4.5f), new Vector2(455f, 4.5f),
                new Vector2(585f, 4.6f) };
            float floorStart = 0f;
            foreach (var hole in holes)
            {
                float left = hole.x - hole.y * .5f;
                Ground("Factory conveyor", square, floorStart, left - floorStart);
                float platformX = left + .5f;
                Platform("Jumping conveyor", square, new Vector2(platformX + .8f, .75f), new Vector2(1.7f, .35f));
                Platform("Jumping conveyor", square, new Vector2(platformX + 2.7f, 1.15f), new Vector2(1.5f, .35f));
                floorStart = hole.x + hole.y * .5f;
            }
            Ground("Factory exit conveyor", square, floorStart, 654f - floorStart);

            foreach (float x in new[] { 53f, 236f, 418f, 604f })
            {
                var spikes = Box("Crusher teeth", scrapSprite, new Vector2(x, .45f), new Vector2(2.7f, .6f), 6,
                    true, new Color(.92f, .23f, .08f));
                spikes.AddComponent<SpikeTrap>();
            }

            var zones = new[] { 76f, 270f, 470f };
            for (int i = 0; i < zones.Length; i++)
            {
                var zoneObject = new GameObject("Random ceiling drop " + (i + 1), typeof(BoxCollider2D), typeof(FactoryDropZone));
                zoneObject.transform.position = new Vector3(zones[i], 6.8f, 0f);
                var trigger = zoneObject.GetComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(21f, 12f);
                Set(zoneObject.GetComponent<FactoryDropZone>(), "horizontalJitter", 8f);
                zoneObject.GetComponent<FactoryDropZone>().Configure(scrapPrefab, gearPrefab, warning);
            }

            foreach (float x in new[] { 158f, 350f, 550f })
                BuildGearWall(square, gearDark, x);

            var goalObject = new GameObject("Factory exit apple", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(FactoryGoal));
            goalObject.transform.position = new Vector2(650f, 1.2f);
            var goalRenderer = goalObject.GetComponent<SpriteRenderer>();
            goalRenderer.sprite = apple;
            goalRenderer.sortingOrder = 7;
            goalObject.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            var goalCollider = goalObject.GetComponent<CircleCollider2D>();
            goalCollider.isTrigger = true;
            goalCollider.radius = .5f;
            var runObject = new GameObject("Factory run timer");
            runObject.AddComponent<FactoryRun>();

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(1f, 3f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.5f;
            camera.backgroundColor = new Color(.12f, .055f, .075f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var tracking = cameraObject.AddComponent<TutorialCamera>();
            Set(tracking, "target", player.transform);
            Set(tracking, "offset", new Vector3(0f, 2.2f, -10f));
            Set(tracking, "smoothTime", .2f);

            var scene = SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save Factory scene.");
            ConfigureHubAndBuildList();
            AssetDatabase.SaveAssets();
            Debug.Log("TRAPPLE: Factory World created: one 650-unit factory level, three randomized falling gear drops and three throw-open barriers.");
        }

        private static void ConfigureHubAndBuildList()
        {
            var config = AssetDatabase.LoadAssetAtPath<Trapple.Hub.HubConfiguration>(Root + "Settings/HubConfiguration.asset");
            if (config != null)
            {
                var factory = config.World("factory");
                factory.plannedLevels = 1;
                factory.description = "Промышленные ловушки: падающий металл, шестерёнки и заводские заслонки.";
                factory.boss = string.Empty;
                factory.requiredCompletedWorldId = "meadow";
                factory.completionRequiresBoss = false;
                factory.levelScenes = new[] { ScenePath };
                EditorUtility.SetDirty(config);
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            else
                for (int i = 0; i < scenes.Count; i++)
                    if (scenes[i].path == ScenePath) scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject BuildGearPrefab(Sprite sprite)
        {
            var gear = new GameObject("Factory Gear", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(FactoryGear));
            gear.GetComponent<SpriteRenderer>().sprite = sprite;
            gear.GetComponent<SpriteRenderer>().sortingOrder = 8;
            gear.transform.localScale = Vector3.one * .82f;
            var body = gear.GetComponent<Rigidbody2D>();
            body.gravityScale = 2f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            gear.GetComponent<CircleCollider2D>().radius = .43f;
            var path = Root + "Prefabs/Factory/FactoryGear.prefab";
            var result = PrefabUtility.SaveAsPrefabAsset(gear, path);
            UnityEngine.Object.DestroyImmediate(gear);
            return result;
        }

        private static GameObject BuildScrapPrefab(Sprite sprite)
        {
            var scrap = new GameObject("Falling Scrap", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(FactoryScrapDrop));
            scrap.GetComponent<SpriteRenderer>().sprite = sprite;
            scrap.GetComponent<SpriteRenderer>().sortingOrder = 10;
            scrap.transform.localScale = new Vector3(1.55f, 1.15f, 1f);
            var body = scrap.GetComponent<Rigidbody2D>();
            body.gravityScale = 2.4f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = scrap.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(.9f, .9f);
            var lethal = scrap.AddComponent<BoxCollider2D>();
            lethal.size = new Vector2(.85f, .85f);
            lethal.isTrigger = true;
            var path = Root + "Prefabs/Factory/FallingScrap.prefab";
            var result = PrefabUtility.SaveAsPrefabAsset(scrap, path);
            UnityEngine.Object.DestroyImmediate(scrap);
            return result;
        }

        private static void BuildGearWall(Sprite metal, Sprite gear, float x)
        {
            var wall = new GameObject("Gear powered blast door", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(FactoryGearWall));
            wall.transform.position = new Vector2(x, 2.55f);
            wall.transform.localScale = new Vector3(2.4f, 5.1f, 1f);
            var renderer = wall.GetComponent<SpriteRenderer>();
            renderer.sprite = metal;
            renderer.color = new Color(.84f, .36f, .12f);
            renderer.sortingOrder = 5;
            var collider = wall.GetComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            var indicatorObject = new GameObject("Gear socket lamp", typeof(SpriteRenderer));
            indicatorObject.transform.SetParent(wall.transform, false);
            indicatorObject.transform.localPosition = new Vector3(0f, -.2f, 0f);
            indicatorObject.transform.localScale = new Vector3(.72f, .72f, 1f);
            var indicator = indicatorObject.GetComponent<SpriteRenderer>();
            indicator.sprite = gear;
            indicator.color = new Color(1f, .7f, .25f);
            indicator.sortingOrder = 7;
            indicator.enabled = false;
            wall.GetComponent<FactoryGearWall>().Configure(renderer, collider, indicator);
        }

        private static void Ground(string name, Sprite sprite, float start, float width)
        {
            if (width <= 0f) return;
            var ground = Box(name, sprite, new Vector2(start + width * .5f, -.5f), new Vector2(width, 1f), 1, false,
                new Color(.72f, .65f, .65f));
            ground.layer = GroundLayer;
            ground.AddComponent<BoxCollider2D>();
        }

        private static void Platform(string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var platform = Box(name, sprite, position, size, 4, false, new Color(.88f, .52f, .17f));
            platform.layer = GroundLayer;
            platform.AddComponent<BoxCollider2D>();
        }

        private static GameObject Box(string name, Sprite sprite, Vector2 position, Vector2 size, int sorting,
            bool trigger, Color? tint = null)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sorting;
            renderer.color = tint ?? Color.white;
            if (trigger)
            {
                var collider = obj.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
            }
            return obj;
        }

        private static SpriteRenderer Visual(string name, Sprite sprite, Vector2 position, Vector2 size, int order, Color tint)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.color = tint;
            return renderer;
        }

        private static Sprite SpriteAsset(string name, Func<int, int, Color> pixel)
        {
            string path = Root + "Resources/Factory/" + name + ".png";
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++) texture.SetPixel(x, y, pixel(x, y));
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

        private static Color GearPixel(int x, int y, Color color)
        {
            float dx = x - 31.5f, dy = y - 31.5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);
            float toothRadius = 25f + (Mathf.Cos(angle * 8f) > .76f ? 5f : 0f);
            if (radius > toothRadius || radius < 7f) return Color.clear;
            if (radius > 21f || radius < 10f) return new Color(.18f, .13f, .14f);
            if (radius < 12f) return Color.clear;
            return color;
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(UnityEngine.Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(UnityEngine.Object target, string field, Vector3 value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).vector3Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
