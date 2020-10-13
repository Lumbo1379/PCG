using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// must put on global namespace
public static class PackageExport
{
    // method must be static
    [MenuItem("Tools/Export Unitypackage")]
    public static void Export()
    {
        // configure
        var root = "Scripts/CISample";
        var exportPath = "./CISample.unitypackage";

        var path = Path.Combine(Application.dataPath, root);
        var assets = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(x => Path.GetExtension(x) == ".cs")
            .Select(x => "Assets" + x.Replace(Application.dataPath, "").Replace(@"\", "/"))
            .ToArray();

        UnityEngine.Debug.Log("Export below files" + Environment.NewLine + string.Join(Environment.NewLine, assets));

        AssetDatabase.ExportPackage(
            assets,
            exportPath,
            ExportPackageOptions.Default);

        UnityEngine.Debug.Log("Export complete: " + Path.GetFullPath(exportPath));
    }
}