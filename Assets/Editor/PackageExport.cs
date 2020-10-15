using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// must put on global namespace
public static class PackageExport
{
    static string kAppName = Application.productName;
    static string kTargetPathBuilds = "Builds";
    static string kTargetPathTests = "Tests";
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

    [MenuItem("PCG/Build/Win64Final", false, 0)]
    static void PerformWin64FinalBuild()
    {
        BuildOptions options = BuildOptions.None;
        EditorUserBuildSettings.development = false;
        GenericBuild(kScenes, kTargetPathBuilds + "/Win64/" + kAppName + ".exe", BuildTarget.StandaloneWindows64, options);
    }

    [MenuItem("PCG/UnitTests", false, 1)]
    static void PerformUnitTests()
    {
        var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        testRunnerApi.RegisterCallbacks(new MyCallbacks());
        var filter = new Filter()
        {
            testMode = TestMode.EditMode
        };
        testRunnerApi.Execute(new ExecutionSettings(filter));
    }

    private class MyCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {

        }

        public void RunFinished(ITestResultAdaptor result)
        {

        }

        public void TestStarted(ITestAdaptor test)
        {

        }

        public void TestFinished(ITestResultAdaptor result)
        {
            using (StreamWriter sw = new StreamWriter("tests.txt", true))
            {
                    sw.Write(("Test {0} {1}", result.Test.Name, result.ResultState));
            }
        }
    }
}