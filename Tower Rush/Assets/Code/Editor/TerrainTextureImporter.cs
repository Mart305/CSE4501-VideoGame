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
            platformSettings.format = TextureImporterFormat.RGBA32; // Uncompressed
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SetPlatformTextureSettings(platformSettings);
            
            Debug.Log($"[TerrainTextureImporter] Set high-quality import settings for: {assetPath}");
        }
    }
}
