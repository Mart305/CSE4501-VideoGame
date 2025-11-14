using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Automatically detects and sets texture types for terrain textures
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
            
            // Set texture type (important for normal maps)
            if (isNormalMap)
            {
                textureImporter.textureType = TextureImporterType.NormalMap;
                textureImporter.convertToNormalmap = false; // Don't convert, already a normal map
                textureImporter.normalmapFilter = TextureImporterNormalFilter.Standard;
                Debug.Log($"[TerrainTextureImporter] Set texture type to NormalMap for: {assetPath}");
            }
            else
            {
                textureImporter.textureType = TextureImporterType.Default;
                Debug.Log($"[TerrainTextureImporter] Set texture type to Default for: {assetPath}");
            }
        }
    }
}

#if UNITY_EDITOR
[InitializeOnLoad]
public class TerrainTextureMenu
{
    [MenuItem("Tools/Reimport All Terrain Textures")]
    public static void ReimportTerrainTextures()
    {
        string[] terrainPaths = new string[]
        {
            "Assets/Textures/TerrainTextures",
            "Assets/Terrains",
            "Assets/Textures/TerrainLayers"
        };
        
        int count = 0;
        EditorUtility.DisplayProgressBar("Reimporting Terrain Textures", "Finding textures...", 0);
        
        foreach (string searchPath in terrainPaths)
        {
            if (!Directory.Exists(searchPath))
                continue;
                
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchPath });
            
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Reimporting Terrain Textures", 
                    $"Processing {Path.GetFileName(path)}...", (float)count / (guids.Length * terrainPaths.Length));
                
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[TerrainTextureMenu] Reimported {count} terrain textures");
    }
}
#endif
