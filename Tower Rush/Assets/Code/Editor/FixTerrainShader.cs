using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

#if UNITY_EDITOR
public class FixTerrainShader : EditorWindow
{
    [MenuItem("Tools/Fix Terrain Shader and Materials")]
    public static void FixTerrainShaderAndMaterials()
    {
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
        int fixedCount = 0;
        
        foreach (Terrain terrain in terrains)
        {
            if (terrain != null && terrain.materialTemplate != null)
            {
                Material mat = terrain.materialTemplate;
                
                Debug.Log($"[FixTerrainShader] Terrain: {terrain.name}");
                Debug.Log($"  Current Shader: {mat.shader.name}");
                Debug.Log($"  Render Queue: {mat.renderQueue}");
                
                // Check if using correct terrain shader
                if (!mat.shader.name.Contains("Terrain"))
                {
                    // Try to find the correct terrain shader
                    Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                    if (terrainShader == null)
                    {
                        terrainShader = Shader.Find("Nature/Terrain/Standard");
                    }
                    
                    if (terrainShader != null)
                    {
                        mat.shader = terrainShader;
                        Debug.Log($"  Fixed shader to: {terrainShader.name}");
                    }
                }
                
                // Check terrain layers
                TerrainLayer[] layers = terrain.terrainData.terrainLayers;
                Debug.Log($"  Terrain Layers: {layers.Length}");
                
                for (int i = 0; i < layers.Length && i < 5; i++)
                {
                    if (layers[i] != null)
                    {
                        Debug.Log($"    Layer {i}: {layers[i].name}");
                        Debug.Log($"      Diffuse: {(layers[i].diffuseTexture != null ? "OK" : "MISSING")}");
                        Debug.Log($"      Normal: {(layers[i].normalMapTexture != null ? "OK" : "None")}");
                        Debug.Log($"      Tile Size: {layers[i].tileSize}");
                    }
                }
                
                // Force terrain to update
                terrain.terrainData.SetBaseMapDirty();
                terrain.Flush();
                EditorUtility.SetDirty(terrain);
                
                fixedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[FixTerrainShader] Checked {fixedCount} terrains - see console for details");
        
        EditorUtility.DisplayDialog("Terrain Shader Check Complete", 
            $"Checked {fixedCount} terrains.\n\nCheck the Console for detailed information about shader and texture assignments.", 
            "OK");
    }
}
#endif
