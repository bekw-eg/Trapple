using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Level;
using Trapple.Player;
using Trapple.Traps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    /// <summary>Adds authored encounter sequences to the two existing scenes through Unity serialization.</summary>
    public static class ExpandedLevelBuilder
    {
        private const string Root = "Assets/_Trapple/";
        private const string Output = "Docs/LevelExpansion/";
        private const string ExpansionName = "Expanded course";
        private static Transform root, sectionRoot;
        private static PlayerController player;
        private static bool forest;
        private static float cursor, stride, speed, jumpSeconds, lastRouteX;
        private static int encounterIndex;
        private static Route route;

        [Serializable] private sealed class Command
        {
            public string action, context;
            public float x, seconds;
        }
        [Serializable] private sealed class Route
        {
            public string scene;
            public float speed, jumpHeight, gravity, jumpRange, stride, colliderWidth;
            public float extensionStart, finishX;
            public List<Command> commands = new List<Command>();
            public List<LevelCourse.Section> sections = new List<LevelCourse.Section>();
        }
        private enum Kind { Ridge, Stones, Bridge, Spikes, Hidden, FalseGround, Bait, Tree, Gate }
        private readonly struct Encounter
        {
            public readonly Kind kind;
            public readonly int count;
            public Encounter(Kind kind, int count = 3) { this.kind = kind; this.count = count; }
        }
        private static Encounter R(int n) => new Encounter(Kind.Ridge, n);
        private static Encounter S(int n) => new Encounter(Kind.Stones, n);
        private static Encounter B(int n) => new Encounter(Kind.Bridge, n);
        private static Encounter P(int n) => new Encounter(Kind.Spikes, n);
        private static Encounter H(int n) => new Encounter(Kind.Hidden, n);
        private static Encounter F => new Encounter(Kind.FalseGround);
        private static Encounter T => new Encounter(Kind.Tree);
        private static Encounter A => new Encounter(Kind.Bait);
        private static Encounter G => new Encounter(Kind.Gate);

        [MenuItem("Trapple/Levels/Apply seven-section expansion")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Output);
            Expand(false);
            Expand(true);
            AssetDatabase.SaveAssets();
            Debug.Log("EXPANSION: both existing scenes saved by Unity. Timing estimates require novice playtests.");
        }

        private static void Expand(bool isForest)
        {
            forest = isForest;
            string path = forest ? ForestGoal.ScenePath : TutorialBuilder.ScenePath;
            EditorSceneManager.OpenScene(path);
            var previous = GameObject.Find(ExpansionName);
            if (previous != null) Object.DestroyImmediate(previous);
            root = new GameObject(ExpansionName, typeof(LevelCourse), typeof(LevelRunTimer)).transform;
            player = Object.FindAnyObjectByType<PlayerController>();
            var config = new SerializedObject(player);
            speed = config.FindProperty("moveSpeed").floatValue;
            float height = config.FindProperty("jumpHeight").floatValue;
            float gravity = Mathf.Abs(Physics2D.gravity.y) * player.GetComponent<Rigidbody2D>().gravityScale;
            jumpSeconds = 2 * Mathf.Sqrt(2 * gravity * height) / gravity;
            // 66% of theoretical level-ground travel: room for acceleration, edge clearance and a late press.
            stride = Mathf.Floor(speed * jumpSeconds * .66f * 10) / 10;
            if (stride < 3 || height < 2.2f) throw new InvalidOperationException("Movement changed; re-author the route before rebuilding.");
            cursor = forest ? 80 : 44;
            lastRouteX = forest ? 75.8f : 39.9f;
            encounterIndex = 0;
            route = new Route { scene = path, speed = speed, jumpHeight = height, gravity = gravity,
                jumpRange = speed * jumpSeconds, stride = stride,
                colliderWidth = player.GetComponent<CapsuleCollider2D>().bounds.size.x, extensionStart = cursor };
            var camera = Object.FindAnyObjectByType<Camera>();
            if (forest)
            {
                foreach (var obj in Object.FindObjectsByType<Transform>().Where(t => t.name == "Level boundary" && t.position.x > 0).ToArray())
                    Object.DestroyImmediate(obj.gameObject);
                var far = GameObject.Find("Far background · parallax");
                if (far != null) Object.DestroyImmediate(far);
                var mid = GameObject.Find("Midground trees");
                if (mid != null)
                    foreach (Transform child in mid.transform.Cast<Transform>().Where(t => t.name.StartsWith("Distant tree ")).ToArray())
                        Object.DestroyImmediate(child.gameObject);
            }

            string[] names = { "Вступление", "Первые ловушки", "Платформинг", "Обманчивая секция", "Усложнение", "Главное испытание", "Финиш" };
            Encounter[][] meadow = {
                new[] { R(3), P(2) },
                new[] { P(3), B(3), R(3), H(2), S(5), P(2) },
                new[] { R(5), S(5), B(3), R(5), S(3) },
                new[] { H(3), B(5), P(2), S(3), H(2), R(3) },
                new[] { S(5), P(3), B(5), H(2), R(5) },
                new[] { R(5), H(3), B(5), P(3), S(5) },
                new[] { R(3), P(2), B(3), S(3) }
            };
            Encounter[][] woodland = {
                new Encounter[0],
                new[] { F, S(5), T, R(3), A, S(5) },
                new[] { R(5), S(5), F, R(5), S(3) },
                new[] { A, T, S(5), G, F, R(5) },
                new[] { R(5), F, T, S(5), A, R(3) },
                new[] { S(5), T, A, R(5), G, S(5) },
                new[] { R(3), F, S(5), T, R(3) }
            };
            for (int i = 0; i < names.Length; i++)
            {
                sectionRoot = Group($"{i + 1:00} · {names[i]}", root);
                float start = i == 0 ? 0 : cursor;
                int firstCommand = route.commands.Count;
                foreach (var encounter in (forest ? woodland : meadow)[i]) BuildEncounter(encounter);
                // Four units of safe footing mark the transition; no waiting or artificial gates.
                Ground(cursor, cursor + 4); Walk(cursor + 3); cursor += 4;
                float estimate = Estimate(firstCommand) + (i == 0 ? (forest ? 41 : 21) : 2);
                route.sections.Add(new LevelCourse.Section { name = names[i], startX = start,
                    endX = cursor, estimatedSeconds = Mathf.Round(estimate * 10) / 10 });
            }

            Ground(cursor, cursor + 8);
            route.finishX = cursor + 3;
            Walk(route.finishX);
            var goal = forest ? (Component)Object.FindAnyObjectByType<ForestGoal>() : Object.FindAnyObjectByType<TutorialGoal>();
            goal.transform.position = new Vector3(route.finishX, forest ? 1 : .8f, 0);
            if (forest) GameObject.Find("Finish flag").transform.position = new Vector3(route.finishX - 1.4f, .85f, 0);
            float end = cursor + 8;
            var finalSection = route.sections[6]; finalSection.endX = end; route.sections[6] = finalSection;
            Wall(end + .5f);
            if (!forest) Wall(-4.5f);
            ConfigureCourse(end);
            if (forest) Set(camera.GetComponent<ForestCamera>(), "levelBounds", new Vector2(-4, end));
            else
            {
                var follow = camera.GetComponent<TutorialCamera>();
                Set(follow, "useBounds", true); Set(follow, "levelBounds", new Vector2(-4, end));
                Set(follow, "height", 3.4f); camera.orthographicSize = 6;
            }
            Environment(camera, end);
            Physics2D.SyncTransforms();
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new Exception("Could not save " + path);
            File.WriteAllText(Output + (forest ? "forest" : "meadow") + "-route.json", JsonUtility.ToJson(route, true));
            Debug.Log($"EXPANSION {path}: finish {route.finishX:F1}, range {route.jumpRange:F2}, stride {stride:F1}, estimated {route.sections.Sum(s => s.estimatedSeconds):F1}s");
        }

        private static void BuildEncounter(Encounter encounter)
        {
            encounterIndex++;
            var parent = sectionRoot;
            sectionRoot = Group($"{encounterIndex:00} · {encounter.kind} {encounter.count}", parent);
            switch (encounter.kind)
            {
                case Kind.Ridge: case Kind.Stones: case Kind.Bridge: Crossing(encounter); break;
                case Kind.Spikes: case Kind.Hidden: SpikeSequence(encounter); break;
                case Kind.FalseGround: case Kind.Bait: Concealed(encounter.kind == Kind.Bait); break;
                case Kind.Tree: Tree(); break;
                case Kind.Gate: Gate(); break;
            }
            sectionRoot = parent;
        }

        private static void Crossing(Encounter encounter)
        {
            float x = cursor;
            Ground(x, x + 4); Walk(x + 3);
            float first = x + 3 + stride;
            float bank = first + encounter.count * stride - 1.2f;
            Pit(x + 4, bank);
            for (int i = 0; i < encounter.count; i++)
            {
                float top = encounter.kind == Kind.Ridge ? Mathf.Min(i + 1, encounter.count - i, 3) :
                    encounter.kind == Kind.Stones ? (i % 2 == 0 ? .65f : 1.4f) : 0;
                float center = first + i * stride;
                bool unstable = encounter.kind == Kind.Bridge ? i % 3 != 1 :
                    forest && encounter.kind == Kind.Stones && i % 3 == 2;
                Platform(center, top, unstable);
                Hop(center);
            }
            Ground(bank, bank + 5.2f); Hop(bank + 1.2f); Walk(bank + 4);
            cursor = bank + 5.2f;
        }

        private static void SpikeSequence(Encounter encounter)
        {
            float spacing = stride + 2.4f;
            float end = cursor + 4 + (encounter.count - 1) * spacing + 4;
            Ground(cursor, end);
            for (int i = 0; i < encounter.count; i++)
            {
                float x = cursor + 4 + i * spacing;
                bool hidden = encounter.kind == Kind.Hidden && i % 2 == 0;
                var obj = Prefab("Traps/" + (hidden ? "HiddenTrap" : "SpikeTrap"),
                    new Vector2(hidden ? x - .7f : x, hidden ? .8f : .35f));
                obj.name = hidden ? "Surprise spikes" : "Visible spikes";
                foreach (var spike in obj.GetComponentsInChildren<SpikeTrap>(true)) DressSpike(spike);
                if (hidden) Art("Disturbed grass", "Grass", new Vector2(x, .13f), new Vector2(.9f, .38f), 2, sectionRoot);
                Walk(x - 1.7f); Hop(x + 1.7f);
            }
            Walk(end - 1); cursor = end;
        }

        private static void Concealed(bool bait)
        {
            float x = cursor;
            Ground(x, x + 5);
            var cover = Prefab("DeceptiveForest/FalseGround", new Vector2(x + 6.2f, -2));
            Set(cover.GetComponent<ForestDisappearingPlatform>(), "warningDuration", .38f);
            if (bait)
            {
                Set(cover.GetComponent<ForestDisappearingPlatform>(), "activateOnLanding", false);
                var trigger = new GameObject("Windfall apple trigger", typeof(CircleCollider2D), typeof(ForestBaitTrigger));
                trigger.transform.SetParent(cover.transform); trigger.transform.position = new Vector3(x + 6.2f, .7f, 0);
                trigger.GetComponent<CircleCollider2D>().isTrigger = true; trigger.GetComponent<CircleCollider2D>().radius = .8f;
                Set(trigger.GetComponent<ForestBaitTrigger>(), "surface", cover.GetComponent<ForestDisappearingPlatform>());
                Art("Windfall apple", "Apple", new Vector2(x + 6.2f, .23f), new Vector2(.38f, .4f), 3, cover.transform.Find("Surface"));
            }
            Ground(x + 7.4f, x + 12);
            Walk(x + 4.2f); Hop(x + 8.2f); Walk(x + 11);
            cursor += 12;
        }

        private static void Tree()
        {
            float x = cursor;
            Ground(x, x + 16);
            var tree = Prefab("DeceptiveForest/FallingTree", new Vector2(x + 10, 0)).GetComponent<ForestFallingTree>();
            var trigger = new GameObject("Tree proximity", typeof(BoxCollider2D), typeof(ForestTreeTrigger));
            trigger.transform.SetParent(sectionRoot); trigger.transform.position = new Vector3(x + 3.6f, 2, 0);
            trigger.GetComponent<BoxCollider2D>().isTrigger = true; trigger.GetComponent<BoxCollider2D>().size = new Vector2(.8f, 4);
            Set(trigger.GetComponent<ForestTreeTrigger>(), "tree", tree);
            Walk(x + 3); CommandAt("wait", 0, 1.3f); Hop(x + 6.5f); Walk(x + 9); Hop(x + 12); Walk(x + 15);
            cursor += 16;
        }

        private static void Gate()
        {
            float x = cursor;
            Ground(x, x + 13);
            var stick = Prefab("DeceptiveForest/RecoverableStick", new Vector2(x + 3, .65f)).GetComponent<ForestStick>();
            Set(stick, "player", player);
            Prefab("DeceptiveForest/StickBarrier", new Vector2(x + 8, 0));
            Walk(x + 2.8f); CommandAt("pickup"); Walk(x + 5.3f); CommandAt("throw");
            CommandAt("wait", 0, .6f); Walk(x + 12);
            Art("Stick stump", "Stump", new Vector2(x + 1.5f, .55f), new Vector2(1.3f, 1.1f), -3, sectionRoot);
            cursor += 13;
        }

        private static void Ground(float left, float right)
        {
            var obj = new GameObject("Safe bank", typeof(BoxCollider2D)); obj.layer = 6;
            obj.transform.SetParent(sectionRoot); obj.transform.position = new Vector3((left + right) / 2, -2, 0);
            obj.GetComponent<BoxCollider2D>().size = new Vector2(right - left, 4);
            var art = Art("Grass and soil", "Ground", new Vector2((left + right) / 2, -1.9f), Vector2.one, 0, obj.transform);
            art.transform.localScale = Vector3.one; art.drawMode = SpriteDrawMode.Tiled; art.size = new Vector2(right - left, 4);
            if (right - left < 4) return;
            float x = (left + right) / 2;
            Art("Flowers", encounterIndex % 2 == 0 ? "Flowers" : "Rocks", new Vector2(x, .18f), new Vector2(.8f, .4f), -2, obj.transform);
            var tree = Art("Framing tree", encounterIndex % 3 == 0 ? "Tree" : "Bush", new Vector2(x - .7f, encounterIndex % 3 == 0 ? 2.7f : .4f),
                encounterIndex % 3 == 0 ? new Vector2(5.4f, 5.4f) : new Vector2(1.4f, .8f), -12, obj.transform);
            if (forest) tree.color = new Color(.72f, .88f, .7f);
            Motion(tree.gameObject, HubAmbience.Motion.Sway, .3f);
        }

        private static void Platform(float x, float top, bool unstable)
        {
            if (unstable && forest)
            {
                var obj = Prefab("DeceptiveForest/DisappearingBranch", new Vector2(x, top - .225f));
                Set(obj.GetComponent<ForestDisappearingPlatform>(), "warningDuration", .5f);
                return;
            }
            if (unstable)
            {
                var obj = Prefab("Traps/FallingPlatform", new Vector2(x, top - .2f));
                obj.name = "Unstable bridge";
                obj.transform.localScale = new Vector3(2.4f, .4f, 1);
                obj.GetComponent<SpriteRenderer>().enabled = false;
                Art("Bridge artwork", "Bridge", new Vector2(x, top), new Vector2(2.4f, .8f), 2, obj.transform);
                return;
            }
            var safe = new GameObject("Stable stepping stone", typeof(BoxCollider2D)); safe.layer = 6;
            safe.transform.SetParent(sectionRoot); safe.transform.position = new Vector3(x, top - .225f, 0);
            safe.GetComponent<BoxCollider2D>().size = new Vector2(2.4f, .45f);
            Art("Branch artwork", "Island", new Vector2(x, top - .28f), new Vector2(2.4f, .85f), 2, safe.transform);
        }

        private static void Pit(float left, float right)
        {
            // Use the same reusable lethal prefab, sized to visible teeth at the bottom of the ravine.
            for (float x = left; x < right - .01f; x += 1.4f)
            {
                float width = Mathf.Min(1.4f, right - x);
                var obj = Prefab("Traps/SpikeTrap", new Vector2(x + width / 2, -2.5f));
                obj.name = "Ravine thorns";
                obj.transform.localScale = new Vector3(width, .65f, 1);
                DressSpike(obj.GetComponent<SpikeTrap>());
            }
        }

        private static void DressSpike(SpikeTrap spike)
        {
            var renderer = spike.GetComponent<SpriteRenderer>(); renderer.sprite = Meadow("Spikes"); renderer.color = Color.white;
            Vector2 size = renderer.sprite.bounds.size;
            Vector3 scale = spike.transform.localScale;
            spike.transform.localScale = new Vector3(scale.x / size.x, scale.y / size.y, 1);
            var hitbox = spike.GetComponent<BoxCollider2D>(); hitbox.size = Vector2.Scale(hitbox.size, size);
        }

        private static void Environment(Camera camera, float end)
        {
            var background = Group("Continuous parallax", root).gameObject;
            var backdrop = background.AddComponent<RepeatingLevelBackdrop>();
            Set(backdrop, "view", camera);
            var data = new SerializedObject(backdrop); var panels = data.FindProperty("panels"); panels.arraySize = 3;
            Sprite sprite = forest ? AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/DeceptiveForest/ForestBackdrop.png") : Meadow("Landscape");
            for (int i = 0; i < 3; i++)
            {
                var panel = new GameObject("Scenery panel " + i, typeof(SpriteRenderer)); panel.transform.SetParent(background.transform);
                var renderer = panel.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = -100;
                panels.GetArrayElementAtIndex(i).objectReferenceValue = renderer;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            for (float x = -24; x < end + 30; x += 8)
            {
                var tree = Art("Distant tree", "Tree", new Vector2(x, 3.7f), new Vector2(6.6f, 7.4f), -55, root);
                tree.color = forest ? new Color(.6f, .75f, .64f, .13f) : new Color(.75f, .9f, .85f, .15f);
                Motion(tree.gameObject, HubAmbience.Motion.Parallax, .22f, camera.transform);
            }
        }

        private static void ConfigureCourse(float end)
        {
            var course = root.GetComponent<LevelCourse>(); Set(course, "player", player.GetComponent<PlayerDeath>());
            Set(course, "worldBounds", new Vector2(-4, end));
            var data = new SerializedObject(course); var sections = data.FindProperty("sections"); sections.arraySize = route.sections.Count;
            for (int i = 0; i < route.sections.Count; i++)
            {
                var section = route.sections[i]; var prop = sections.GetArrayElementAtIndex(i);
                prop.FindPropertyRelative("name").stringValue = section.name;
                prop.FindPropertyRelative("startX").floatValue = section.startX;
                prop.FindPropertyRelative("endX").floatValue = section.endX;
                prop.FindPropertyRelative("estimatedSeconds").floatValue = section.estimatedSeconds;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float Estimate(int firstCommand)
        {
            // Conservative first-success model: real travel/flight + 0.9s reading/aiming per jump.
            // These allowances are hypotheses for human playtests, not forced waits or measured results.
            return route.commands.Skip(firstCommand).Sum(c => c.seconds);
        }
        private static void Walk(float x)
        {
            CommandAt("walk", x, Mathf.Abs(x - lastRouteX) / speed + .1f); lastRouteX = x;
        }
        private static void Hop(float x)
        {
            CommandAt("hop", x, jumpSeconds + 1f); lastRouteX = x;
        }
        private static void CommandAt(string action, float x = 0, float seconds = 1.5f) =>
            route.commands.Add(new Command { action = action, x = x, seconds = seconds, context = sectionRoot.name });
        private static void Wall(float x)
        {
            var obj = new GameObject("Level boundary", typeof(BoxCollider2D)); obj.transform.SetParent(root);
            obj.transform.position = new Vector3(x, 4, 0); obj.GetComponent<BoxCollider2D>().size = new Vector2(1, 14);
        }
        private static Transform Group(string name, Transform parent)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj.transform;
        }
        private static Sprite Meadow(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Resources/AppleMeadow/" + name + ".png");
        private static SpriteRenderer Art(string name, string spriteName, Vector2 position, Vector2 size, int order, Transform parent)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer)); obj.transform.SetParent(parent); obj.transform.position = position;
            var renderer = obj.GetComponent<SpriteRenderer>(); renderer.sprite = Meadow(spriteName); renderer.sortingOrder = order;
            if (renderer.sprite == null) throw new InvalidOperationException("Missing art " + spriteName);
            Vector3 scale = parent.lossyScale;
            obj.transform.localScale = new Vector3(size.x / renderer.sprite.bounds.size.x / scale.x,
                size.y / renderer.sprite.bounds.size.y / scale.y, 1);
            return renderer;
        }
        private static GameObject Prefab(string name, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/" + name + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing prefab " + name);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, sectionRoot); obj.transform.position = position; return obj;
        }
        private static void Motion(GameObject obj, HubAmbience.Motion motion, float amplitude, Transform camera = null)
        {
            var component = obj.AddComponent<HubAmbience>(); Set(component, "motion", (int)motion);
            Set(component, "amplitude", amplitude); Set(component, "speed", .55f);
            if (camera != null) Set(component, "cameraTransform", camera);
        }
        private static void Set(Object obj, string name, object value)
        {
            var data = new SerializedObject(obj); var property = data.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Missing property " + name);
            if (value is Object reference) property.objectReferenceValue = reference;
            else if (value is Vector2 vector) property.vector2Value = vector;
            else if (value is float number) property.floatValue = number;
            else if (value is int integer) property.intValue = integer;
            else if (value is bool boolean) property.boolValue = boolean;
            else throw new InvalidOperationException("Unsupported property " + name);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
