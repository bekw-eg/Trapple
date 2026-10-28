using System;
using Trapple.Bosses;
using UnityEditor;
using UnityEngine;

namespace Trapple.Editor
{
    /// <summary>Upgrades reusable boss prefabs without rebuilding routes or scene object identifiers.</summary>
    public static class GuardianVarietyBuilder
    {
        private const string Root = "Assets/_Trapple/";
        public const string BranchPath = Root + "Art/GuardianAttacks/GiantBranch.png";
        private const string BranchSpritePath = Root + "Art/GuardianAttacks/GiantBranch.asset";

        [MenuItem("Trapple/Boss progression/Upgrade both boss attack sets")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(BranchPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Generated GiantBranch.png is required.");
            importer.textureType = TextureImporterType.Default; importer.isReadable = true;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BranchPath);
            var pixels = texture.GetPixels32(); int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
            for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a > 8)
                { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
            var sprite = Sprite.Create(texture, new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1),
                new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.Tight); sprite.name = "Giant falling branch";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(BranchSpritePath);
            if (existing == null) AssetDatabase.CreateAsset(sprite, BranchSpritePath);
            else { EditorUtility.CopySerialized(sprite, existing); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(existing); }
            Upgrade(Root + "Prefabs/ForestGuardian/ForestGuardian.prefab");
            Upgrade(Root + "Prefabs/OrchardWarden/OrchardWarden.prefab");
            AssetDatabase.SaveAssets(); Debug.Log("GUARDIAN VARIETY: both reusable prefabs upgraded; existing scenes preserved.");
        }

        public static void Configure(GameObject obj)
        {
            var boss = obj.GetComponent<ForestGuardianController>(); var rig = obj.GetComponent<ForestGuardianAnimation>();
            var body = obj.transform.Find("Animated torso");
            var left = FindArt(body, "Left branch fist"); var right = FindArt(body, "Right branch fist");
            Set(rig, "leftArm", Shoulder(body, left, new Vector2(-1.6f, 4.5f)));
            Set(rig, "rightArm", Shoulder(body, right, new Vector2(1.6f, 4.5f)));
            Set(rig, "crown", body.Find("Small leafy crown")); Set(rig, "articulatedArms", true);
            var effects = obj.GetComponent<GuardianAttackEffects>() ?? obj.AddComponent<GuardianAttackEffects>();
            Set(effects, "dustSprite", Load(Root + "Art/ForestGuardian/ContactShadow.png"));
            Set(effects, "debrisSprite", Load(Root + "Resources/AppleMeadow/Rocks.png"));
            var data = new SerializedObject(boss);
            Sprite ReadSprite(string field) => data.FindProperty(field).objectReferenceValue as Sprite;
            Set(effects, "leafSprite", ReadSprite("leafSprite")); Set(boss, "attackEffects", effects);
            var special = obj.GetComponent<GuardianSpecialAttacks>() ?? obj.AddComponent<GuardianSpecialAttacks>();
            Set(special, "giantBranch", Load(BranchSpritePath)); Set(special, "apple", Load(Root + "Resources/AppleMeadow/Apple.png"));
            Set(special, "root", ReadSprite("rootSprite"));
            // Gold separates damaging fan leaves from the forest's green ambient foliage.
            Set(special, "leaf", Load(Root + "Art/OrchardWarden/GoldenLeaf.asset"));
            Set(special, "markerSprite", Load(Root + "Art/Prototype/Square.png"));
            Set(special, "rainBranchCount", 50); Set(boss, "specialAttacks", special);
        }

        private static void Upgrade(string path)
        {
            var obj = PrefabUtility.LoadPrefabContents(path);
            try { Configure(obj); PrefabUtility.SaveAsPrefabAsset(obj, path); }
            finally { PrefabUtility.UnloadPrefabContents(obj); }
        }

        internal static Transform FindArt(Transform body, string name)
        {
            foreach (var child in body.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            throw new InvalidOperationException("Missing guardian artwork: " + name);
        }
        internal static Transform Shoulder(Transform body, Transform artwork, Vector2 anchor)
        {
            if (artwork.parent != body) return artwork.parent;
            var joint = new GameObject(artwork.name + " shoulder").transform;
            joint.SetParent(body, false); joint.localPosition = anchor; artwork.SetParent(joint, true); return joint;
        }
        private static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        private static void Set(UnityEngine.Object target, string field, object value) => ForestGuardianBuilder.Set(target, field, value);
    }
}
