using System;
using System.IO;
using System.Linq;
using Trapple.Hub;
using Trapple.Workshop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Trapple.Editor
{
    public static class WorkshopDoorAssets
    {
        public const string SourcePath="Assets/_Trapple/Art/Workshop/Source/WorkshopDoor8Frames.png";
        public const string MaterialPath="Assets/_Trapple/Art/Workshop/WorkshopDoor.mat";
        public const int FrameCount=8;

        internal static Sprite[] Import()
        {
            AssetDatabase.ImportAsset(SourcePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(SourcePath);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
            if(texture==null || texture.width!=1983 || texture.height!=793)
                throw new InvalidOperationException("Expected the supplied 1983 x 793 door sheet.");
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=100;
            var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=2048;
            importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
#pragma warning disable 618
            // The divider lies at y=384. Bottom padding is not another frame. Insets exclude grid lines.
            importer.spritesheet=Enumerable.Range(0,FrameCount).Select(index=>new SpriteMetaData
            {
                name="WorkshopDoor"+(index+1).ToString("00"),
                rect=new Rect(4+(index%4)*496,texture.height-4-(index/4)*384-376,488,376),
                alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)
            }).ToArray();
#pragma warning restore 618
            importer.SaveAndReimport();
            var frames=AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Sprite>().OrderBy(sprite=>sprite.name,StringComparer.Ordinal).ToArray();
            if(frames.Length!=FrameCount) throw new InvalidOperationException("Door sheet must contain eight ordered sprites.");
            return frames;
        }

        internal static Material Material()
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material!=null) return material;
            var shader=Shader.Find("Trapple/Sprites/Workshop Door");
            if(shader==null) throw new InvalidOperationException("Workshop door shader is missing.");
            material=new Material(shader); AssetDatabase.CreateAsset(material,MaterialPath); return material;
        }

        public static void BuildAndCapture()
        {
            WorkshopBuilder.Build(); WorkshopBuilder.Build();
            EditorSceneManager.OpenScene(HubSceneFlowController.HubScenePath);
            var door=UnityEngine.Object.FindAnyObjectByType<WorkshopDoorAnimator>();
            if(door.FrameCount!=8) throw new InvalidOperationException("Eight door frames were not installed.");
            var camera=UnityEngine.Object.FindAnyObjectByType<HubCameraController>().GetComponent<Camera>();
            camera.transform.position=new Vector3(-8,2.45f,-10); camera.orthographicSize=3;
            Directory.CreateDirectory("Builds/WorkshopDoor/Frames");
            var target=new RenderTexture(720,600,24);
            var previousTarget=camera.targetTexture; var previousActive=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                for(int index=0;index<8;index++)
                {
                    door.Set(index/7f); camera.Render(); RenderTexture.active=target;
                    var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                    File.WriteAllBytes("Builds/WorkshopDoor/Frames/frame-"+(index+1).ToString("00")+".png",image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
            finally
            {
                door.Set(0); camera.targetTexture=previousTarget; RenderTexture.active=previousActive;
                target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
            // Inspection above is transient; saved scenes keep the closed frame.
            Debug.Log("WORKSHOP DOOR CAPTURE PASS: eight ordered frames rendered; rebuild executed twice.");
        }
    }
}
