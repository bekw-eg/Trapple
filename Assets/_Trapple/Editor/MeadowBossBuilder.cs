using System;
using System.Linq;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    /// <summary>Appends the existing prototype fight through Unity serialization, preserving the authored route.</summary>
    public static class MeadowBossBuilder
    {
        private const string Root = "Assets/_Trapple/";
        private const string EncounterName = "Meadow boss encounter";
        [MenuItem("Trapple/Boss Progression/Integrate meadow Ancient Tree")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(GameManager.TutorialScene);
            AppendToOpenScene();
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))
                throw new InvalidOperationException("Could not save the meadow boss integration.");
            AssetDatabase.SaveAssets();
        }

        public static void AppendToOpenScene()
        {
            if (SceneManager.GetActiveScene().path != GameManager.TutorialScene)
                throw new InvalidOperationException("Open Tutorial before appending the meadow encounter.");
            var existing = Object.FindAnyObjectByType<MeadowBossEncounter>(FindObjectsInactive.Include);
            if (existing != null)
            {
                var completion = existing.GetComponent<WorldCompletion>();
                if (completion == null) completion = existing.gameObject.AddComponent<WorldCompletion>();
                completion.Configure(AssetDatabase.LoadAssetAtPath<HubConfiguration>(Root + "Settings/HubConfiguration.asset"));
                EditorUtility.SetDirty(completion);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log("MEADOW BOSS: existing integration preserved.");
                return;
            }
            var goal = Object.FindAnyObjectByType<TutorialGoal>();
            var player = Object.FindAnyObjectByType<PlayerDeath>();
            var course = Object.FindAnyObjectByType<LevelCourse>();
            if (goal == null || player == null) throw new InvalidOperationException("Meadow player or goal is missing.");
            float finish = goal.transform.position.x;
            float offset = finish + 15f;
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Prototype/Square.png");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Bosses/AncientTree.prefab");
            if (square == null || prefab == null) throw new InvalidOperationException("Existing Ancient Tree prefab or square sprite missing.");
            var root = new GameObject(EncounterName);
            if (course != null) root.transform.SetParent(course.transform, true);
            var encounter = root.AddComponent<MeadowBossEncounter>();
            root.AddComponent<WorldCompletion>().Configure(
                AssetDatabase.LoadAssetAtPath<HubConfiguration>(Root + "Settings/HubConfiguration.asset"));
            foreach (var wall in Object.FindObjectsByType<BoxCollider2D>().Where(c => c.name == "Level boundary" &&
                c.transform.position.x > finish && c.gameObject.scene == root.scene).ToArray()) Object.DestroyImmediate(wall.gameObject);

            float floorLeft = finish - 2f, floorRight = offset + 8f;
            Ground("Arena clearing", square, new Vector2((floorLeft + floorRight) * .5f, -.5f),
                new Vector2(floorRight - floorLeft, 1f), root.transform);
            Ground("Warning platform", square, new Vector2(offset - .5f, 1.5f), new Vector2(3f, .35f), root.transform);
            Ground("Upper platform", square, new Vector2(offset + 2.5f, 2.8f), new Vector2(2f, .35f), root.transform);
            var gate = Ground("Closing root gate", square, new Vector2(offset - 9.7f, 3f), new Vector2(.4f, 7f), root.transform);
            Ground("Ancient Tree roots", square, new Vector2(offset + 4.8f, 3f), new Vector2(.4f, 7f), root.transform);
            foreach (string name in new[] { "Closing root gate", "Ancient Tree roots" })
                root.transform.Find(name).GetComponent<SpriteRenderer>().color = new Color(.34f, .25f, .13f);

            var bossObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            bossObject.transform.position = new Vector3(offset, 0f, 0f);
            var boss = bossObject.GetComponent<BossController>();
            boss.enabled = false;
            Set(boss, "completeLevelOnDeath", false);
            var attack = boss.GetComponent<BossAttack>();
            Set(attack, "playerTarget", player.transform);
            Set(attack, "arenaX", new Vector2(offset - 8.5f, offset + 3.8f));
            Set(attack, "floorY", 0f);
            bossObject.SetActive(false);

            var lever = Art("Arena lever · E", square, new Vector2(offset - 6f, .6f), new Vector2(.45f, 1.2f),
                new Color(.3f, .85f, .95f), 12, root.transform).gameObject.AddComponent<ArenaLever>();
            Art("Lever handle", square, new Vector2(offset - 6f, 1.2f), new Vector2(.9f, .2f),
                new Color(.3f, .85f, .95f), 13, root.transform);
            var stone = Art("Lever stone", square, new Vector2(offset + 6.2f, 7f), Vector2.one,
                new Color(.7f, .76f, .71f), 14, root.transform);
            stone.gameObject.SetActive(false);
            Set(lever, "boss", boss.GetComponent<BossHealth>()); Set(lever, "player", player);
            Set(lever, "fallingStone", stone.transform);
            Set(lever, "stoneStart", new Vector3(offset + 6.2f, 7f, 0f));
            Set(lever, "stoneEnd", new Vector3(offset + 6.2f, 3f, 0f));
            var camera = Object.FindAnyObjectByType<Camera>();
            Set(encounter, "goal", goal); Set(encounter, "player", player); Set(encounter, "boss", boss);
            Set(encounter, "arenaCamera", camera); Set(encounter, "follow", camera.GetComponent<TutorialCamera>());
            Set(encounter, "entranceBarrier", gate);
            Set(encounter, "cameraPosition", new Vector3(offset - 1f, 3.4f, -10f));
            Set(goal, "encounter", encounter);
            goal.transform.position = new Vector3(offset - 7f, .8f, 0f);
            gate.SetActive(false);
            if (course != null)
            {
                Set(course, "worldBounds", new Vector2(course.WorldBounds.x, floorRight));
                var sections = new SerializedObject(course);
                var values = sections.FindProperty("sections");
                if (values.arraySize > 0) values.GetArrayElementAtIndex(values.arraySize - 1)
                    .FindPropertyRelative("endX").floatValue = floorRight;
                sections.ApplyModifiedPropertiesWithoutUndo();
            }
            var follow = camera.GetComponent<TutorialCamera>();
            if (follow != null) { Set(follow, "useBounds", true); Set(follow, "levelBounds", new Vector2(-4f, floorRight)); }
            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("MEADOW BOSS: original Ancient Tree and E lever appended after the complete platforming route.");
        }

        private static GameObject Ground(string name, Sprite sprite, Vector2 position, Vector2 size, Transform parent)
        {
            var view = Art(name, sprite, position, size, new Color(.36f, .48f, .22f), 2, parent);
            var collider = view.gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            view.gameObject.layer = 6;
            return view.gameObject;
        }
        private static SpriteRenderer Art(string name, Sprite sprite, Vector2 position, Vector2 size,
            Color color, int order, Transform parent)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);
            var view = obj.GetComponent<SpriteRenderer>();
            view.sprite = sprite; view.color = color; view.sortingOrder = order;
            return view;
        }
        private static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + field + " is missing.");
            if (value is Object asset) property.objectReferenceValue = asset;
            else if (value is Vector2 vector2) property.vector2Value = vector2;
            else if (value is Vector3 vector3) property.vector3Value = vector3;
            else if (value is bool boolean) property.boolValue = boolean;
            else if (value is float number) property.floatValue = number;
            else throw new InvalidOperationException("Unsupported meadow builder value: " + field);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }
}
