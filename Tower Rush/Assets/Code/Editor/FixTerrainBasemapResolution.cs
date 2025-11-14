using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class FixTerrainBasemapResolution : EditorWindow
{
    [MenuItem("Tools/Fix Terrain Basemap Resolution")]
    public static void FixBasemapResolution()
    {
        // Find all terrain data assets
        string[] guids = AssetDatabase.FindAssets("t:TerrainData");
        int count = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            
            if (terrainData != null)
            {
                // Increase basemap resolution to maximum (4096 for best quality)
                terrainData.baseMapResolution = 4096;
                
                // Also ensure alphamap resolution is high
                if (terrainData.alphamapResolution < 1024)
                {
                    terrainData.alphamapResolution = 1024;
                }
                
                // Mark as dirty and regenerate basemap
                terrainData.SetBaseMapDirty();
                EditorUtility.SetDirty(terrainData);
                count++;
                
                Debug.Log($"[FixTerrainBasemapResolution] Updated {terrainData.name}: basemapResolution = 2048, alphamapResolution = {terrainData.alphamapResolution}");
            }
        }
        
        // Also fix any terrains in the scene
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in terrains)
        {
            if (terrain.terrainData != null)
            {
                terrain.terrainData.baseMapResolution = 4096;
                if (terrain.terrainData.alphamapResolution < 1024)
                {
                    terrain.terrainData.alphamapResolution = 1024;
                }
                terrain.terrainData.SetBaseMapDirty();
                terrain.Flush();
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[FixTerrainBasemapResolution] Fixed {count} terrain data assets + {terrains.Length} scene terrains with 4096 basemap resolution");
        
        EditorUtility.DisplayDialog("Terrain Basemap Fixed", 
            $"Updated {count} terrain data assets with 4096 basemap resolution (maximum quality).\n\nThis should fix the whitish/pixelated appearance at distance.", 
            "OK");
    }
}
#endif
