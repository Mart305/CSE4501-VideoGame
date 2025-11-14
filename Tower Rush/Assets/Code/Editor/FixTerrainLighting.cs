using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class FixTerrainLighting : EditorWindow
{
    [MenuItem("Tools/Fix Terrain Lighting")]
    public static void FixLighting()
    {
        // Check and adjust directional light
        Light[] lights = Object.FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                Debug.Log($"[FixTerrainLighting] Directional Light: {light.name}");
                Debug.Log($"  Intensity: {light.intensity}");
                Debug.Log($"  Color: {light.color}");
                
                // If light is too bright, reduce it
                if (light.intensity > 1.5f)
                {
                    light.intensity = 1.0f;
                    Debug.Log($"  Reduced intensity to 1.0");
                }
                
                EditorUtility.SetDirty(light);
            }
        }
        
        // Check ambient lighting
        Debug.Log($"\n[FixTerrainLighting] Ambient Settings:");
        Debug.Log($"  Ambient Mode: {RenderSettings.ambientMode}");
        Debug.Log($"  Ambient Intensity: {RenderSettings.ambientIntensity}");
        Debug.Log($"  Ambient Light: {RenderSettings.ambientLight}");
        
        // Reduce ambient if too bright
        if (RenderSettings.ambientIntensity > 1.0f)
        {
            RenderSettings.ambientIntensity = 0.7f;
            Debug.Log($"  Reduced ambient intensity to 0.7");
        }
        
        // Check terrain materials for any overrides
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in terrains)
        {
            if (terrain.materialTemplate != null)
            {
                Material mat = terrain.materialTemplate;
                
                // Check if material has any brightness/color overrides
                if (mat.HasProperty("_BaseColor"))
                {
                    Color baseColor = mat.GetColor("_BaseColor");
                    Debug.Log($"\n[FixTerrainLighting] Terrain {terrain.name} Base Color: {baseColor}");
                    
                    // Reset if too bright
                    if (baseColor.r > 0.9f && baseColor.g > 0.9f && baseColor.b > 0.9f)
                    {
                        mat.SetColor("_BaseColor", Color.white);
                        Debug.Log($"  Reset base color to white");
                    }
                }
                
                EditorUtility.SetDirty(mat);
            }
        }
        
        EditorUtility.DisplayDialog("Lighting Check Complete", 
            "Checked lighting settings.\n\nIf terrain still looks washed out, the issue may be with the Skybox or Post-Processing.", 
            "OK");
    }
}
#endif
