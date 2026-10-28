using System;
using TMPro;
using Trapple.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    // Only patches the shared UI prefab. Open scenes and existing button assets are untouched.
    public static class DeathScreenUIBuilder
    {
        [MenuItem("Tools/Trapple/Install death screen")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var root = PrefabUtility.LoadPrefabContents(StorybookUIBuilder.InterfacePath);
            try
            {
                AddTo(root);
                PrefabUtility.SaveAsPrefabAsset(root, StorybookUIBuilder.InterfacePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("DEATH SCREEN INSTALLED");
        }

        public static void AddTo(GameObject root)
        {
            foreach (var previous in root.GetComponentsInChildren<DeathScreenUI>(true))
                Object.DestroyImmediate(previous.gameObject);
            var overlay = Rect("Death screen", root.transform, new Vector2(1920, 1080), Vector2.zero);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            var shade = overlay.gameObject.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, .64f);
            shade.raycastTarget = true;
            var canvas = overlay.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 300;
            overlay.gameObject.AddComponent<GraphicRaycaster>();
            var view = overlay.gameObject.AddComponent<DeathScreenUI>();
            var content = Rect("Centered content", overlay, new Vector2(860, 580), Vector2.zero);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StorybookUIBuilder.Root + "Fonts/Thaleah SDF.asset");
            if (font == null) throw new InvalidOperationException("Existing Trapple font is missing.");
            Label("Title", content, new Vector2(860, 140), new Vector2(0, 220), "YOU DIED!", 128, font);
            var score = Label("Score", content, new Vector2(860, 60), new Vector2(0, 115), "Score: 0", 52, font);
            var respawn = Button(content, new Vector2(0, -45), "Respawn");
            var hub = Button(content, new Vector2(0, -205), "Go to the hub");
            respawn.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = hub, selectOnUp = hub };
            hub.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = respawn, selectOnUp = respawn };
            view.Configure(score, respawn, hub);
            root.GetComponent<GameInterface>().SetDeathScreen(view);
            overlay.gameObject.SetActive(false);
        }

        private static Button Button(Transform parent, Vector2 position, string caption)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(StorybookUIBuilder.Root + "Prefabs/PrimaryButton.prefab");
            if (source == null) throw new InvalidOperationException("Existing Trapple button prefab is missing.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = caption;
            var rect = (RectTransform)instance.transform;
            Center(rect, new Vector2(560, 140), position);
            var label = instance.GetComponentInChildren<TMP_Text>();
            label.text = caption;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(60, 12);
            label.rectTransform.offsetMax = new Vector2(-60, -12);
            label.fontSize = label.fontSizeMax = 50;
            label.fontSizeMin = 32;
            label.enableAutoSizing = true;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return instance.GetComponent<Button>();
        }

        private static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 position,
            string caption, float fontSize, TMP_FontAsset font)
        {
            var rect = Rect(name, parent, size, position);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = caption;
            label.fontSize = fontSize;
            label.color = new Color(1f, .93f, .69f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Center(rect, size, position);
            return rect;
        }

        private static void Center(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }
    }
}
