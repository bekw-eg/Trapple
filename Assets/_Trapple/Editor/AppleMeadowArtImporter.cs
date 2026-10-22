using UnityEditor;
using UnityEngine;

namespace Trapple.Editor
{
    public sealed class AppleMeadowArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Trapple/Resources/AppleMeadow/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            if (assetPath.EndsWith("/Ground.png"))
            {
                importer.spritePixelsPerUnit = 64f;
                importer.spriteBorder = new Vector4(0f, 0f, 0f, 20f);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
