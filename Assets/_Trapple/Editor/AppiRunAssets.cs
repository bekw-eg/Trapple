using System;
using System.IO;
using System.Linq;
using Trapple.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.Editor
{
    // Clips and the existing PlayerSpriteAnimator share one authored sprite sequence.
    // No second animation driver or changes to the movement controller are needed.
    public static class AppiRunAssets
    {
        public const string SheetPath = "Assets/_Trapple/Art/Hub/AppiRun.png";
        public const string ClipsPath = "Assets/_Trapple/Animations";

        [MenuItem("Tools/Trapple/Reimport Appi Run")]
        public static void Reimport()
        {
            PixelPlayerAssets.Import();
            AssetDatabase.SaveAssets();
            Debug.Log("Appi: eight frames per direction, 12 FPS looping run clips imported.");
        }

        // Batch validation renders the imported Unity sprites, without saving a scene.
        public static void CaptureFrames()
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(PixelPlayerAssets.SetPath);
            var scene = EditorSceneManager.NewPreviewScene();
            var target = new RenderTexture(1536, 512, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(1536, 512, TextureFormat.RGBA32, false);
            try
            {
                var cameraObject = new GameObject("Appi validation camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 2.048f;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.5f, .58f, .65f);
                camera.targetTexture = target;
                var frames = set.walkRight.Concat(set.walkLeft).ToArray();
                for (int i = 0; i < frames.Length; i++)
                {
                    var actor = new GameObject(frames[i].name);
                    SceneManager.MoveGameObjectToScene(actor, scene);
                    actor.transform.position = new Vector3((i % 8 - 3.5f) * 1.5f, i < 8 ? .8f : -.8f, 0);
                    actor.AddComponent<SpriteRenderer>().sprite = frames[i];
                }
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1536, 512), 0, 0);
                image.Apply();
                const string output = "Docs/Images/AppiRun/UnityFrames.png";
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, image.EncodeToPNG());
                Debug.Log("Rendered imported Appi sprites: " + output);
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        internal static void Import(PlayerAnimationSet set)
        {
            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            if (importer == null) throw new InvalidOperationException("Missing Appi run sheet.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
#pragma warning disable 618
            importer.spritesheet = Enumerable.Range(0, 16).Select(index => new SpriteMetaData
            {
                name = (index < 8 ? "AppiRunRight" : "AppiRunLeft") + index % 8,
                rect = new Rect(index % 8 * 144, index < 8 ? 156 : 0, 144, 156),
                alignment = (int)SpriteAlignment.Custom,
                // Shared baseline at y=147; keep the existing capsule's foot offset.
                pivot = new Vector2(.5f, (9f + .475f * 128f) / 156f)
            }).ToArray();
#pragma warning restore 618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
            set.walkRight = Frames(sprites, "AppiRunRight");
            set.walkLeft = Frames(sprites, "AppiRunLeft");
            set.walkFramesPerSecond = 12f;
            if (!AssetDatabase.IsValidFolder(ClipsPath))
                AssetDatabase.CreateFolder("Assets/_Trapple", "Animations");
            set.runRightClip = Clip("Appi_Run_Right", set.walkRight, 12f);
            set.runLeftClip = Clip("Appi_Run_Left", set.walkLeft, 12f);
            set.idleClip = Clip("Appi_Idle", set.idle, set.idleFramesPerSecond);
            EditorUtility.SetDirty(set);
        }

        private static Sprite[] Frames(Sprite[] sprites, string prefix) =>
            Enumerable.Range(0, 8).Select(index => sprites.Single(sprite => sprite.name == prefix + index)).ToArray();

        private static AnimationClip Clip(string name, Sprite[] frames, float fps)
        {
            string path = ClipsPath + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = fps;
            clip.wrapMode = WrapMode.Loop;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            // Unity holds the final sprite for one sample. A duplicate closing key
            // would add a ninth frame and visibly pause at the loop boundary.
            var keys = Enumerable.Range(0, frames.Length).Select(index => new ObjectReferenceKeyframe
            { time = index / fps, value = frames[index] }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }
    }
}
