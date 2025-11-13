using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to fix compression on all terrain textures
/// Use: Tools > Fix Terrain Texture Compression
/// </summary>
public class FixTerrainTextureCompression : EditorWindow
{
    private int texturesFixed = 0;
    private bool isProcessing = false;
    
    [MenuItem("Tools/Fix Terrain Texture Compression")]
    static void ShowWindow()
    {
        GetWindow<FixTerrainTextureCompression>("Fix Terrain Textures");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Terrain Texture Compression Fix", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "• Find all terrain-related textures\n" +
            "• Remove compression\n" +
            "• Set maximum quality\n" +
            "• Enable anisotropic filtering\n\n" +
            "This will increase memory usage but eliminate compression artifacts.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (isProcessing)
        {
            EditorGUILayout.HelpBox($"Processing... {texturesFixed} textures fixed", MessageType.Warning);
        }
        else
        {
            if (GUILayout.Button("Fix All Terrain Textures", GUILayout.Height(40)))
            {
                FixAllTerrainTextures();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Fix Terrain Layer Textures Only", GUILayout.Height(30)))
            {
                FixTerrainLayerTextures();
            }
        }
        
        if (texturesFixed > 0 && !isProcessing)
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox($"✓ Fixed {texturesFixed} textures successfully!", MessageType.Info);
        }
    }
    
    void FixAllTerrainTextures()
    {
        isProcessing = true;
        texturesFixed = 0;
        
        // Search paths for terrain textures
        string[] searchPaths = new string[]
        {
            "Assets/Textures/TerrainLayers",
            "Assets/Textures",
            "Assets/StarterAssets/Environment/Textures"
        };
        
        foreach (string searchPath in searchPaths)
        {
            if (Directory.Exists(searchPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { searchPath });
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    // Check if it's a terrain-related texture
                    if (IsTerrainTexture(path))
                    {
                        FixTextureCompression(path);
                    }
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        isProcessing = false;
        
        Debug.Log($"[FixTerrainTextureCompression] Fixed {texturesFixed} terrain textures");
    }
    
    void FixTerrainLayerTextures()
    {
        isProcessing = true;
        texturesFixed = 0;
        
        // Get all terrain layers
        string[] terrainLayerGuids = AssetDatabase.FindAssets("t:TerrainLayer");
        
        foreach (string guid in terrainLayerGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            
            if (layer != null)
            {
                // Fix diffuse texture
                if (layer.diffuseTexture != null)
                {
                    string texturePath = AssetDatabase.GetAssetPath(layer.diffuseTexture);
                    FixTextureCompression(texturePath);
                }
                
                // Fix normal map
                if (layer.normalMapTexture != null)
                {
                    string texturePath = AssetDatabase.GetAssetPath(layer.normalMapTexture);
                    FixTextureCompression(texturePath);
                }
                
                // Fix mask map
                if (layer.maskMapTexture != null)
                {
                    string texturePath = AssetDatabase.GetAssetPath(layer.maskMapTexture);
                    FixTextureCompression(texturePath);
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        isProcessing = false;
        
        Debug.Log($"[FixTerrainTextureCompression] Fixed {texturesFixed} terrain layer textures");
    }
    
    bool IsTerrainTexture(string path)
    {
        string lowerPath = path.ToLower();
        return lowerPath.Contains("terrain") ||
               lowerPath.Contains("ground") ||
               lowerPath.Contains("grass") ||
               lowerPath.Contains("sand") ||
               lowerPath.Contains("rock") ||
               lowerPath.Contains("soil") ||
               lowerPath.Contains("snow") ||
               lowerPath.Contains("mud") ||
               lowerPath.Contains("pebble") ||
               lowerPath.Contains("heather") ||
               lowerPath.Contains("tidal");
    }
    
    void FixTextureCompression(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer != null)
        {
            bool changed = false;
            
            // Detect texture type by filename
            bool isNormalMap = path.Contains("_Normal") || path.Contains("_normal");
            bool isSingleChannel = path.Contains("_Height") ||
                                   path.Contains("_Thickness") ||
                                   path.Contains("_AO") ||
                                   path.Contains("_Roughness") ||
                                   path.Contains("_Metallic") ||
                                   path.Contains("_Mask");
            
            // Set texture type first (important for normal maps)
            TextureImporterType targetType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != targetType)
            {
                importer.textureType = targetType;
                changed = true;
            }
            
            // Set high-quality settings
            if (importer.maxTextureSize != 2048)
            {
                importer.maxTextureSize = 2048;
                changed = true;
            }
            
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            
            if (importer.anisoLevel != 16)
            {
                importer.anisoLevel = 16;
                changed = true;
            }
            
            if (importer.filterMode != FilterMode.Trilinear)
            {
                importer.filterMode = FilterMode.Trilinear;
                changed = true;
            }
            
            if (importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                changed = true;
            }
            
            // Platform-specific settings
            var platformSettings = importer.GetDefaultPlatformTextureSettings();
            if (platformSettings.maxTextureSize != 2048 || 
                platformSettings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                platformSettings.maxTextureSize = 2048;
                platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
                
                // Use appropriate format based on texture type
                if (isNormalMap)
                {
                    platformSettings.format = TextureImporterFormat.RGBA32; // Normal maps
                }
                else if (isSingleChannel)
                {
                    platformSettings.format = TextureImporterFormat.R8; // Single channel
                }
                else
                {
                    platformSettings.format = TextureImporterFormat.RGBA32; // Color
                }
                
                importer.SetPlatformTextureSettings(platformSettings);
                changed = true;
            }
            
            if (changed)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                texturesFixed++;
                string formatType = isNormalMap ? "RGBA32 (NormalMap)" : (isSingleChannel ? "R8" : "RGBA32");
                Debug.Log($"[FixTerrainTextureCompression] Fixed: {path} (format: {formatType})");
            }
        }
    }
}
