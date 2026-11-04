using System;
using System.Collections.Generic;
using System.IO;
using Trapple.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Editor
{
    /// <summary>Builds the authored factory route from the provided industrial sprites and reusable prefabs.</summary>
    public static class FactoryWorldBuilder
    {
        public const string ScenePath = "Assets/_Trapple/Scenes/Levels/AbandonedFactory.unity";
        private const string Root = "Assets/_Trapple/";
        private const string Art = Root + "Art/Factory/Sprites/";
        private const int GroundLayer = 6;
        private static Sprite platformSprite, wallSprite, conveyorSprite, machineSprite, spikeSprite, gearSprite, pipeSprite, furnaceSprite, backdropSprite, moltenSprite, lampSprite;
        private static Sprite sawToothSprite;
        private static GameObject platformPrefab, breakablePrefab, mobilePrefab, sawPrefab, pressPrefab;

        [MenuItem("Tools/Trapple/Build Abandoned Factory World")]
        public static void Build()
        {
            if (!Application.isBatchMode)
            {
                var active = SceneManager.GetActiveScene();
                if (active.isDirty || string.IsNullOrEmpty(active.path))
                {
                    if (!EditorUtility.DisplayDialog("Build Factory World", "Save the currently open Unity scenes before building the factory?", "Save and build", "Cancel")) return;
                    if (!EditorSceneManager.SaveOpenScenes()) return;
                }
            }
            AssetDatabase.Refresh();
            LoadArt();
            EnsureFolders();
            BuildPrefabs();
            var factoryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            if (SceneManager.GetActiveScene() != factoryScene && !SceneManager.SetActiveScene(factoryScene))
                throw new InvalidOperationException("Could not activate the new factory scene.");
            BuildScene();
            ConfigureWorldAndBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Factory World built: six sections, 350-unit route, active hazards, moving equipment and seven checkpoints.");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Root + "Prefabs/Factory");
            Directory.CreateDirectory(Root + "Scenes/Levels");
        }

        private static void LoadArt()
        {
            platformSprite = LoadSprite(Art + "FactoryPlatform.png");
            wallSprite = LoadSprite(Art + "FactoryWall.png");
            conveyorSprite = LoadSprite(Art + "FactoryConveyor.png");
            machineSprite = LoadSprite(Art + "FactoryMachine.png");
            spikeSprite = LoadSprite(Art + "FactorySpikes.png");
            gearSprite = LoadSprite(Art + "FactoryGear.png");
            pipeSprite = LoadSprite(Art + "FactoryPipe.png");
            furnaceSprite = LoadSprite(Art + "FactoryFurnace.png");
            backdropSprite = LoadSprite(Art + "FactoryBackdrop.png");
            moltenSprite = LoadSprite(Art + "FactoryMolten.png");
            lampSprite = LoadSprite(Art + "FactoryLamp.png");
            sawToothSprite = LoadSprite(Art + "FactorySawTooth.png");
            if (platformSprite == null || conveyorSprite == null || machineSprite == null || gearSprite == null || backdropSprite == null)
                throw new InvalidOperationException("Factory art has not imported. Let Unity finish asset import, then build again.");
        }

        private static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency ||
                importer.spritePixelsPerUnit != 100f || importer.textureCompression != TextureImporterCompression.Uncompressed))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BuildPrefabs()
        {
            platformPrefab = SavePlatform("FactoryPlatform", platformSprite, false);
            breakablePrefab = SavePlatform("FactoryBreakablePlatform", platformSprite, true);

            var mobile = MakeSpriteBody("Factory Moving Platform", conveyorSprite, new Vector2(3.15f, .7f), 8,
                new Vector2(2.9f, .38f), RigidbodyType2D.Kinematic);
            mobile.AddComponent<FactoryMovingPlatform>();
            mobile.layer = GroundLayer;
            mobilePrefab = SavePrefab(mobile, "FactoryMovingPlatform");

            var saw = MakeSpriteBody("Rotating Factory Saw", gearSprite, Vector2.one * 1.45f, 11, Vector2.one * .94f,
                RigidbodyType2D.Static, true);
            saw.GetComponent<CircleCollider2D>().radius = .48f;
            for (int i = 0; i < 8; i++)
            {
                float degrees = i * 45f;
                float radians = degrees * Mathf.Deg2Rad;
                var tooth = new GameObject("Hardened saw tooth", typeof(SpriteRenderer));
                tooth.transform.SetParent(saw.transform, false);
                tooth.transform.localPosition = new Vector3(Mathf.Sin(radians) * .61f, Mathf.Cos(radians) * .61f, 0f);
                tooth.transform.localRotation = Quaternion.Euler(0, 0, -degrees);
                tooth.transform.localScale = new Vector3(.35f, .29f, 1f);
                tooth.GetComponent<SpriteRenderer>().sprite = sawToothSprite;
                tooth.GetComponent<SpriteRenderer>().sortingOrder = 12;
            }
            saw.AddComponent<FactorySawTrap>();
            sawPrefab = SavePrefab(saw, "FactorySaw");

            var press = MakeSpriteBody("Factory Press Ram", platformSprite, new Vector2(2.75f, .9f), 9,
                new Vector2(2.7f, .8f), RigidbodyType2D.Kinematic);
            press.AddComponent<FactoryPressTrap>();
            press.layer = GroundLayer;
            var danger = new GameObject("Press crush volume", typeof(BoxCollider2D));
            danger.transform.SetParent(press.transform, false);
            danger.transform.localPosition = new Vector3(0, -2.3f, 0);
            var trigger = danger.GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.5f, 4.7f);
            pressPrefab = SavePrefab(press, "FactoryPress");
        }

        private static GameObject SavePlatform(string name, Sprite sprite, bool breakable)
        {
            var platform = MakeSpriteBody(name, sprite, Vector2.one, 3,
                new Vector2(sprite.bounds.size.x * 1.03f, sprite.bounds.size.y),
                breakable ? RigidbodyType2D.Kinematic : RigidbodyType2D.Static);
            platform.layer = GroundLayer;
            if (breakable)
            {
                var fall = platform.AddComponent<Trapple.Traps.FallingPlatform>();
                var serialized = new SerializedObject(fall);
                serialized.FindProperty("fallDelay").floatValue = .38f;
                serialized.FindProperty("fallGravityScale").floatValue = 2.7f;
                serialized.FindProperty("lifetimeAfterFall").floatValue = 1.8f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return SavePrefab(platform, name);
        }

        private static GameObject MakeSpriteBody(string name, Sprite sprite, Vector2 visualSize, int order,
            Vector2 colliderSize, RigidbodyType2D bodyType, bool circle = false)
        {
            var root = new GameObject(name, typeof(SpriteRenderer), typeof(Rigidbody2D));
            var renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            root.transform.localScale = new Vector3(visualSize.x / sprite.bounds.size.x,
                visualSize.y / sprite.bounds.size.y, 1f);
            var body = root.GetComponent<Rigidbody2D>();
            body.bodyType = bodyType;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (circle)
            {
                var collider = root.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
            }
            else
            {
                var collider = root.AddComponent<BoxCollider2D>();
                collider.size = colliderSize;
            }
            return root;
        }

        private static GameObject SavePrefab(GameObject source, string name)
        {
            string path = Root + "Prefabs/Factory/" + name + ".prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(source, path);
            UnityEngine.Object.DestroyImmediate(source);
            if (saved == null) throw new InvalidOperationException("Could not save factory prefab: " + path);
            return saved;
        }

        private static GameObject Place(GameObject prefab, string name, Vector2 position, Vector3? scale = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            instance.name = name;
            instance.transform.position = position;
            if (scale.HasValue) instance.transform.localScale = scale.Value;
            return instance;
        }

        private static void BuildScene()
        {
            var playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Player/Player.prefab");
            if (playerAsset == null) throw new InvalidOperationException("The existing Appi player prefab is missing.");

            BuildBackground();
            BuildFloor();
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerAsset, SceneManager.GetActiveScene());
            player.name = "Appi";
            player.transform.position = new Vector2(2f, 1.05f);
            ConfigureCourse(player.GetComponent<Trapple.Player.PlayerDeath>());

            // Zone 1: a broad, low-risk entry that teaches the belt and vertical movement.
            AddConveyor(12f, 19f, 1f, 10f);
            AddStairSet(26f, 1f, 4, 2.4f, .52f, .54f, false);
            AddCheckpoint(43f);
            AddSectionSign(3f, "01   ВХОД В ФАБРИКУ", "ОСТАНОВИ ЛИНИЮ И НАЙДИ РИТМ");
            WorldText("A / D   ·   SPACE: JUMP   ·   SHIFT: SPRINT", new Vector2(11f, 6.25f), .16f,
                new Color(.94f, .83f, .76f), 25);

            // Zone 2: increasingly narrow belts, exposed saws and the first short floor break.
            AddConveyor(48f, 58f, 1f, 20f);
            AddSaw(55f, 1.15f, .75f);
            AddSaw(70f, 1.05f, 1.15f);
            AddSaw(83f, 1.2f, .9f);
            AddSpikeStrip(78f);
            AddConveyor(65f, 75f, -1f, 20f);
            AddCheckpoint(88f);
            AddSectionSign(49f, "02   ЛИНИЯ СБОРКИ", "КОНВЕЙЕРЫ   /   ЦИРКУЛЯРНЫЕ ПИЛЫ");

            // Zone 3: three offset hydraulic cycles. Wait at the lit side bays and cross on the lift.
            AddPress(100f, 5.6f, 0f);
            AddPress(113f, 5.9f, 1.8f);
            AddPress(127f, 5.7f, 3.2f);
            AddPlatform(97f, 1.15f, 2.6f, false);
            AddPlatform(106f, 1.0f, 2.4f, false);
            AddPlatform(119f, 1.1f, 2.5f, false);
            AddPlatform(133f, 1.25f, 2.5f, false);
            AddSaw(107f, 1.7f, .55f);
            AddCheckpoint(137f);
            AddSectionSign(99f, "03   ПРЕССОВЫЙ ЗАЛ", "СМОТРИ НА СВЕТОВОЙ ЦИКЛ");

            // Zone 4: a molten spill, a moving bridge and a second moving deck under pressure.
            AddHazard(146f, -2.2f, new Vector2(8.7f, 3.7f), new Color(1f, .27f, .04f, .9f));
            AddPlatform(140f, .7f, 2.5f, false);
            AddPlatform(152f, .85f, 2.5f, false);
            AddMovingPlatform(new Vector2(141.5f, 2.0f), new Vector2(150.5f, 2.0f), 2.1f);
            AddConveyor(158f, 170f, -1f, 24f);
            AddSaw(164f, 1.2f, .8f);
            AddPress(176.5f, 5.4f, 1.2f);
            AddPlatform(172f, 1.0f, 2.6f, false);
            AddPlatform(181.5f, 1.1f, 2.8f, false);
            AddCheckpoint(185f);
            AddSectionSign(157f, "04   ЛИТЕЙНЫЙ ЦЕХ", "ГОРЯЧИЙ ПОЛ   /   ДВИЖУЩАЯСЯ ПЛАТФОРМА");

            // Zone 5: a forced vertical route above a live furnace trench, with safe and weak steps mixed.
            AddHazard(248f, -.78f, new Vector2(59f, 1.05f), new Color(1f, .22f, .025f, .96f));
            AddSaw(190f, 1.25f, .8f);
            AddSaw(208f, 1.15f, .72f);
            AddStairSet(217f, .85f, 10, 2.8f, .76f, .5f, true);
            AddStairSet(245f, 7.6f, 11, 2.75f, -.74f, .5f, true);
            AddMovingPlatform(new Vector2(241f, 5.35f), new Vector2(248f, 5.35f), 2.6f, .6f);
            AddCheckpoint(214.5f);
            AddPlatform(276f, .8f, 2.8f, false);
            AddCheckpoint(276f, 1.65f);
            AddSectionSign(220f, "05   ВЕРТИКАЛЬНАЯ СБОРКА", "НЕ ЗАДЕРЖИВАЙСЯ НА РЖАВОМ МЕТАЛЛЕ");

            // Zone 6: return to the main deck, combine presses and belts, then enter the closed arena bay.
            AddPress(282f, 5.8f, 2.1f);
            AddConveyor(291f, 301f, 1f, 25f);
            AddSaw(296f, 1.3f, .85f);
            AddMovingPlatform(new Vector2(310f, 1.8f), new Vector2(318f, 1.8f), 2.3f, .3f);
            AddPlatform(307f, 1.2f, 2.6f, false);
            AddPlatform(321f, 1.15f, 2.8f, false);
            AddCheckpoint(306f);
            AddSectionSign(280f, "06   ПУТЬ К АРЕНЕ", "ПОСЛЕДНИЙ ЦИКЛ — И ВЫХОДНОЙ ШЛЮЗ");
            BuildArena();
            BuildAtmosphere();

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(TutorialCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(2f, 3f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.backgroundColor = new Color(.075f, .045f, .075f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            var follow = cameraObject.GetComponent<TutorialCamera>();
            Set(follow, "target", player.transform);
            Set(follow, "offset", new Vector3(2.1f, 2.05f, -10f));
            Set(follow, "smoothTime", .13f);
            AddParallax(camera);

            var config = AssetDatabase.LoadAssetAtPath<Trapple.Hub.HubConfiguration>(Root + "Settings/HubConfiguration.asset");
            var progression = new GameObject("World Completion").AddComponent<WorldCompletion>();
            progression.Configure(config);
            new GameObject("Factory Checkpoint Restore").AddComponent<FactoryCheckpointRestore>();

            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath))
                throw new InvalidOperationException("Could not save the abandoned factory scene.");
        }

        private static void BuildBackground()
        {
            BoxVisual("Factory night wall", wallSprite, new Vector2(174f, 4.3f), new Vector2(390f, 13f), -100,
                new Color(.2f, .125f, .16f));
            for (int i = 0; i < 14; i++)
            {
                float x = 13.5f + i * 27f;
                BoxVisual("Factory painted interior", backdropSprite, new Vector2(x, 4.35f), new Vector2(27.1f, 5.05f), -80);
                BoxVisual("Riveted wall support", wallSprite, new Vector2(x - 13.35f, 4.4f), new Vector2(.9f, 9.3f), -65,
                    new Color(.66f, .36f, .25f));
                if (i % 2 == 0)
                    BoxVisual("Furnace silhouette", furnaceSprite, new Vector2(x + 7.8f, 2.9f), new Vector2(5.6f, 4.1f), -50,
                        new Color(.72f, .52f, .44f));
                if (i % 2 == 1)
                    BoxVisual("Pipe bank", pipeSprite, new Vector2(x + 8.7f, 5.25f), new Vector2(2.25f, 5.3f), -46,
                        new Color(.68f, .51f, .48f));
                if (i % 2 == 0)
                    BoxVisual("Control bank", machineSprite, new Vector2(x + 4.2f, 3.3f), new Vector2(4.5f, 4.1f), -44,
                        new Color(.76f, .57f, .49f));
                BoxVisual("Factory warning lantern", lampSprite, new Vector2(x + 1.5f, 7.4f), new Vector2(1.15f, 1.65f), -35);
                BoxVisual("Overhead hazard beam", conveyorSprite, new Vector2(x + 6f, 8.5f), new Vector2(12f, .9f), -42,
                    new Color(.69f, .51f, .46f));
            }
        }

        private static void BuildFloor()
        {
            var gaps = new (float left, float right)[]
            {
                (61f,64.2f),(92f,95.2f),(141.7f,150.4f),(179f,182.2f),
                (218.6f,278f),(283f,286.2f),(309f,312.5f)
            };
            float cursor = 0f;
            foreach (var gap in gaps)
            {
                FloorRun(cursor, gap.left);
                cursor = gap.right;
            }
            FloorRun(cursor, 351f);
            for (float x = 0f; x < 351f; x += 13.5f)
                BoxVisual("Conveyor side rail", conveyorSprite, new Vector2(x + 6f, -.62f), new Vector2(12.4f, .38f), 4,
                    new Color(.75f, .69f, .66f));
        }

        private static void FloorRun(float start, float end)
        {
            const float tileWidth = 3.48f;
            for (float x = start; x < end - .01f;)
            {
                float width = Mathf.Min(tileWidth, end - x);
                var tile = Place(platformPrefab, "Bolted steel floor", new Vector2(x + width * .5f, -.62f),
                    new Vector3(width / tileWidth, 1f, 1f));
                tile.layer = GroundLayer;
                var box = tile.GetComponent<BoxCollider2D>();
                box.size = new Vector2(width / Mathf.Max(.001f, tile.transform.localScale.x) + .03f, 1.22f);
                x += width;
            }
        }

        private static void AddConveyor(float left, float right, float direction, float force)
        {
            var belt = new GameObject("Powered conveyor", typeof(BoxCollider2D), typeof(FactoryConveyor));
            belt.transform.position = new Vector2((left + right) * .5f, .42f);
            var trigger = belt.GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(right - left, .96f);
            belt.GetComponent<FactoryConveyor>().Configure(force, direction);
            for (float x = left + 1.6f; x < right; x += 3.2f)
                BoxVisual("Moving steel belt", conveyorSprite, new Vector2(x, .52f), new Vector2(3.22f, 1.05f), 5);
        }

        private static void AddSaw(float x, float y, float scale)
        {
            var saw = Place(sawPrefab, "Live circular saw", new Vector2(x, y));
            saw.transform.localScale *= scale;
            saw.GetComponent<SpriteRenderer>().color = new Color(1f, .82f, .68f);
            var collider = saw.GetComponent<CircleCollider2D>();
            collider.radius = .58f;
        }

        private static void AddPress(float x, float raisedY, float delay)
        {
            var press = Place(pressPrefab, "Timed hydraulic press", new Vector2(x, raisedY));
            var hazard = press.transform.Find("Press crush volume").GetComponent<BoxCollider2D>();
            hazard.enabled = true;
            var controller = press.GetComponent<FactoryPressTrap>();
            controller.Configure(new Vector2(x, raisedY), new Vector2(x, 1.22f), 5.7f, .55f, delay);
            var beaconObject = new GameObject("Press cycle warning", typeof(SpriteRenderer));
            beaconObject.transform.SetParent(press.transform, false);
            beaconObject.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            beaconObject.transform.localScale = Vector3.one * .4f;
            var beacon = beaconObject.GetComponent<SpriteRenderer>();
            beacon.sprite = gearSprite;
            beacon.sortingOrder = 12;
            controller.SetWarningIndicator(beacon);
        }

        private static void AddSpikeStrip(float x)
        {
            var spikes = new GameObject("Factory floor spikes", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Trapple.Traps.SpikeTrap));
            spikes.transform.position = new Vector2(x, .38f);
            spikes.transform.localScale = new Vector3(2.35f / spikeSprite.bounds.size.x, .68f / spikeSprite.bounds.size.y, 1f);
            var renderer = spikes.GetComponent<SpriteRenderer>();
            renderer.sprite = spikeSprite;
            renderer.sortingOrder = 8;
            var collider = spikes.GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(spikeSprite.bounds.size.x * .94f, spikeSprite.bounds.size.y * .74f);
        }

        private static void AddMovingPlatform(Vector2 a, Vector2 b, float seconds, float phase = 0f)
        {
            var platform = Place(mobilePrefab, "Synchronized transfer platform", a);
            platform.GetComponent<FactoryMovingPlatform>().Configure(a, b, seconds, phase);
        }

        private static void AddPlatform(float x, float y, float width, bool breakable)
        {
            float nominalWidth = platformSprite.bounds.size.x;
            var prefab = breakable ? breakablePrefab : platformPrefab;
            var platform = Place(prefab, breakable ? "Weak riveted catwalk" : "Raised steel catwalk",
                new Vector2(x, y), new Vector3(width / nominalWidth, .72f / platformSprite.bounds.size.y, 1f));
            platform.layer = GroundLayer;
            var collider = platform.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(nominalWidth * .98f, platformSprite.bounds.size.y * .8f);
        }

        private static void AddStairSet(float startX, float startY, int count, float spacing, float rise, float platformWidth, bool breakables)
        {
            for (int i = 0; i < count; i++)
            {
                float x = startX + i * spacing;
                float y = startY + i * rise;
                AddPlatform(x, y, platformWidth, breakables && i % 3 == 1);
                if (i % 2 == 0)
                    BoxVisual("Stair support girder", pipeSprite, new Vector2(x, y - 1.35f), new Vector2(.42f, 2.6f), 1,
                        new Color(.72f, .5f, .42f));
            }
        }

        private static void AddHazard(float x, float y, Vector2 size, Color color)
        {
            var hazard = new GameObject("Lethal molten channel", typeof(BoxCollider2D), typeof(FactoryHazard));
            hazard.transform.position = new Vector2(x, y);
            var trigger = hazard.GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = size;
            float tileWidth = moltenSprite.bounds.size.x;
            float left = x - size.x * .5f;
            for (float at = left; at < left + size.x; at += tileWidth)
            {
                float width = Mathf.Min(tileWidth, left + size.x - at);
                BoxVisual("Lava-filled hazard trough", moltenSprite,
                    new Vector2(at + width * .5f, y), new Vector2(width, size.y), 8, color);
            }
        }

        private static void AddCheckpoint(float x, float spawnY = 1.1f)
        {
            var checkpoint = new GameObject("Factory checkpoint", typeof(BoxCollider2D), typeof(FactoryCheckpoint), typeof(SpriteRenderer));
            checkpoint.transform.position = new Vector2(x, spawnY);
            var trigger = checkpoint.GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.15f, 2.2f);
            var renderer = checkpoint.GetComponent<SpriteRenderer>();
            renderer.sprite = gearSprite;
            renderer.color = new Color(.52f, .82f, .54f);
            renderer.sortingOrder = 13;
            checkpoint.GetComponent<FactoryCheckpoint>().Configure(checkpoint.transform, renderer);
            BoxVisual("Checkpoint station", wallSprite, new Vector2(x, .7f), new Vector2(1.6f, 1.55f), 9,
                new Color(.66f, .5f, .43f));
        }

        private static void AddSectionSign(float x, string title, string subtitle)
        {
            BoxVisual("Amber section sign", wallSprite, new Vector2(x + 6.3f, 6.9f), new Vector2(12.4f, 1.45f), 20,
                new Color(.72f, .48f, .35f));
            WorldText(title, new Vector2(x + 6.3f, 7.15f), .3f, new Color(1f, .76f, .36f), 25);
            WorldText(subtitle, new Vector2(x + 6.3f, 6.72f), .19f, new Color(.95f, .83f, .74f), 25);
        }

        private static void BuildArena()
        {
            BoxVisual("Arena blast gate", wallSprite, new Vector2(329f, 3.4f), new Vector2(2.8f, 6.7f), 14,
                new Color(.88f, .42f, .22f));
            BoxVisual("Arena header", conveyorSprite, new Vector2(335f, 7.1f), new Vector2(15f, 1.1f), 16,
                new Color(.95f, .64f, .39f));
            WorldText("MECHANICAL BOSS ARENA", new Vector2(335f, 7.17f), .3f, new Color(1f, .79f, .47f), 25);
            BoxVisual("Arena deck", wallSprite, new Vector2(337f, 2.9f), new Vector2(21f, 1.1f), 8,
                new Color(.74f, .5f, .39f));
            var apple = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Resources/AppleMeadow/Apple.png");
            if (apple == null) throw new InvalidOperationException("Existing Apple Meadow goal sprite is missing.");
            var goal = new GameObject("Factory exit apple", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(FactoryGoal));
            goal.transform.position = new Vector2(344f, 1.35f);
            goal.transform.localScale = Vector3.one * 1.25f;
            goal.GetComponent<SpriteRenderer>().sprite = apple;
            goal.GetComponent<SpriteRenderer>().sortingOrder = 18;
            goal.GetComponent<CircleCollider2D>().radius = .58f;
        }

        private static GameObject BoxVisual(string name, Sprite sprite, Vector2 position, Vector2 size, int order, Color? tint = null)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (tint.HasValue) renderer.color = tint.Value;
            return obj;
        }

        private static void WorldText(string text, Vector2 position, float size, Color color, int order)
        {
            var obj = new GameObject(text, typeof(TextMesh), typeof(MeshRenderer));
            obj.transform.position = new Vector3(position.x, position.y, -1f);
            obj.transform.localScale = Vector3.one;
            var mesh = obj.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 72;
            mesh.characterSize = size;
            mesh.color = color;
            obj.GetComponent<MeshRenderer>().sortingOrder = order;
        }

        private static void ConfigureWorldAndBuildSettings()
        {
            var config = AssetDatabase.LoadAssetAtPath<Trapple.Hub.HubConfiguration>(Root + "Settings/HubConfiguration.asset");
            if (config == null) throw new InvalidOperationException("HubConfiguration.asset is missing.");
            var factory = config.World("factory");
            if (factory == null) throw new InvalidOperationException("The factory world is missing from HubConfiguration.");
            factory.plannedLevels = 1;
            factory.requiredCompletedWorldId = "meadow";
            factory.completionRequiresBoss = false;
            factory.description = "Шесть участков заброшенной фабрики: конвейеры, пилы, прессы и разрушающиеся мосты.";
            factory.levelScenes = new[] { ScenePath };
            EditorUtility.SetDirty(config);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int index = scenes.FindIndex(s => s.path == ScenePath);
            var entry = new EditorBuildSettingsScene(ScenePath, true);
            if (index < 0) scenes.Add(entry); else scenes[index] = entry;
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigureCourse(Trapple.Player.PlayerDeath player)
        {
            var course = new GameObject("Factory course map").AddComponent<LevelCourse>();
            var serialized = new SerializedObject(course);
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.FindProperty("worldBounds").vector2Value = new Vector2(0f, 351f);
            var sections = serialized.FindProperty("sections");
            (string name, float start, float end, float seconds)[] design =
            {
                ("01 / Вход в фабрику", 0f, 48f, 40f),
                ("02 / Линия сборки", 48f, 97f, 48f),
                ("03 / Прессовый зал", 97f, 138f, 60f),
                ("04 / Литейный цех", 138f, 185f, 55f),
                ("05 / Вертикальная сборка", 185f, 280f, 75f),
                ("06 / Путь к арене", 280f, 351f, 52f)
            };
            sections.arraySize = design.Length;
            for (int i = 0; i < design.Length; i++)
            {
                var section = sections.GetArrayElementAtIndex(i);
                section.FindPropertyRelative("name").stringValue = design[i].name;
                section.FindPropertyRelative("startX").floatValue = design[i].start;
                section.FindPropertyRelative("endX").floatValue = design[i].end;
                section.FindPropertyRelative("estimatedSeconds").floatValue = design[i].seconds;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddParallax(Camera camera)
        {
            var far = new GameObject("Slow industrial parallax").AddComponent<FactoryParallaxLayer>();
            far.Configure(camera, backdropSprite, .16f, 5.1f, 5.2f, -90, new Color(.72f, .68f, .78f));
            var mid = new GameObject("Near industrial parallax").AddComponent<FactoryParallaxLayer>();
            mid.Configure(camera, backdropSprite, .48f, 3.55f, 3.55f, -76, new Color(.48f, .38f, .47f));
        }

        private static void BuildAtmosphere()
        {
            foreach (float x in new[] { 17f, 79f, 137f, 194f, 253f, 311f, 337f })
                AddSteam(x, 6.2f);
            foreach (float x in new[] { 57f, 108f, 164f, 296f })
                AddSparks(x, .65f);
        }

        private static void AddSteam(float x, float y)
        {
            var effect = new GameObject("Soft steam vent", typeof(ParticleSystem));
            effect.transform.position = new Vector3(x, y, 0f);
            var system = effect.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.duration = 4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.12f, .38f);
            main.startSize = new ParticleSystem.MinMaxCurve(.18f, .48f);
            main.startColor = new Color(.72f, .68f, .74f, .23f);
            main.maxParticles = 24;
            var emission = system.emission;
            emission.rateOverTime = 1.5f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 11f;
            shape.radius = .28f;
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = -24;
        }

        private static void AddSparks(float x, float y)
        {
            var effect = new GameObject("Occasional welding sparks", typeof(ParticleSystem));
            effect.transform.position = new Vector3(x, y, 0f);
            var system = effect.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.duration = 3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.2f, .6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.7f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.045f, .11f);
            main.startColor = new Color(1f, .49f, .1f, .95f);
            main.maxParticles = 8;
            var emission = system.emission;
            emission.rateOverTime = .9f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = .1f;
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 10;
        }

        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException("Missing serialized field: " + field);
            if (value is Transform transform) property.objectReferenceValue = transform;
            else if (value is float number) property.floatValue = number;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
