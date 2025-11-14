using UnityEngine;
using UnityEditor;
using UnityEngine.TerrainTools;

#if UNITY_EDITOR
public class ResetTerrainLayerTiling : EditorWindow
{
    [MenuItem("Tools/Reset Terrain Layer Tiling to 1x1")]
    public static void ResetAllTerrainLayers()
    {
        // Find all terrain layers
        string[] guids = AssetDatabase.FindAssets("t:TerrainLayer", new[] { "Assets/Textures/TerrainLayers" });
        int count = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            
            if (layer != null)
            {
                // Reset to 1x1 tiling
                layer.tileSize = new Vector2(1f, 1f);
                
                EditorUtility.SetDirty(layer);
                count++;
                
                Debug.Log($"[ResetTerrainLayerTiling] Reset {layer.name}: tileSize = 1x1");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[ResetTerrainLayerTiling] Reset {count} terrain layers to 1x1 tiling");
        
        EditorUtility.DisplayDialog("Terrain Layers Reset", 
            $"Reset {count} terrain layers to 1x1 tiling.", 
            "OK");
    }
}
#endif
