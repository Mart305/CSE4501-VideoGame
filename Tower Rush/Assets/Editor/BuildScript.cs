using UnityEditor;
using UnityEngine;

public class BuildScript
{
    public static void BuildWebGL()
    {
        // Build options for production release
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetScenePaths(),
            locationPathName = "build/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None // Release build, no development options
        };

        Debug.Log("Starting WebGL build...");
        Debug.Log($"Build target: {buildPlayerOptions.target}");
        Debug.Log($"Build options: {buildPlayerOptions.options}");
        Debug.Log($"Output path: {buildPlayerOptions.locationPathName}");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.totalSize} bytes");
            Debug.Log($"Build time: {summary.totalTime}");
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed!");
            EditorApplication.Exit(1);
        }
    }

    private static string[] GetScenePaths()
    {
        // Get all enabled scenes from build settings
        var scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        return scenes;
    }
}
