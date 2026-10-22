using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Trapple.Editor
{
    // Small original support sprites. Hero props/backdrop are authored image assets.
    internal static class HubArtBuilder
    {
        internal const string Root = "Assets/_Trapple/Art/Hub/";
        internal static void BuildSupportArt()
        {
            Write("Cloud", 512, 160, (x, y) =>
            {
                float a = Mathf.Max(Ellipse(x,y,.32f,.45f,.26f,.26f), Ellipse(x,y,.57f,.57f,.22f,.38f));
                a = Mathf.Max(a, Ellipse(x,y,.73f,.39f,.23f,.24f));
                return new Color(1f, .96f, .82f, Mathf.Clamp01(a * 4f) * .25f);
            });
            Write("Hill", 1024, 256, (x,y) =>
            {
                float edge = .64f + Mathf.Sin(x * 7f) * .13f + Mathf.Sin(x * 17f) * .04f;
                return new Color(.39f,.53f,.35f,Mathf.Clamp01((edge-y)*160f));
            });
            Write("Grass", 128, 128, (x,y) =>
            {
                float a = 0;
                for (int i=0; i<7; i++)
                {
                    float height = .45f + Mathf.Sin(i*13f)*.25f;
                    float center = .19f + i*.1f + y*y*(i-3)*.15f;
                    if (y<height) a = Mathf.Max(a, Mathf.Clamp01((.026f*(1-y/height)-Mathf.Abs(x-center))*160f));
                }
                return new Color(.21f,.39f,.24f,a);
            });
            Write("Leaf", 128, 64, (x,y) =>
            {
                float d = Mathf.Abs(y-.5f) + Mathf.Pow(Mathf.Abs(x-.5f)*1.9f, 1.5f);
                return new Color(.66f,.72f,.32f,Mathf.Clamp01((.48f-d)*80f));
            });
            Write("Glow", 256, 256, (x,y) => new Color(1f,.86f,.45f,
                Mathf.Pow(Mathf.Clamp01(1f-Vector2.Distance(new Vector2(x,y),Vector2.one*.5f)*2f), 2f)*.65f));
            Write("Ground", 512, 128, (x,y) =>
            {
                float grain = Mathf.Sin(x*1921f+y*1313f)*.005f;
                Color c = Color.Lerp(new Color(.14f,.26f,.19f),new Color(.35f,.44f,.25f),y);
                if (y>.88f) c = Color.Lerp(new Color(.38f,.48f,.27f),new Color(.61f,.65f,.35f),(y-.88f)/.12f);
                return c + new Color(grain,grain,grain,0);
            });
        }

        private static float Ellipse(float x,float y,float cx,float cy,float rx,float ry) =>
            1f - Mathf.Pow((x-cx)/rx,2f) - Mathf.Pow((y-cy)/ry,2f);

        private static void Write(string name,int width,int height,Func<float,float,Color> pixel)
        {
            var texture = new Texture2D(width,height,TextureFormat.RGBA32,false);
            var pixels=new Color[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++) pixels[y*width+x]=pixel((x+.5f)/width,(y+.5f)/height);
            texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(Root+name+".png",texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        internal static void Import(string path, bool sprite = true)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spritePixelsPerUnit=128f;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
            importer.maxTextureSize=4096;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        internal static void ImportProps()
        {
            string path=Root+"HubProps.png";
            Import(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.isReadable=true;
            importer.SaveAndReimport();
            var source=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var pixels=source.GetPixels32();
            var columns=new List<Vector2Int>();
            int beginning=-1,gap=0;
            for(int x=0;x<source.width;x++)
            {
                int count=0;
                for(int y=0;y<source.height;y++) if(pixels[y*source.width+x].a>=32) count++;
                if(count>=6) { if(beginning<0) beginning=x; gap=0; }
                else if(beginning>=0 && ++gap>Mathf.Max(3,source.height/96))
                {
                    if(x-gap-beginning>source.height/5) columns.Add(new Vector2Int(beginning,x-gap+1));
                    beginning=-1; gap=0;
                }
            }
            if(beginning>=0) columns.Add(new Vector2Int(beginning,source.width));
            if(columns.Count!=3) throw new InvalidOperationException("Expected three isolated hub props, found "+columns.Count);
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.isReadable=false;
            // Unity 6 auto-slices on the first switch to Multiple. Apply our named slices afterwards.
            importer.SaveAndReimport();
#pragma warning disable 618
            importer.spritesheet=new[] {
                Slice("Workshop",pixels,source.width,source.height,columns[0].x,columns[0].y),
                Slice("Wardrobe",pixels,source.width,source.height,columns[1].x,columns[1].y),
                Slice("Portal",pixels,source.width,source.height,columns[2].x,columns[2].y)
            };
#pragma warning restore 618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log("Hub prop sprites: "+string.Join(", ",Array.ConvertAll(AssetDatabase.LoadAllAssetsAtPath(path),asset=>asset.name)));
            importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static SpriteMetaData Slice(string name,Color32[] pixels,int width,int height,int start,int end)
        {
            int minX=end,minY=height,maxX=start,maxY=0;
            for(int y=0;y<height;y++) for(int x=start;x<end;x++)
            {
                if(pixels[y*width+x].a<32) continue;
                minX=Math.Min(minX,x); minY=Math.Min(minY,y);
                maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y);
            }
            if(minX>maxX) throw new InvalidOperationException("Empty hub sprite column: "+name);
            minX=Math.Max(start,minX-3); maxX=Math.Min(end-1,maxX+3);
            minY=Math.Max(0,minY-3); maxY=Math.Min(height-1,maxY+3);
            return new SpriteMetaData { name=name,rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),pivot=new Vector2(.5f,.5f),alignment=0 };
        }
    }
}
