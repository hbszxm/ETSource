using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridCLR.Editor;
using UnityEditor;
using UnityEngine;

public class HybridCLRHelper
{
    public static void BuildWolongMetaDll()
    {
        BuildMetaDateDlls(EditorUserBuildSettings.activeBuildTarget);
    }

    public static void BuildMetaDateDlls(BuildTarget target)
    {
        MoveAotDllMeta();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    public static void MoveAotDllMeta()
    {
        string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);


        List<string> AOTMetaAssemblyNames = new List<string>()
        {
            "mscorlib",
            "System",
            "System.Core",
            "Unity.Core",
            "Unity.Mono",
            "Unity.ThirdParty",
            "MongoDB.Bson"
        };
        foreach (var dll in AOTMetaAssemblyNames)
        {
            string dllPath = $"{aotDllDir}/{dll}.dll";
            if (!File.Exists(dllPath))
            {
                Debug.LogError(
                    $"ab中添加AOT补充元数据dll:{dllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                continue;
            }

            string dllBytesPath = $"{Application.dataPath}/Bundles/Code/{dll}.bytes";
            File.Copy(dllPath, dllBytesPath, true);
            Debug.Log($"[BuildAssetBundles] copy AOT dll {dllPath} -> {dllBytesPath}");
        }
    }
}