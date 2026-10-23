using System;
using System.IO;
using System.Linq;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using Trapple.Workshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Editor
{
    /// <summary>Installs the approved illustrated lobby without rebuilding gameplay or UI.</summary>
    public static class AppleMeadowLobbyBuilder
    {
        public const string ArtRoot = "Assets/_Trapple/Art/Hub/AppleMeadow/";
        private const string SourceRoot = "Docs/Art/AppleMeadowLobby/";
        private const string DressingName = "Apple Meadow lobby art";

        [MenuItem("Trapple/Hub/Apply Apple Meadow Lobby")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyToScenes();
        }

        public static void ApplyToScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying lobby art.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the open scene before applying lobby art.");

            ImportArt();
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[] { HubSceneFlowController.HubScenePath, TrappleUIManager.ScenePath })
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    DressScene(scene);
                    ValidateScene(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Could not save " + path);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("APPLE MEADOW LOBBY: installed and validated in Hub and TrappleUI.");
            }
            finally
            {
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void ImportArt()
        {
            Directory.CreateDirectory(ArtRoot);
            foreach (string name in new[] { "SceneryPlate.png", "InteractablesAtlas.png" })
            {
                string source = SourceRoot + name, destination = ArtRoot + name;
                if (!File.Exists(source)) throw new FileNotFoundException("Lobby handoff image missing", source);
                if (!File.Exists(destination) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination)))
                    File.Copy(source, destination, true);
            }
            AssetDatabase.Refresh();
            var scenery = Configure(ArtRoot + "SceneryPlate.png", SpriteImportMode.Single);
            scenery.SaveAndReimport();
            var atlas = Configure(ArtRoot + "InteractablesAtlas.png", SpriteImportMode.Multiple);
            // Unity 6 initializes Multiple import before accepting authored rectangles.
            atlas.SaveAndReimport();
#pragma warning disable 618
            atlas.spritesheet = new[]
            {
                Slice("Workshop", new Rect(0, 499, 650, 525), new Vector2(320, 15)),
                Slice("Wardrobe", new Rect(660, 499, 350, 425), new Vector2(165, 15)),
                Slice("PortalForest", new Rect(1020, 499, 516, 525), new Vector2(250, 15)),
                Slice("PortalFactory", new Rect(0, 0, 525, 499), new Vector2(280, 42)),
                Slice("PortalLava", new Rect(525, 0, 515, 499), new Vector2(255, 42))
            };
#pragma warning restore 618
            EditorUtility.SetDirty(atlas);
            atlas.SaveAndReimport();
            WorkshopDoorAssets.Import();
        }

        private static TextureImporter Configure(string path, SpriteImportMode mode)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.spritePixelsPerUnit = 64;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            return importer;
        }

        private static SpriteMetaData Slice(string name, Rect rect, Vector2 pivotPixels) =>
            new SpriteMetaData { name = name, rect = rect, alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2(pivotPixels.x / rect.width, pivotPixels.y / rect.height) };

        private static Sprite Prop(string name) => AssetDatabase.LoadAllAssetsAtPath(ArtRoot + "InteractablesAtlas.png")
            .OfType<Sprite>().Single(sprite => sprite.name == name);

        private static void DressScene(Scene scene)
        {
            var old = scene.GetRootGameObjects().SingleOrDefault(root => root.name == DressingName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            foreach (var root in scene.GetRootGameObjects())
            {
                string name = root.name;
                if (name == "Golden hour orchard" || name == "Distant rolling meadow" || name == "Near orchard ridge" ||
                    name.StartsWith("Drifting cloud ", StringComparison.Ordinal) ||
                    name.StartsWith("Meadow grass ", StringComparison.Ordinal) ||
                    name.StartsWith("Falling leaf ", StringComparison.Ordinal) ||
                    name == "Portal aura" || name == "Soft station shadow" ||
                    name == "Portal label" || name == "Workshop label" || name == "Wardrobe label")
                    root.SetActive(false);
            }
            var dressing = new GameObject(DressingName).transform;
            var backdrop = Visual("Apple Meadow scenery", AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "SceneryPlate.png"),
                dressing, new Vector2(-4.5f, 2.65f), 36, -40);
            backdrop.transform.localScale = Vector3.one * (36 / backdrop.sprite.bounds.size.x);

            var ground = scene.GetRootGameObjects().Single(root => root.name == "Safe meadow ground");
            ground.GetComponent<SpriteRenderer>().enabled = false;
            var terrain = Visual("Apple Meadow ground", AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/_Trapple/Resources/AppleMeadow/Ground.png"), dressing, new Vector2(0, -2.65f), 1, 1);
            terrain.transform.localScale = Vector3.one * 2;
            terrain.drawMode = SpriteDrawMode.Tiled;
            terrain.size = new Vector2(28, 2.75f);

            var stations = UnityEngine.Object.FindObjectsByType<HubInteractable>(FindObjectsInactive.Include)
                .Where(station => station.gameObject.scene == scene).ToArray();
            var wardrobe = stations.Single(station => station.Screen == HubScreen.Wardrobe).GetComponentInChildren<SpriteRenderer>(true);
            SetProp(wardrobe, Prop("Wardrobe"), new Vector2(-3.8f, 0), 2.25f);
            var portals = stations.Where(station => station.Screen == HubScreen.Worlds)
                .OrderBy(station => station.transform.position.x).ToArray();
            string[] portalNames = { "PortalForest", "PortalFactory", "PortalLava" };
            for (int i = 0; i < portals.Length; i++)
                SetProp(portals[i].GetComponentInChildren<SpriteRenderer>(true), Prop(portalNames[i]),
                    new Vector2(portals[i].transform.position.x, 0), 3.35f);

            RegisterWorkshopDoor(scene);

            var camera = UnityEngine.Object.FindAnyObjectByType<HubCameraController>();
            camera.GetComponent<Camera>().orthographicSize = 6.8f;
            camera.GetComponent<Camera>().backgroundColor = new Color32(52, 157, 249, 255);
            var serialized = new SerializedObject(camera);
            serialized.FindProperty("horizontalBounds").vector2Value = new Vector2(-1.4f, 1.4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (var wall in scene.GetRootGameObjects().Where(root => root.name == "Meadow boundary"))
                wall.transform.position = new Vector3(wall.transform.position.x < 0 ? -12.2f : 13.3f, 3, 0);

            Visual("Apple tree frame", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Resources/AppleMeadow/Tree.png"),
                dressing, new Vector2(-11.5f, 3.2f), 6.5f, -5);
            foreach (float x in new[] { -11.7f, -10.8f, -5.6f, 13f })
                Visual("Meadow flowers", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Resources/AppleMeadow/Flowers.png"),
                    dressing, new Vector2(x, .16f), .65f, 4);
            foreach (float x in new[] { -12f, -9.2f, 10.4f, 13f })
            {
                var foliage = Visual("Low foreground foliage", AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/_Trapple/Resources/AppleMeadow/Bush.png"), dressing, new Vector2(x, -4.25f), 3.5f, 12);
                foliage.color = new Color(.48f, .66f, .52f);
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        internal static void RegisterWorkshopDoor(Scene scene)
        {
            var transition = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<WorkshopTransitionController>(true)).Single();
            var door = transition.GetComponentInChildren<WorkshopDoorAnimator>(true).Presentation;
            var cottage = door.transform.parent.GetComponent<SpriteRenderer>();
            // The illustrated atlas has a baked closed door. Use the matched empty facade instead,
            // so opening the supplied overlay exposes a dark doorway rather than a second door.
            SetProp(cottage, AssetDatabase.LoadAssetAtPath<Sprite>(WorkshopDoorAssets.HousePath),
                new Vector2(-8, 2.45f), 6.7f);
            cottage.sharedMaterial = WorkshopDoorAssets.CottageMaterial();
            // Registration is authored in the facade's original 469 x 347 pixel canvas.
            door.transform.localPosition = WorkshopDoorAssets.OverlayOffsetPixels / WorkshopDoorAssets.PixelsPerUnit;
            door.transform.localRotation = Quaternion.identity;
            door.transform.localScale = Vector3.one;
            door.sortingOrder = cottage.sortingOrder + 1;
            Record(cottage); Record(cottage.transform); Record(door); Record(door.transform);
            var entrance = new SerializedObject(transition);
            var focus = (Transform)entrance.FindProperty("doorFocus").objectReferenceValue;
            var path = entrance.FindProperty("entrancePath");
            var lastStep = (Transform)path.GetArrayElementAtIndex(path.arraySize - 1).objectReferenceValue;
            Vector3 passingOffset = lastStep.position - focus.position;
            focus.position = door.transform.position;
            lastStep.position = focus.position + passingOffset;
            Record(focus); Record(lastStep);
        }

        private static SpriteRenderer Visual(string name, Sprite sprite, Transform parent, Vector2 position, float width, int order)
        {
            if (sprite == null) throw new InvalidOperationException("Missing lobby sprite: " + name);
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            SetProp(renderer, sprite, position, width);
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void SetProp(SpriteRenderer renderer, Sprite sprite, Vector2 position, float width)
        {
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.transform.position = position;
            SetWorldScale(renderer.transform, width / sprite.bounds.size.x);
            var motion = renderer.GetComponent<HubAmbience>();
            if (motion != null) motion.enabled = false; // Keep feet on the common baseline.
            Record(renderer); Record(renderer.transform);
        }

        private static void SetWorldScale(Transform transform, float scale)
        {
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(scale / parentScale.x, scale / parentScale.y, 1);
        }

        private static void Record(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        public static void ValidateScene(Scene scene)
        {
            if (scene.GetRootGameObjects().Count(root => root.name == DressingName) != 1)
                throw new InvalidOperationException("Expected one meadow dressing in " + scene.path);
            var stations = UnityEngine.Object.FindObjectsByType<HubInteractable>().Where(station => station.gameObject.scene == scene).ToArray();
            if (stations.Length != 5 || stations.Count(station => station.Screen == HubScreen.Worlds) != 3)
                throw new InvalidOperationException("Lobby interactions changed in " + scene.path);
            var door = UnityEngine.Object.FindAnyObjectByType<WorkshopDoorAnimator>();
            if (door.FrameCount != 9)
                throw new InvalidOperationException("Workshop door frames are missing.");
            var overlay = door.Presentation;
            var cottage = overlay.transform.parent.GetComponent<SpriteRenderer>();
            Vector3 expectedOffset = WorkshopDoorAssets.OverlayOffsetPixels / WorkshopDoorAssets.PixelsPerUnit;
            if (AssetDatabase.GetAssetPath(cottage.sprite) != WorkshopDoorAssets.HousePath ||
                cottage.sharedMaterial.shader.name != "Trapple/Sprites/Cottage Cutout" ||
                Vector3.Distance(overlay.transform.localPosition, expectedOffset) > .0001f ||
                overlay.transform.localScale != Vector3.one)
                throw new InvalidOperationException("Workshop exterior and animated doorway are not registered.");
            var ground = scene.GetRootGameObjects().Single(root => root.name == "Safe meadow ground").GetComponent<BoxCollider2D>();
            if (Mathf.Abs(ground.bounds.max.y) > .001f)
                throw new InvalidOperationException("The gameplay ground line moved.");
            foreach (var portal in stations.Where(station => station.Screen == HubScreen.Worlds))
                if (!portal.GetComponentInChildren<SpriteRenderer>(true).sprite.name.StartsWith("Portal", StringComparison.Ordinal))
                    throw new InvalidOperationException("Portal artwork is missing.");
        }
    }
}
