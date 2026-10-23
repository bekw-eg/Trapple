using System.IO;
using System.Linq;
using NUnit.Framework;
using Trapple.Workshop;
using UnityEditor;
using UnityEngine;

namespace Trapple.Tests
{
    public sealed class WorkshopDoorAssetTests
    {
        private const string Root="Assets/_Trapple/Art/Workshop/Door/";
        [Test]
        public void IndependentOverlaysHaveIdenticalCenteredFullRectPointImports()
        {
            for(int index=0;index<9;index++)
            {
                string path=Root+"doors_png/door_"+index.ToString("00")+".png";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(sprite.rect,Is.EqualTo(new Rect(0,0,80,120)));
                Assert.That(sprite.pivot,Is.EqualTo(new Vector2(40,60)));
                Assert.That(sprite.pixelsPerUnit,Is.EqualTo(100));
                Assert.That(sprite.vertices.Length,Is.EqualTo(4),"Full Rect must preserve the crop even for the narrow last frame.");
                Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));
                Assert.That(importer.spriteImportMode,Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importer.mipmapEnabled,Is.False);
                Assert.That(importer.npotScale,Is.EqualTo(TextureImporterNPOTScale.None));
                Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.That(importer.alphaIsTransparency,Is.True);
            }
        }
        [Test]
        public void AlternativeSheetUsesTopLeftToBottomRightOrderWithMatchingPivots()
        {
            var frames=AssetDatabase.LoadAllAssetsAtPath(Root+"door_spritesheet_3x3.png").OfType<Sprite>().OrderBy(sprite=>sprite.name).ToArray();
            Assert.That(frames.Length,Is.EqualTo(9));
            for(int index=0;index<9;index++)
            {
                Assert.That(frames[index].name,Is.EqualTo("door_"+index.ToString("00")));
                Assert.That(frames[index].rect,Is.EqualTo(new Rect(index%3*80,(2-index/3)*120,80,120)));
                Assert.That(frames[index].pivot,Is.EqualTo(new Vector2(40,60)));
            }
        }
        [Test]
        public void ReusablePrefabRegistersTheOverlayAgainstOneStaticCottage()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Trapple/Prefabs/Hub/WorkshopSystem.prefab");
            var door=prefab.GetComponentInChildren<WorkshopDoorAnimator>();
            var overlay=door.Presentation;
            var cottage=overlay.transform.parent.GetComponent<SpriteRenderer>();
            Assert.That(door.FrameCount,Is.EqualTo(9)); Assert.That(door.HasAuthoredFrames,Is.True);
            Assert.That(overlay.enabled,Is.True); Assert.That(cottage.enabled,Is.True);
            Assert.That(overlay.sprite.name,Is.EqualTo("door_00"));
            Assert.That(AssetDatabase.GetAssetPath(cottage.sprite),Is.EqualTo(Root+"house_empty_doorway.png"));
            Assert.That(cottage.sprite.rect.size,Is.EqualTo(new Vector2(469,347)));
            Assert.That(cottage.sprite.pivot,Is.EqualTo(new Vector2(234.5f,173.5f)));
            Assert.That(cottage.sprite.texture.filterMode,Is.EqualTo(FilterMode.Point));
            Assert.That(cottage.sharedMaterial.shader.name,Is.EqualTo("Trapple/Sprites/Cottage Cutout"));
            Assert.That(AssetDatabase.GetAssetPath(cottage.sharedMaterial.GetTexture("_CutoutTex")),Is.EqualTo(Root+"cottage_cutout.png"));
            Assert.That(overlay.transform.localPosition,Is.EqualTo(new Vector3(-1.035f,-.845f,0)));
            Assert.That(overlay.transform.localScale,Is.EqualTo(Vector3.one));
            Assert.That(overlay.sortingOrder,Is.EqualTo(cottage.sortingOrder+1));
            Assert.That(overlay.sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            var serialized=new SerializedObject(door);
            Assert.That(serialized.FindProperty("framesPerSecond").floatValue,Is.EqualTo(10));
        }
        [Test]
        public void ExteriorCutoutRemovesTheBackdropButKeepsTheDarkDoorwayOpaque()
        {
            var mask=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Assert.That(mask.LoadImage(File.ReadAllBytes(Root+"cottage_cutout.png")),Is.True);
                foreach(var point in new[]{new Vector2Int(0,0),new Vector2Int(468,0),new Vector2Int(0,346),new Vector2Int(468,346),new Vector2Int(20,100)})
                    Assert.That(AlphaAtSourcePixel(mask,point.x,point.y),Is.LessThan(.5f),"Exterior background must be transparent.");
                for(int y=230;y<300;y++) for(int x=115;x<150;x++)
                    Assert.That(AlphaAtSourcePixel(mask,x,y),Is.GreaterThanOrEqualTo(.5f),"The doorway must not reveal the landscape.");
                Assert.That(AlphaAtSourcePixel(mask,250,150),Is.GreaterThanOrEqualTo(.5f),"Preserve the cottage roof/wall.");
            }
            finally { Object.DestroyImmediate(mask); }
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"cottage_cutout.png");
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled,Is.False);
            Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
        }
        private static float AlphaAtSourcePixel(Texture2D mask,int x,int y)
        {
            int mx=Mathf.FloorToInt((x+.5f)*mask.width/469);
            int my=mask.height-1-Mathf.FloorToInt((y+.5f)*mask.height/347);
            return mask.GetPixel(mx,my).a;
        }
    }
}
