using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// must put on global namespace
public static class PackageExport
{
    static string kAppName = Application.productName;
    static string kTargetPath = "Builds";
    static string[] kScenes = FindEnabledEditorScenes();

    private static string[] FindEnabledEditorScenes()
    {
        List<string> EditorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            EditorScenes.Add(scene.path);
        }
        return EditorScenes.ToArray();
    }

    static void GenericBuild(string[] scenes, string targetPath, BuildTarget buildTarget, BuildOptions buildOptions)
    {
        bool splashscreenshow = PlayerSettings.SplashScreen.show;
        PlayerSettings.SplashScreen.show = false;
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(scenes, targetPath, buildTarget, buildOptions);
        PlayerSettings.SplashScreen.show = splashscreenshow;
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("Build Failed " + report.summary.result);
            Console.WriteLine("Build Failed " + report.summary.result);
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode) EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("Windows version has now been built.");
            Debug.Log("DONE");
        }
    }

    [MenuItem("Lost Words/Build/Win64Final", false, 0)]
    static void PerformWin64FinalBuild()
    {
        BuildOptions options = BuildOptions.None;
        EditorUserBuildSettings.development = false;
        GenericBuild(kScenes, kTargetPath + "/Win64/" + kAppName + ".exe", BuildTarget.StandaloneWindows64, options);
    }
}