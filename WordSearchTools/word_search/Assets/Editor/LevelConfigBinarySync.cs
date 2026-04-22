using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelConfigBinarySync
{
    // 当前工程（word_search）工程根目录（ProjectSettings 同级）
    // source: word_search/../level_config  (即 WordSearchTools/level_config)
    private const string SourceDirRelativeToThisProjectRoot = "../level_config";

    // dest: word_search/../../UnityProject/Assets/AssetRaw/Configs/levels
    private const string DestDirRelativeToThisProjectRoot = "../../UnityProject/Assets/AssetRaw/Configs/levels";

    [MenuItem("Tools/Configs/Sync Level Config Binaries To GameProject")]
    public static void SyncToGameProject()
    {
        var thisProjectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(thisProjectRoot))
        {
            Debug.LogError("无法定位当前 Unity 工程根目录。");
            return;
        }

        var sourceDir = Path.GetFullPath(Path.Combine(thisProjectRoot, SourceDirRelativeToThisProjectRoot));
        var destDir = Path.GetFullPath(Path.Combine(thisProjectRoot, DestDirRelativeToThisProjectRoot));

        if (!Directory.Exists(sourceDir))
        {
            Debug.LogError($"源目录不存在：{SourceDirRelativeToThisProjectRoot}");
            return;
        }

        Directory.CreateDirectory(destDir);

        var copiedCount = 0;
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            var normalizedSourceDir = sourceDir.Replace('\\', '/').TrimEnd('/');
            var normalizedFile = file.Replace('\\', '/');
            var relativePath = normalizedFile.StartsWith(normalizedSourceDir + "/", StringComparison.OrdinalIgnoreCase)
                ? normalizedFile.Substring(normalizedSourceDir.Length + 1)
                : Path.GetFileName(file);

            var targetPath = Path.Combine(destDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? destDir);

            File.Copy(file, targetPath, overwrite: true);
            copiedCount++;
        }

        Debug.Log($"同步完成：{copiedCount} 个文件 -> {DestDirRelativeToThisProjectRoot}");
    }
}

