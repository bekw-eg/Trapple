using System;
using System.Linq;
using Trapple.Player;
using UnityEditor;
using UnityEngine;

namespace Trapple.Editor
{
    // Keep the supplied PNG unchanged; exclude its drawn floor strips when slicing.
    // Feet anchors stay fixed when a rectangle is shortened, preserving the existing capsule alignment.
    internal static class PixelPlayerAssets
    {
        internal const string SourcePath = "Assets/_Trapple/Art/Hub/Source/AppiAnimation.png";
        internal const string SetPath = "Assets/_Trapple/Settings/AppiAnimations.asset";
        private const float PixelsPerUnit = 128f;

        [MenuItem("Tools/Trapple/Reimport Appi Animation")]
        public static void Reimport()
        {
            Import();
            AssetDatabase.SaveAssets();
            Debug.Log("Appi idle/jump and eight-frame run animations imported.");
        }

        internal static PlayerAnimationSet Import()
        {
            HubArtBuilder.Import(SourcePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SourcePath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
            int height = texture.height;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();
#pragma warning disable 618
            importer.spritesheet = new[]
            {
                Frame("AppiIdle0", height, 296,304,154,140,374,443),
                Frame("AppiIdle1", height, 496,304,154,140,574,443),
                Frame("AppiIdle2", height, 728,304,154,140,806,443),
                Frame("AppiIdle3", height, 958,304,154,140,1036,443),
                Frame("AppiWalkRight0", height, 310,474,145,123,385,596),
                Frame("AppiWalkRight1", height, 496,474,145,123,569,596),
                Frame("AppiWalkRight2", height, 684,474,145,123,757,596),
                Frame("AppiWalkRight3", height, 870,474,145,123,943,596),
                Frame("AppiWalkRight4", height, 1055,474,145,123,1128,596),
                Frame("AppiWalkRight5", height, 1243,474,145,123,1316,596),
                Frame("AppiWalkLeft0", height, 310,629,145,122,383,749),
                Frame("AppiWalkLeft1", height, 496,629,145,122,569,749),
                Frame("AppiWalkLeft2", height, 684,629,145,122,757,749),
                Frame("AppiWalkLeft3", height, 870,629,145,122,943,749),
                Frame("AppiWalkLeft4", height, 1055,629,145,130,1128,760),
                Frame("AppiWalkLeft5", height, 1243,629,145,124,1316,749),
                Frame("AppiJump0", height, 280,778,183,136,392,918),
                Frame("AppiJump1", height, 531,780,165,130,641,890),
                Frame("AppiJump2", height, 807,778,143,131,887,891),
                Frame("AppiJump3", height, 1008,794,176,125,1105,921)
            };
#pragma warning restore 618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Sprite>().ToArray();
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(SetPath);
            if (set == null) { set = ScriptableObject.CreateInstance<PlayerAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
            set.idle = Frames(sprites, "AppiIdle", 4);
            set.walkingRight = Frames(sprites, "AppiWalkRight", 6);
            set.walkingLeft = Frames(sprites, "AppiWalkLeft", 6);
            set.jump = Frames(sprites, "AppiJump", 4);
            AppiRunAssets.Import(set);
            EditorUtility.SetDirty(set);
            ApplyPlayerPrefab(set);
            return set;
        }

        private static SpriteMetaData Frame(string name, int imageHeight, int x, int top, int width, int height, int centreX, int feetY)
        {
            float pivotY = (top + height - feetY + 0.475f * PixelsPerUnit) / height;
            return new SpriteMetaData { name = name, rect = new Rect(x,imageHeight-top-height,width,height),
                alignment = (int)SpriteAlignment.Custom, pivot = new Vector2((centreX-x)/(float)width,pivotY) };
        }

        private static Sprite[] Frames(Sprite[] sprites, string prefix, int count) =>
            Enumerable.Range(0,count).Select(index => sprites.First(sprite => sprite.name == prefix+index)).ToArray();

        private static void ApplyPlayerPrefab(PlayerAnimationSet set)
        {
            const string path = "Assets/_Trapple/Prefabs/Player/Player.prefab";
            var player = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer = player.GetComponent<SpriteRenderer>();
                renderer.sprite = set.idle[0]; renderer.color = Color.white;
                var animator = player.GetComponent<PlayerSpriteAnimator>();
                if (animator == null) animator = player.AddComponent<PlayerSpriteAnimator>();
                var serialized = new SerializedObject(animator);
                serialized.FindProperty("animations").objectReferenceValue = set;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
        }
    }
}
