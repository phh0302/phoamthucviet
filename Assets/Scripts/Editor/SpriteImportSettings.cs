using UnityEditor;

namespace PhoAmThucViet.EditorTools
{
    /// <summary>
    /// Ảnh thả vào Assets/Resources/Sprites tự được nhập đúng kiểu cho game 2D:
    /// dạng Sprite, giữ nền trong suốt, không nén mờ, 100 pixel = 1 đơn vị.
    /// </summary>
    public class SpriteImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Sprites/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 1024;
        }
    }
}
