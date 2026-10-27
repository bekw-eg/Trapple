using Trapple.Core;
using Trapple.Traps;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Level
{
    /// <summary>Visual dressing for the existing tutorial; collision and trap timing remain scene-owned.</summary>
    [DisallowMultipleComponent]
    public sealed class AppleMeadowPresentation : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitialScene() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != GameManager.TutorialScene) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<AppleMeadowPresentation>() != null) return;
            var dressing = new GameObject("Apple Meadow Art");
            SceneManager.MoveGameObjectToScene(dressing, scene);
            dressing.AddComponent<AppleMeadowPresentation>();
        }

        private void Awake()
        {
            foreach (var collider in FindObjectsByType<BoxCollider2D>())
            {
                if (collider.gameObject.scene != gameObject.scene || collider.gameObject.layer != 6 ||
                    collider.GetComponent<FallingPlatform>() != null || collider.isTrigger ||
                    collider.GetComponentInParent<LevelCourse>() != null) continue;
                Bounds bounds = collider.bounds;
                var original = collider.GetComponent<SpriteRenderer>();
                if (original != null) original.enabled = false;
                // One continuous surface per collider: the grass cap stays at the
                // walking edge while only the soil fills the deeper land below.
                float depth = bounds.size.x > 6f ? 4f : Mathf.Max(.65f, bounds.size.y + .15f);
                var ground = Visual("Ground", new Vector2(bounds.center.x, bounds.max.y + .1f - depth * .5f),
                    Vector2.one, 1, collider.transform);
                Vector3 parentScale = collider.transform.lossyScale;
                ground.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);
                ground.drawMode = SpriteDrawMode.Tiled;
                ground.size = new Vector2(bounds.size.x, depth);
            }

            foreach (var bridge in FindObjectsByType<FallingPlatform>())
            {
                if (bridge.GetComponentInParent<LevelCourse>() != null) continue;
                bridge.GetComponent<SpriteRenderer>().enabled = false;
                Visual("Bridge", (Vector2)bridge.transform.position + Vector2.up * .2f,
                    new Vector2(4f, 1.3f), 3, bridge.transform);
            }
            foreach (var spike in FindObjectsByType<SpikeTrap>(FindObjectsInactive.Include))
            {
                if (spike.gameObject.scene != gameObject.scene || spike.GetComponentInParent<LevelCourse>() != null) continue;
                var renderer = spike.GetComponent<SpriteRenderer>();
                renderer.sprite = Resources.Load<Sprite>("AppleMeadow/Spikes");
                renderer.color = Color.white;
                // Match the existing trigger rather than extending the lethal area with decorative grass.
                renderer.drawMode = SpriteDrawMode.Simple;
                Vector3 scale = spike.transform.localScale;
                Vector2 size = renderer.sprite.bounds.size;
                spike.transform.localScale = new Vector3(scale.x / size.x, scale.y / size.y, 1f);
                var hitbox = spike.GetComponent<BoxCollider2D>();
                hitbox.size = Vector2.Scale(hitbox.size, size);
            }
            var goal = FindAnyObjectByType<TutorialGoal>();
            if (goal != null)
            {
                var renderer = goal.GetComponent<SpriteRenderer>();
                renderer.sprite = Resources.Load<Sprite>("AppleMeadow/Apple");
                renderer.color = Color.white;
                renderer.drawMode = SpriteDrawMode.Simple;
                // Use a child for art sizing so the apple's trigger keeps its original dimensions.
                renderer.enabled = false;
                var art = Visual("Apple", goal.transform.position, Vector2.one, 5, goal.transform);
                goal.SetArtwork(art);
            }

            foreach (float x in new[] { -2.5f, 5f, 17f, 28f, 37.5f, 43f })
                Visual("Tree", new Vector2(x, 2.15f), new Vector2(4.5f, 4.4f), -5);
            foreach (float x in new[] { -3f, 1f, 6f, 15f, 19f, 24f, 28f, 35f, 42f })
            {
                Visual("Bush", new Vector2(x, .37f), new Vector2(1.25f, .85f), -3);
                Visual("Grass", new Vector2(x + .8f, .13f), new Vector2(.9f, .46f), 2);
                Visual("Flowers", new Vector2(x - .65f, .18f), new Vector2(.8f, .46f), 2);
            }
            Visual("Crate", new Vector2(3.5f, .4f), new Vector2(1.4f, .85f), 4);
            Visual("Stump", new Vector2(18f, .4f), new Vector2(1.1f, .85f), -2);
            Visual("Rocks", new Vector2(36f, .3f), new Vector2(.9f, .62f), -2);
            Visual("Sign", new Vector2(1.2f, .67f), new Vector2(.85f, 1.15f), -2);
            Visual("Warning", new Vector2(9f, .6f), new Vector2(.85f, 1.1f), -2);
            if (goal != null) Visual("Flag", new Vector2(goal.transform.position.x + 1.4f, .85f), new Vector2(1.4f, 1.5f), -2);

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = new Color(.29f, .73f, .95f);
                if (FindAnyObjectByType<LevelCourse>() != null) return;
                var sky = Visual("Landscape", Vector2.zero, new Vector2(34f, 12.8f), -100);
                sky.transform.SetParent(camera.transform, false);
                sky.transform.localPosition = new Vector3(0f, .8f, 20f);
            }
        }

        private SpriteRenderer Visual(string sprite, Vector2 position, Vector2 size, int order, Transform parent = null)
        {
            var obj = new GameObject(sprite);
            obj.transform.SetParent(parent != null ? parent : transform, false);
            obj.transform.position = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("AppleMeadow/" + sprite);
            renderer.sortingOrder = order;
            Vector2 bounds = renderer.sprite.bounds.size;
            Vector3 parentScale = obj.transform.parent.lossyScale;
            obj.transform.localScale = new Vector3(size.x / bounds.x / parentScale.x,
                size.y / bounds.y / parentScale.y, 1f);
            return renderer;
        }
    }
}
