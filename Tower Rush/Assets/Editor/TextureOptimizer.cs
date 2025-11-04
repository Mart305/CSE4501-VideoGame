using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureOptimizer : AssetPostprocessor
{
    // Maximum texture size for WebGL builds
    private const int MAX_TEXTURE_SIZE = 2048; // Increased from 1024
    private const int HIGH_QUALITY_TEXTURE_SIZE = 2048; // For important textures
    
    void OnPreprocessTexture()
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        
        // Check if this is a high-quality texture that should not be compressed aggressively
        bool isHighQualityTexture = assetPath.Contains("Portal") || 
                                     assetPath.Contains("Terrain") ||
                                     assetPath.Contains("Effect") ||
                                     assetPath.Contains("Particle");
        
        // Get platform settings for WebGL
        var webGLSettings = textureImporter.GetPlatformTextureSettings("WebGL");
        
        // Only optimize if not already configured
        if (!webGLSettings.overridden)
        {
            webGLSettings.overridden = true;
            
            // Use higher resolution for important textures
            webGLSettings.maxTextureSize = isHighQualityTexture ? HIGH_QUALITY_TEXTURE_SIZE : MAX_TEXTURE_SIZE;
            
            // Use appropriate compression based on texture type
            if (textureImporter.textureType == TextureImporterType.SingleChannel)
            {
                // Single channel textures must use BC4 or R8
                webGLSettings.format = TextureImporterFormat.BC4;
                webGLSettings.compressionQuality = 100;
            }
            else if (textureImporter.textureType == TextureImporterType.NormalMap)
            {
                // Normal maps must use DXT5 or BC5
                webGLSettings.format = TextureImporterFormat.DXT5;
                webGLSettings.compressionQuality = 100;
            }
            else if (isHighQualityTexture)
            {
                // High quality textures - use less compression
                if (textureImporter.DoesSourceTextureHaveAlpha())
                {
                    webGLSettings.format = TextureImporterFormat.RGBA32; // Uncompressed for best quality
                }
                else
                {
                    webGLSettings.format = TextureImporterFormat.RGB24; // Uncompressed for best quality
                }
                webGLSettings.compressionQuality = 100; // Maximum quality
            }
            else if (textureImporter.DoesSourceTextureHaveAlpha())
            {
                // Textures with alpha channel
                webGLSettings.format = TextureImporterFormat.DXT5;
                webGLSettings.compressionQuality = 100;
            }
            else
            {
                // Standard RGB textures
                webGLSettings.format = TextureImporterFormat.DXT1;
                webGLSettings.compressionQuality = 50;
            }
            
            textureImporter.SetPlatformTextureSettings(webGLSettings);
        }
        
        // Enable mipmaps for better quality at distance
        if (assetPath.Contains("Normal") || assetPath.Contains("Height") || assetPath.Contains("Terrain"))
        {
            textureImporter.isReadable = false;
            textureImporter.mipmapEnabled = true;
            textureImporter.streamingMipmaps = true; // Enable mipmap streaming for better performance
        }
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public class TextureOptimizerMenu
{
    [MenuItem("Tools/Optimize All Textures for WebGL")]
    public static void OptimizeAllTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int count = 0;
        
        EditorUtility.DisplayProgressBar("Optimizing Textures", "Processing textures...", 0);
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                // Check if this is a high-quality texture
                bool isHighQualityTexture = path.Contains("Portal") || 
                                             path.Contains("Terrain") ||
                                             path.Contains("Effect") ||
                                             path.Contains("Particle");
                
                var webGLSettings = importer.GetPlatformTextureSettings("WebGL");
                webGLSettings.overridden = true;
                webGLSettings.maxTextureSize = isHighQualityTexture ? 2048 : 2048;
                
                // Compress based on texture type - check type FIRST
                if (importer.textureType == TextureImporterType.SingleChannel)
                {
                    // Single channel textures must use BC4
                    webGLSettings.format = TextureImporterFormat.BC4;
                    webGLSettings.compressionQuality = 100;
                }
                else if (importer.textureType == TextureImporterType.NormalMap)
                {
                    // Normal maps must use DXT5
                    webGLSettings.format = TextureImporterFormat.DXT5;
                    webGLSettings.compressionQuality = 100;
                }
                else if (isHighQualityTexture)
                {
                    // High quality textures - use less compression
                    if (importer.DoesSourceTextureHaveAlpha())
                    {
                        webGLSettings.format = TextureImporterFormat.RGBA32;
                    }
                    else
                    {
                        webGLSettings.format = TextureImporterFormat.RGB24;
                    }
                    webGLSettings.compressionQuality = 100;
                }
                else if (importer.DoesSourceTextureHaveAlpha())
                {
                    webGLSettings.format = TextureImporterFormat.DXT5;
                    webGLSettings.compressionQuality = 100;
                }
                else
                {
                    webGLSettings.format = TextureImporterFormat.DXT1;
                    webGLSettings.compressionQuality = 50;
                }
                
                importer.SetPlatformTextureSettings(webGLSettings);
                importer.SaveAndReimport();
                count++;
            }
            
            EditorUtility.DisplayProgressBar("Optimizing Textures", 
                $"Processing {i + 1}/{guids.Length}", (float)i / guids.Length);
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Optimized {count} textures for WebGL with improved quality settings");
    }
}
#endif
