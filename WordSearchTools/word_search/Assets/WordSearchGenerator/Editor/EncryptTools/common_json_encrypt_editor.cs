//common_json_encrypt_editor.cs
//通用 Json 加密 Editor 菜单（基于 common_json_encrypt_tool）
//2026-02-27

using System.IO;
using UnityEditor;
using UnityEngine;

public class common_json_encrypt_editor
{
    [MenuItem("Assets/Tools/通用加密Json文件", false, 2)]
    public static void encrypt_selected_json_file()
    {
        if (Selection.activeObject == null)
        {
            Debug.Log("请在 Project 视图中选择目标 Json 文件");
            return;
        }

        string select_guid = Selection.assetGUIDs[0];
        string select_path = AssetDatabase.GUIDToAssetPath(select_guid);

        if (string.IsNullOrEmpty(select_path))
        {
            EditorUtility.DisplayDialog("fail", "加密失败，未获取到文件路径", "ok");
            return;
        }

        FileInfo json_file = new FileInfo(select_path);
        if (json_file.Extension != ".json")
        {
            EditorUtility.DisplayDialog("fail", "加密失败，当前选择的不是 json 文件", "ok");
            return;
        }

        string content = File.ReadAllText(select_path);
        if (string.IsNullOrEmpty(content))
        {
            EditorUtility.DisplayDialog("fail", "加密失败，没有读到文本内容", "ok");
            return;
        }

        // 先解析，检查是否已经有 code 字段，避免重复加密
        JSONObject json_content = new JSONObject(content);
        if (json_content.HasField("code"))
        {
            EditorUtility.DisplayDialog("fail", "加密失败，该文件已经存在 code 字段，可能已经被加密", "ok");
            return;
        }

        // 调用通用工具进行加密
        string encrypt_result = common_json_encrypt_tool.encrypt_json_object(json_content);
        if (string.IsNullOrEmpty(encrypt_result))
        {
            EditorUtility.DisplayDialog("fail", "加密失败，加密结果为空", "ok");
            return;
        }

        // 直接覆盖原文件
        save_file(json_file.DirectoryName + "/", json_file.Name, encrypt_result);

        Debug.Log("通用 Json 加密成功: " + select_path);
        EditorUtility.DisplayDialog("succes", "通用 Json 加密成功", "ok");
    }

    public static void save_file(string path, string filename, string content)
    {
        if (Directory.Exists(path) == false)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            dir.Create();
        }
        string fullpath = path + filename;

        if (!File.Exists(fullpath))
        {
            File.Create(fullpath).Dispose();
        }
        File.WriteAllText(fullpath, content);
        AssetDatabase.Refresh();
    }
}

