using UnityEngine;
using UnityEditor;

/// <summary>
/// Automatically sets high-quality import settings for terrain textures
/// This prevents compression artifacts on terrain materials
/// </summary>
public class TerrainTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Check if this texture is in a terrain-related folder
        bool isTerrainTexture = assetPath.Contains("Terrain") || 
                                assetPath.Contains("Ground") || 
                                assetPath.Contains("Grass") ||
                                assetPath.Contains("Sand") ||
                                assetPath.Contains("Rock") ||
                                assetPath.Contains("Soil") ||
                                assetPath.Contains("Snow") ||
                                assetPath.Contains("Mud");
        
        if (isTerrainTexture)
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            
            // Detect texture type by filename
            bool isNormalMap = assetPath.Contains("_Normal") || assetPath.Contains("_normal");
            bool isSingleChannel = assetPath.Contains("_Height") ||
                                   assetPath.Contains("_Thickness") ||
                                   assetPath.Contains("_AO") ||
                                   assetPath.Contains("_Roughness") ||
                                   assetPath.Contains("_Metallic") ||
                                   assetPath.Contains("_Mask");
            
            // Set texture type first (important for normal maps)
            if (isNormalMap)
            {
                textureImporter.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                textureImporter.textureType = TextureImporterType.Default;
            }
            
            // Set high-quality settings
            textureImporter.maxTextureSize = 2048; // High resolution
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed; // No compression
            textureImporter.mipmapEnabled = true; // Keep mipmaps for distance
            textureImporter.anisoLevel = 16; // Maximum anisotropic filtering
            textureImporter.filterMode = FilterMode.Trilinear; // Best quality filtering
            textureImporter.wrapMode = TextureWrapMode.Repeat; // For tiling
            
            // Platform-specific settings for better quality
            var platformSettings = textureImporter.GetDefaultPlatformTextureSettings();
            platformSettings.maxTextureSize = 2048;
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            
            // Use appropriate format based on texture type
            if (isNormalMap)
            {
                // Normal maps need RGBA32 or specific normal map format
                platformSettings.format = TextureImporterFormat.RGBA32;
            }
            else if (isSingleChannel)
            {
                // Single channel textures need R8 format
                platformSettings.format = TextureImporterFormat.R8;
            }
            else
            {
                // Color textures use RGBA32
                platformSettings.format = TextureImporterFormat.RGBA32;
            }
            
            textureImporter.SetPlatformTextureSettings(platformSettings);
            
            // WebGL-specific settings for high quality
            var webglSettings = textureImporter.GetPlatformTextureSettings("WebGL");
            webglSettings.overridden = true;
            webglSettings.maxTextureSize = 2048;
            webglSettings.textureCompression = TextureImporterCompression.Uncompressed;
            webglSettings.format = platformSettings.format; // Use same format as default
            textureImporter.SetPlatformTextureSettings(webglSettings);
            
            string formatType = isNormalMap ? "RGBA32 (NormalMap)" : (isSingleChannel ? "R8" : "RGBA32");
            Debug.Log($"[TerrainTextureImporter] Set high-quality import settings for: {assetPath} (format: {formatType})");
        }
    }
}
