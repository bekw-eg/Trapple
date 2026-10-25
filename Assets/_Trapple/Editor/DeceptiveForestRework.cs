using System;
using System.Linq;
using Trapple.DeceptiveForest;
using Trapple.Traps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    // An explicit, repeatable migration of the SAVED Level 2. Never creates another scene.
    public static class DeceptiveForestRework
    {
        private const string Prefabs = "Assets/_Trapple/Prefabs/DeceptiveForest/";
        [MenuItem("Trapple/Deceptive Forest/Apply deceptive route rework")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ForestGoal.ScenePath);
            Transform terrain = GameObject.Find("Terrain").transform;
            Transform traps = GameObject.Find("Traps").transform;
            var environment = GameObject.Find("Environment").transform;
            foreach (var decor in traps.GetComponentsInChildren<SpriteRenderer>(true))
                if (decor.name == "Grass" || decor.name == "Flowers" || decor.name == "Bush")
                    decor.transform.SetParent(environment, true);
            foreach (Transform child in terrain.Cast<Transform>().ToArray())
                if (child.name != "Level boundary") Object.DestroyImmediate(child.gameObject);
            foreach (Transform child in traps.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            foreach (string name in new[] { "Entrance sign", "Tree warning sign" })
            {
                var obj = GameObject.Find(name); if (obj != null) Object.DestroyImmediate(obj);
            }

            Ground("01 · Safe entrance", -4, 9, terrain);
            HiddenGround("01 · Hidden ground", 9, 11.4f, traps, .24f, true);
            Ground("02 · Woodland path", 11.4f, 23.8f, terrain);
            Branch("02 · Trusted branch A", 19.1f, 1.2f, terrain);
            Branch("02 · Trusted branch B", 22.2f, 2.4f, terrain);
            var branch = Branch("02 · Trusted branch C", 25.5f, 2.4f, traps);
            Disappear(branch, .28f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(branch, Prefabs + "DisappearingBranch.prefab", InteractionMode.AutomatedAction);
            Branch("02 · Descent", 29f, 1.2f, terrain);
            Pit("02 · Open ravine", 25.5f, 3.4f, traps);
            Ground("03 · Apple bank", 27.2f, 30.4f, terrain);
            var baitGround = HiddenGround("03 · Apple clearing", 30.4f, 32.8f, traps, .27f, false);
            var bait = new GameObject("03 · Apple proximity", typeof(CircleCollider2D), typeof(ForestBaitTrigger));
            bait.transform.SetParent(baitGround.transform);
            bait.transform.position = new Vector3(31.6f, .7f, 0);
            bait.GetComponent<CircleCollider2D>().isTrigger = true; bait.GetComponent<CircleCollider2D>().radius = .8f;
            Set(bait.GetComponent<ForestBaitTrigger>(), "surface", baitGround.GetComponent<ForestDisappearingPlatform>());
            var surface = baitGround.transform.Find("Surface");
            foreach (float x in new[] { 31.2f, 31.8f })
                Art("Windfall apple", "Apple", new Vector2(x, .24f), new Vector2(.38f, .4f), 3, surface);
            Ground("04 · Stick clearing", 32.8f, 52, terrain);
            Tree("04 · Clearing tree", 48.5f, 43.1f, false, traps);
            Branch("05 · Root step", 50.1f, 1.1f, terrain);
            HiddenGround("05 · Landing pit", 52, 54.4f, traps, .22f, true);
            Ground("06 · Quiet landing", 54.4f, 65, terrain);
            Branch("06 · Landing root", 57.5f, .8f, terrain);
            Tree("06 · Landing tree", 63.5f, 57.5f, true, traps);
            HiddenGround("07 · Final hidden ground", 65, 67.4f, traps, .24f, true);
            Ground("07 · Finish bank", 67.4f, 80, terrain);
            var final = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "DisappearingBranch.prefab"), traps);
            final.name = "07 · Last branch"; final.transform.position = new Vector3(69.6f, 1.1f - .225f, 0);
            Set(final.GetComponent<ForestDisappearingPlatform>(), "warningDuration", .3f);
            Tree("07 · Final tree", 74.8f, 68.2f, false, traps);

            // Keep all environment art, but place nearby windfall apples naturally beneath a tree.
            var existingAppleTree = environment.GetComponentsInChildren<SpriteRenderer>()
                .FirstOrDefault(r => r.name == "Framing tree" && Mathf.Abs(r.transform.position.x - 34f) < .1f);
            if (existingAppleTree != null) existingAppleTree.transform.position = new Vector3(33.4f, 2.85f, 0);
            // Decorative grass/flowers on a disappearing patch travel with its surface.
            foreach (var decoration in environment.GetComponentsInChildren<SpriteRenderer>())
            {
                if (decoration.name != "Grass" && decoration.name != "Flowers" && decoration.name != "Bush") continue;
                foreach (var cover in Object.FindObjectsByType<ForestDisappearingPlatform>())
                {
                    if (cover.transform.Find("Concealed pit") == null) continue;
                    var bounds = cover.GetComponent<BoxCollider2D>().bounds;
                    if (decoration.transform.position.x > bounds.min.x && decoration.transform.position.x < bounds.max.x)
                        decoration.transform.SetParent(cover.transform.Find("Surface"), true);
                }
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new Exception("Reworked scene save failed");
            AssetDatabase.SaveAssets();
            DeceptiveForestBuilder.ValidateSavedScene();
            Debug.Log("FOREST REWORK: saved existing scene; 7 encounters, 4 concealed pits, 2 disappearing branches, 3 trees.");
        }

        // For an isolated verification Editor launched with -forest-capture <output>.
        public static void CaptureInEditor()
        {
            if (!Environment.GetCommandLineArgs().Contains("-forest-capture"))
                throw new InvalidOperationException("Capture requires an explicit output argument.");
            // Let Editor startup/indexing finish before entering the measured Play Mode session.
            double readyAfter = EditorApplication.timeSinceStartup + 3d;
            EditorApplication.update += EnterWhenReady;
            void EnterWhenReady()
            {
                if (EditorApplication.timeSinceStartup < readyAfter || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                EditorApplication.update -= EnterWhenReady;
                EditorSceneManager.OpenScene(ForestGoal.ScenePath);
                var gameView = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
                gameView.Show(); gameView.Focus();
                EditorApplication.EnterPlaymode();
            }
        }

        private static GameObject Ground(string name, float left, float right, Transform parent)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D)); obj.layer = 6;
            obj.transform.SetParent(parent); obj.transform.position = new Vector3((left + right) / 2, -2, 0);
            obj.GetComponent<BoxCollider2D>().size = new Vector2(right - left, 4);
            var surface = new GameObject("Surface").transform; surface.SetParent(obj.transform, false);
            var art = Art("Grass and soil", "Ground", new Vector2((left + right) / 2, -1.9f), Vector2.one, 0, surface);
            art.transform.localScale = Vector3.one; art.drawMode = SpriteDrawMode.Tiled; art.size = new Vector2(right - left, 4);
            return obj;
        }
        private static GameObject HiddenGround(string name, float left, float right, Transform parent, float delay, bool contact)
        {
            GameObject obj;
            if (name.StartsWith("01"))
            {
                obj = Ground(name, left, right, parent);
                var hazard = Pit("Concealed pit", (left + right) / 2, right - left, obj.transform);
                hazard.SetActive(false);
                var trap = Disappear(obj, delay); Set(trap, "concealedHazard", hazard);
                PrefabUtility.SaveAsPrefabAssetAndConnect(obj, Prefabs + "FalseGround.prefab", InteractionMode.AutomatedAction);
            }
            else
            {
                obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "FalseGround.prefab"), parent);
                obj.name = name; obj.transform.position = new Vector3((left + right) / 2, -2, 0);
            }
            var component = obj.GetComponent<ForestDisappearingPlatform>();
            Set(component, "warningDuration", delay); Set(component, "activateOnLanding", contact);
            return obj;
        }
        private static GameObject Branch(string name, float x, float top, Transform parent)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D)); obj.layer = 6;
            obj.transform.SetParent(parent); obj.transform.position = new Vector3(x, top - .225f, 0);
            obj.GetComponent<BoxCollider2D>().size = new Vector2(2.4f, .45f);
            var surface = new GameObject("Surface").transform; surface.SetParent(obj.transform, false);
            Art("Branch artwork", "Island", new Vector2(x, top - .28f), new Vector2(2.4f, .85f), 2, surface);
            return obj;
        }
        private static ForestDisappearingPlatform Disappear(GameObject obj, float delay)
        {
            var trap = obj.AddComponent<ForestDisappearingPlatform>();
            Set(trap, "warningDuration", delay); Set(trap, "surface", obj.transform.Find("Surface"));
            Set(trap, "artwork", obj.GetComponentInChildren<SpriteRenderer>());
            return trap;
        }
        private static GameObject Pit(string name, float x, float width, Transform parent)
        {
            var obj = new GameObject(name, typeof(BoxCollider2D), typeof(SpikeTrap));
            obj.transform.SetParent(parent); obj.transform.position = new Vector3(x, -2.5f, 0);
            var hitbox = obj.GetComponent<BoxCollider2D>(); hitbox.isTrigger = true; hitbox.size = new Vector2(width, .65f);
            for (float offset = -width / 2; offset < width / 2 - .01f; offset += 1.2f)
            {
                float w = Mathf.Min(1.2f, width / 2 - offset);
                Art("Pit thorns", "Spikes", new Vector2(x + offset + w / 2, -2.5f), new Vector2(w, .65f), 1, obj.transform);
            }
            return obj;
        }
        private static void Tree(string name, float x, float triggerX, bool landing, Transform parent)
        {
            GameObject obj;
            if (name.StartsWith("04"))
            {
                obj = new GameObject(name, typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpikeTrap), typeof(ForestFallingTree));
                obj.transform.SetParent(parent); obj.transform.position = new Vector3(x, 0, 0);
                var body = obj.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                var hitbox = obj.GetComponent<BoxCollider2D>(); hitbox.isTrigger = true;
                hitbox.size = new Vector2(.6f, 5.5f); hitbox.offset = new Vector2(0, 2.75f);
                Set(obj.GetComponent<SpikeTrap>(), "armed", false);
                var pivot = new GameObject("Tree artwork pivot").transform; pivot.SetParent(obj.transform, false);
                var art = Art("Tree artwork", "Tree", new Vector2(x, 2.75f), new Vector2(5.5f, 5.5f), -12, pivot);
                art.color = new Color(.72f, .88f, .7f);
                var tree = obj.GetComponent<ForestFallingTree>(); Set(tree, "artwork", pivot);
                Set(tree, "warningDuration", .22f); Set(tree, "fallDuration", .85f);
                PrefabUtility.SaveAsPrefabAssetAndConnect(obj, Prefabs + "FallingTree.prefab", InteractionMode.AutomatedAction);
            }
            else
            {
                obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "FallingTree.prefab"), parent);
                obj.name = name; obj.transform.position = new Vector3(x, 0, 0);
            }
            var trigger = new GameObject(name + " trigger", typeof(BoxCollider2D), typeof(ForestTreeTrigger));
            trigger.transform.SetParent(parent); trigger.transform.position = new Vector3(triggerX, 2, 0);
            var area = trigger.GetComponent<BoxCollider2D>(); area.isTrigger = true; area.size = new Vector2(.8f, 4);
            Set(trigger.GetComponent<ForestTreeTrigger>(), "tree", obj.GetComponent<ForestFallingTree>());
            Set(trigger.GetComponent<ForestTreeTrigger>(), "requireLanding", landing);
        }
        private static SpriteRenderer Art(string name, string spriteName, Vector2 position, Vector2 size, int order, Transform parent)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Resources/AppleMeadow/" + spriteName + ".png");
            if (sprite == null) throw new Exception("Missing sprite " + spriteName);
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent); obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order; return renderer;
        }
        private static void Set(Object obj, string name, Object value)
        {
            var data = new SerializedObject(obj); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object obj, string name, float value)
        {
            var data = new SerializedObject(obj); data.FindProperty(name).floatValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object obj, string name, bool value)
        {
            var data = new SerializedObject(obj); data.FindProperty(name).boolValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
