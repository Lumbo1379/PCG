using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

class JenkinsBuild
{
    private static string[] SCENES = FindEnabledEditorScenes();

    private static string APP_NAME = "PCG";
    private static string TARGET_DIR = "target";

    private static void BuildWindows64()
    {
        string targetDir = APP_NAME + ".app";
        GenericBuild(SCENES, TARGET_DIR + "/" + targetDir, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    private static void RunEditorUnitTest()
    {
        var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        var filter = new Filter()
        {
            testMode = TestMode.EditMode
        };
        testRunnerApi.Execute(new ExecutionSettings(filter));

    }

    private static string[] FindEnabledEditorScenes()
    {
        return (from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray();
    }

    private static void GenericBuild(string[] scenes, string targetDir, BuildTarget buildTarget, BuildOptions buildOptions)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(buildTarget);
        BuildPipeline.BuildPlayer(scenes, targetDir, buildTarget, buildOptions).ToString();
    }
}
