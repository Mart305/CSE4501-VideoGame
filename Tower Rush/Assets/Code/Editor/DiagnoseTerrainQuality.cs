using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class DiagnoseTerrainQuality : EditorWindow
{
    [MenuItem("Tools/Diagnose Terrain Quality Issues")]
    public static void DiagnoseQuality()
    {
        Debug.Log("========== TERRAIN QUALITY DIAGNOSIS ==========");
        
        // Check quality settings
        Debug.Log($"Current Quality Level: {QualitySettings.GetQualityLevel()}");
        Debug.Log($"Global Texture Mipmap Limit: {QualitySettings.globalTextureMipmapLimit}");
        Debug.Log($"Anisotropic Filtering: {QualitySettings.anisotropicFiltering}");
        Debug.Log($"Terrain Basemap Distance: {QualitySettings.terrainBasemapDistance}");
        Debug.Log($"Terrain Pixel Error: {QualitySettings.terrainPixelError}");
        
        // Check all terrain data
        string[] guids = AssetDatabase.FindAssets("t:TerrainData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            
            if (terrainData != null)
            {
                Debug.Log($"\n--- {terrainData.name} ---");
                Debug.Log($"  Basemap Resolution: {terrainData.baseMapResolution}");
                Debug.Log($"  Alphamap Resolution: {terrainData.alphamapResolution}");
                Debug.Log($"  Heightmap Resolution: {terrainData.heightmapResolution}");
            }
        }
        
        // Check terrain layers
        string[] layerGuids = AssetDatabase.FindAssets("t:TerrainLayer", new[] { "Assets/Textures/TerrainLayers" });
        Debug.Log($"\n--- Terrain Layers ({layerGuids.Length} found) ---");
        foreach (string guid in layerGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            
            if (layer != null)
            {
                Debug.Log($"  {layer.name}: tileSize = {layer.tileSize}");
            }
        }
        
        Debug.Log("\n========== END DIAGNOSIS ==========");
        
        EditorUtility.DisplayDialog("Diagnosis Complete", 
            "Check the Console for detailed terrain quality information.", 
            "OK");
    }
}
#endif
