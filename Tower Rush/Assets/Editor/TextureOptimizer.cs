using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureOptimizer : AssetPostprocessor
{
    // Maximum texture size for WebGL builds
    private const int MAX_TEXTURE_SIZE = 1024;
    
    void OnPreprocessTexture()
    {
        TextureImporter textureImporter = (TextureImporter)assetImporter;
        
        // Get platform settings for WebGL
        var webGLSettings = textureImporter.GetPlatformTextureSettings("WebGL");
        
        // Only optimize if not already configured
        if (!webGLSettings.overridden)
        {
            webGLSettings.overridden = true;
            webGLSettings.maxTextureSize = MAX_TEXTURE_SIZE;
            
            // Use appropriate compression based on texture type
            if (textureImporter.textureType == TextureImporterType.NormalMap)
            {
                webGLSettings.format = TextureImporterFormat.DXT5;
            }
            else if (textureImporter.DoesSourceTextureHaveAlpha())
            {
                webGLSettings.format = TextureImporterFormat.DXT5;
            }
            else
            {
                webGLSettings.format = TextureImporterFormat.DXT1;
            }
            
            textureImporter.SetPlatformTextureSettings(webGLSettings);
        }
        
        // Reduce quality for non-critical textures
        if (assetPath.Contains("Normal") || assetPath.Contains("Height"))
        {
            textureImporter.isReadable = false;
            textureImporter.mipmapEnabled = true;
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
                var webGLSettings = importer.GetPlatformTextureSettings("WebGL");
                webGLSettings.overridden = true;
                webGLSettings.maxTextureSize = 1024;
                
                // Compress based on texture type
                if (importer.textureType == TextureImporterType.NormalMap)
                {
                    webGLSettings.format = TextureImporterFormat.DXT5;
                }
                else if (importer.DoesSourceTextureHaveAlpha())
                {
                    webGLSettings.format = TextureImporterFormat.DXT5;
                }
                else
                {
                    webGLSettings.format = TextureImporterFormat.DXT1;
                }
                
                importer.SetPlatformTextureSettings(webGLSettings);
                importer.SaveAndReimport();
                count++;
            }
            
            EditorUtility.DisplayProgressBar("Optimizing Textures", 
                $"Processing {i + 1}/{guids.Length}", (float)i / guids.Length);
        }
        
        EditorUtility.ClearProgressBar();
        Debug.Log($"Optimized {count} textures for WebGL builds");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
