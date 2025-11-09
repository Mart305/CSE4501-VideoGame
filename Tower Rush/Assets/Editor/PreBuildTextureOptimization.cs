using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class PreBuildTextureOptimization : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // Only optimize for WebGL builds
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        Debug.Log("=== Pre-Build Texture Optimization Started ===");
        
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int optimizedCount = 0;
        int skippedCount = 0;
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer == null)
            {
                skippedCount++;
                continue;
            }

            bool needsReimport = false;
            
            // Get WebGL platform settings
            var webGLSettings = importer.GetPlatformTextureSettings("WebGL");
            
            // Check if we need to override settings
            if (!webGLSettings.overridden || webGLSettings.maxTextureSize > 1024)
            {
                webGLSettings.overridden = true;
                webGLSettings.maxTextureSize = 1024;
                
                // Apply appropriate compression
                if (importer.textureType == TextureImporterType.NormalMap)
                {
                    webGLSettings.format = TextureImporterFormat.DXT5;
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
                optimizedCount++;
                needsReimport = true;
            }
            
            if (i % 100 == 0)
            {
                Debug.Log($"Processed {i}/{guids.Length} textures...");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"=== Pre-Build Texture Optimization Complete ===");
        Debug.Log($"Optimized: {optimizedCount} textures");
        Debug.Log($"Skipped: {skippedCount} textures");
        Debug.Log($"Total: {guids.Length} textures");
    }
}
