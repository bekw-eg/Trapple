using System;
using System.IO;
using System.Linq;
using Trapple.Workshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Trapple.Editor
{
    public static class WorkshopDoorAssets
    {
        public const string Root="Assets/_Trapple/Art/Workshop/Door/";
        public const string HousePath=Root+"house_empty_doorway.png";
        public const string SheetPath=Root+"door_spritesheet_3x3.png";
        public const string CutoutPath=Root+"cottage_cutout.png";
        public const string MaterialPath=Root+"CottageCutout.mat";
        public const int FrameCount=9;
        public const float PixelsPerUnit=100;
        public static readonly Vector2 OverlayOffsetPixels=new Vector2(-103.5f,-84.5f);

        internal static Sprite[] Import()
        {
            ConfigureTexture(HousePath,469,347);
            ImportCutout();
            var frames=new Sprite[FrameCount];
            for(int index=0;index<FrameCount;index++)
            {
                string path=Root+"doors_png/door_"+index.ToString("00")+".png";
                ConfigureTexture(path,80,120);
                frames[index]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(frames[index]==null) throw new InvalidOperationException("Missing door sprite: "+path);
            }
            // The supplied sheet is an alternative import. Runtime uses the independent PNGs.
            var sheet=ConfigureTexture(SheetPath,240,360,SpriteImportMode.Multiple);
#pragma warning disable 618
            sheet.spritesheet=Enumerable.Range(0,FrameCount).Select(index=>new SpriteMetaData
            {
                name="door_"+index.ToString("00"),rect=new Rect(index%3*80,(2-index/3)*120,80,120),
                alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)
            }).ToArray();
#pragma warning restore 618
            EditorUtility.SetDirty(sheet);
            sheet.SaveAndReimport();
            var sheetFrames=AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(sprite=>sprite.name,StringComparer.Ordinal).ToArray();
            if(sheetFrames.Length!=FrameCount || sheetFrames.Where((sprite,index)=>sprite.name!="door_"+index.ToString("00")).Any())
                throw new InvalidOperationException("Door sheet must contain nine ordered 80 x 120 sprites.");
            return frames;
        }

        private static void ImportCutout()
        {
            AssetDatabase.ImportAsset(CutoutPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(CutoutPath);
            if(importer==null) throw new InvalidOperationException("Cottage exterior cutout is missing.");
            importer.textureType=TextureImporterType.Default;
            importer.filterMode=FilterMode.Point; importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        internal static Material CottageMaterial()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material==null)
            {
                var shader=Shader.Find("Trapple/Sprites/Cottage Cutout");
                if(shader==null) throw new InvalidOperationException("Cottage cutout shader is missing.");
                material=new Material(shader); AssetDatabase.CreateAsset(material,MaterialPath);
            }
            material.SetTexture("_CutoutTex",AssetDatabase.LoadAssetAtPath<Texture2D>(CutoutPath));
            material.SetFloat("_CutoutThreshold",.5f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static TextureImporter ConfigureTexture(string path,int width,int height,SpriteImportMode mode=SpriteImportMode.Single)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null || texture.width!=width || texture.height!=height)
                throw new InvalidOperationException("Unexpected supplied image dimensions: "+path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=mode;
            importer.spritePixelsPerUnit=PixelsPerUnit;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;
            settings.spriteAlignment=(int)SpriteAlignment.Center; settings.spritePivot=new Vector2(.5f,.5f);
            importer.SetTextureSettings(settings);
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
            importer.filterMode=FilterMode.Point; importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return importer;
        }

        public static void BuildAndCapture()
        {
            WorkshopBuilder.Build(); WorkshopBuilder.Build();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WorkshopBuilder.PrefabPath));
            var door=root.GetComponentInChildren<WorkshopDoorAnimator>();
            if(door.FrameCount!=FrameCount) throw new InvalidOperationException("Nine door overlays were not installed.");
            var cottage=door.Presentation.transform.parent.GetComponent<SpriteRenderer>();
            cottage.transform.localScale=Vector3.one;
            var camera=new GameObject("Door validation camera").AddComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=347/(2*PixelsPerUnit);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
            camera.transform.position=cottage.transform.position+new Vector3(0,0,-10);
            const string output="Builds/WorkshopDoorOverlays/Frames/";
            Directory.CreateDirectory(output);
            var target=new RenderTexture(469,347,24);
            var previousActive=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                for(int index=0;index<FrameCount;index++)
                {
                    door.Set(index/(float)(FrameCount-1)); camera.Render(); RenderTexture.active=target;
                    var image=new Texture2D(469,347,TextureFormat.RGBA32,false);
                    image.ReadPixels(new Rect(0,0,469,347),0,0); image.Apply();
                    File.WriteAllBytes(output+"cottage_"+index.ToString("00")+".png",image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            finally
            {
                door.Set(0); camera.targetTexture=null; RenderTexture.active=previousActive;
                target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
            Debug.Log("WORKSHOP DOOR OVERLAYS CAPTURE PASS: nine ordered overlays rendered; rebuild executed twice.");
        }
    }
}
