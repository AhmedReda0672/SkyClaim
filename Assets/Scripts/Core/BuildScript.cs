#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    [MenuItem("SkyClaim/Build Windows x64")]
    public static void BuildWindows()
    {
        string[] scenes = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GameScene.unity"
        };
        
        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = @"D:\Games\SkyClaim\Build\SkyClaim.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log($"[BuildScript] Build succeeded: D:\\Games\\SkyClaim\\Build\\SkyClaim.exe ({report.summary.totalSize / 1024 / 1024} MB)");
        else
            Debug.LogError($"[BuildScript] Build FAILED: {report.summary.result}");
    }
}
#endif
