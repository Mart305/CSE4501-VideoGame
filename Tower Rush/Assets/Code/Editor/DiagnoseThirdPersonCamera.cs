using UnityEngine;
using UnityEditor;

/// <summary>
/// Diagnoses third-person camera settings that might affect terrain rendering
/// </summary>
public class DiagnoseThirdPersonCamera : EditorWindow
{
    [MenuItem("Tools/Diagnose Third Person Camera")]
    public static void ShowWindow()
    {
        GetWindow<DiagnoseThirdPersonCamera>("Camera Diagnostics");
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Third Person Camera Diagnostics", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Analyze Camera Settings", GUILayout.Height(30)))
        {
            AnalyzeCameraSettings();
        }

        GUILayout.Space(10);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.EndScrollView();
    }

    private void AnalyzeCameraSettings()
    {
        Debug.Log("=== THIRD PERSON CAMERA DIAGNOSTICS ===");

        // Find all cameras in scene
        Camera[] cameras = FindObjectsOfType<Camera>();
        
        foreach (Camera cam in cameras)
        {
            Debug.Log($"\n--- Camera: {cam.gameObject.name} ---");
            Debug.Log($"  Enabled: {cam.enabled}");
            Debug.Log($"  Near Clip: {cam.nearClipPlane}");
            Debug.Log($"  Far Clip: {cam.farClipPlane}");
            Debug.Log($"  FOV: {cam.fieldOfView}");
            Debug.Log($"  Depth: {cam.depth}");
            Debug.Log($"  Clear Flags: {cam.clearFlags}");
            Debug.Log($"  Culling Mask: {LayerMask.LayerToName(cam.cullingMask)}");
            Debug.Log($"  Rendering Path: {cam.renderingPath}");
            Debug.Log($"  Allow HDR: {cam.allowHDR}");
            Debug.Log($"  Allow MSAA: {cam.allowMSAA}");
            
            // Check for any post-processing components
            var components = cam.GetComponents<Component>();
            bool hasPostProcessing = false;
            foreach (var comp in components)
            {
                if (comp.GetType().Name.Contains("Post") || comp.GetType().Name.Contains("Volume"))
                {
                    Debug.Log($"  Post-Processing Component: {comp.GetType().Name}");
                    hasPostProcessing = true;
                }
            }
            if (!hasPostProcessing)
            {
                Debug.Log($"  Post-Processing: NONE");
            }
        }

        Debug.Log("\n=== END DIAGNOSTICS ===");
    }
}
