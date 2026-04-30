using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using WeChatWASM;
using YooAsset;
using YooAsset.Editor;

namespace TEngine.Editor
{
    /// <summary>
    /// 微信小游戏一键打包工具。
    /// <para>
    /// 完整流程：
    ///   1. 切换平台到 WebGL，自动设置 WXTemplate2022
    ///   2. 添加 WEIXINMINIGAME 宏定义（仅 WebGL 平台）
    ///   3. 将 ResourceModuleDriver.playMode 设为 WebPlayMode
    ///   4. 编译并拷贝 HybridCLR DLL
    ///   5. 打包 AssetBundle（YooAsset）
    ///   6. 调用微信 SDK DoExport：内部构建 WebGL + 转换生成 game.json 等
    /// </para>
    /// </summary>
    public static class WechatMiniGameBuilder
    {
        private const string DEFINE_WECHAT    = "WEIXINMINIGAME";
        private const string WX_WEBGL_TEMPLATE = "PROJECT:WXTemplate2022";

        private static string ProjectRoot  => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string BundleOutput => Path.Combine(ProjectRoot, "Bundles");
        private static string MiniGameDst  => Path.Combine(ProjectRoot, "Builds", "WechatMiniGame");

        // ── 菜单 ─────────────────────────────────────────────────────────────────

        [MenuItem("TEngine/Build/一键打包微信小游戏", false, 50)]
        public static void BuildWechatMiniGame()
        {
            bool ok = EditorUtility.DisplayDialog(
                "一键打包微信小游戏",
                "将依次执行：\n" +
                "1. 切换平台到 WebGL，设置 WXTemplate2022\n" +
                "2. 添加 WEIXINMINIGAME 宏定义\n" +
                "3. 设置 ResourceModuleDriver.PlayMode = WebPlayMode\n" +
                "4. 编译并拷贝 HybridCLR DLL\n" +
                "5. 打包 AssetBundle\n" +
                "6. 微信 SDK 构建 + 转换（生成 game.json 等）\n\n" +
                $"输出目录：{MiniGameDst}",
                "开始打包", "取消");

            if (!ok)
            {
                Debug.Log("[WechatMiniGame] 用户取消。");
                return;
            }

            try
            {
                RunPipeline();
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[WechatMiniGame] 打包异常：{e}");
            }
        }

        [MenuItem("TEngine/Build/微信小游戏 - 仅准备环境", false, 51)]
        public static void PrepareEnvironmentOnly()
        {
            SwitchToWebGL();
            AddWechatDefine();
            SetWebPlayMode();
            Debug.Log("[WechatMiniGame] 环境准备完成。");
        }

        [MenuItem("TEngine/Build/微信小游戏 - 还原 PlayMode", false, 52)]
        public static void RestorePlayMode()
        {
            ApplyPlayModeToDriver(EPlayMode.OfflinePlayMode);
            Debug.Log("[WechatMiniGame] PlayMode 已还原为 OfflinePlayMode。");
        }

        // ── 流水线 ────────────────────────────────────────────────────────────────

        private static void RunPipeline()
        {
            Progress("Step 1/6  切换平台到 WebGL...", 0f / 6f);
            SwitchToWebGL();

            Progress("Step 2/6  添加 WEIXINMINIGAME 宏定义...", 1f / 6f);
            AddWechatDefine();

            Progress("Step 3/6  设置 PlayMode = WebPlayMode...", 2f / 6f);
            SetWebPlayMode();

            Progress("Step 4/6  编译并拷贝 HybridCLR DLL...", 3f / 6f);
            BuildDlls();

            Progress("Step 5/6  打包 AssetBundle...", 4f / 6f);
            BuildAssetBundle();

            Progress("Step 6/6  微信 SDK 构建 + 转换...", 5f / 6f);
            bool success = DoWechatExport();

            EditorUtility.ClearProgressBar();

            if (success)
            {
                Debug.Log($"[WechatMiniGame] ✅ 打包完成！工程已输出到：{MiniGameDst}/minigame");
                EditorUtility.DisplayDialog("打包完成",
                    $"微信小游戏工程已导出：\n{MiniGameDst}/minigame\n\n请用微信开发者工具打开该目录进行上传。",
                    "确定");
            }
            else
            {
                Debug.LogError("[WechatMiniGame] ❌ 微信 SDK 构建/转换失败，请查看 Console 日志。");
            }
        }

        // ── 各步骤 ────────────────────────────────────────────────────────────────

        private static void SwitchToWebGL()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                Debug.Log("[WechatMiniGame] 切换平台到 WebGL...");
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
                if (!switched)
                    throw new Exception("切换 WebGL 平台失败，请确认已安装 WebGL Build Support 模块。");
                AssetDatabase.Refresh();
                Debug.Log("[WechatMiniGame] 平台已切换到 WebGL。");
            }
            else
            {
                Debug.Log("[WechatMiniGame] 已是 WebGL 平台，跳过切换。");
            }

            SetWXWebGLTemplate();
        }

        private static void SetWXWebGLTemplate()
        {
            if (PlayerSettings.WebGL.template == WX_WEBGL_TEMPLATE)
            {
                Debug.Log($"[WechatMiniGame] WebGL Template 已是 {WX_WEBGL_TEMPLATE}，跳过。");
                return;
            }

            string templatePath = Path.Combine(Application.dataPath, "WebGLTemplates", "WXTemplate2022");
            if (!Directory.Exists(templatePath))
            {
                Debug.LogWarning($"[WechatMiniGame] 未找到 {templatePath}，跳过模板设置，请手动选择微信 WebGL 模板。");
                return;
            }

            PlayerSettings.WebGL.template = WX_WEBGL_TEMPLATE;
            Debug.Log($"[WechatMiniGame] WebGL Template 已设置为 {WX_WEBGL_TEMPLATE}。");
        }

        private static void AddWechatDefine()
        {
            if (ScriptingDefineSymbols.HasScriptingDefineSymbol(BuildTargetGroup.WebGL, DEFINE_WECHAT))
            {
                Debug.Log($"[WechatMiniGame] 宏 {DEFINE_WECHAT} 已存在，跳过。");
                return;
            }

            ScriptingDefineSymbols.AddScriptingDefineSymbol(BuildTargetGroup.WebGL, DEFINE_WECHAT);
            AssetDatabase.Refresh();
            Debug.Log($"[WechatMiniGame] 已添加宏定义：{DEFINE_WECHAT}（WebGL 平台）。");
        }

        private static void SetWebPlayMode()
        {
            ApplyPlayModeToDriver(EPlayMode.WebPlayMode);
        }

        private static void BuildDlls()
        {
            try
            {
                BuildDLLCommand.BuildAndCopyDlls(BuildTarget.WebGL);
                Debug.Log("[WechatMiniGame] HybridCLR DLL 编译拷贝完成。");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WechatMiniGame] HybridCLR DLL 步骤跳过（未启用 HybridCLR 可忽略）：{e.Message}");
            }
        }

        private static void BuildAssetBundle()
        {
            string version = MakeVersion();
            Debug.Log($"[WechatMiniGame] 打包 AssetBundle，版本：{version}，输出：{BundleOutput}");

            var scriptableParams = new ScriptableBuildParameters
            {
                CompressOption              = ECompressOption.LZ4,
                BuiltinShadersBundleName    = GetBuiltinShaderBundleName("DefaultPackage"),
                ReplaceAssetPathWithAddress = Settings.UpdateSetting.GetReplaceAssetPathWithAddress()
            };

            BuildParameters p = scriptableParams;
            p.BuildOutputRoot       = BundleOutput;
            p.BuildinFileRoot       = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            p.BuildPipeline         = EBuildPipeline.ScriptableBuildPipeline.ToString();
            p.BuildTarget           = BuildTarget.WebGL;
            p.BuildBundleType       = (int)EBuildBundleType.AssetBundle;
            p.PackageName           = "DefaultPackage";
            p.PackageVersion        = version;
            p.VerifyBuildingResult  = true;
            p.EnableSharePackRule   = true;
            p.FileNameStyle         = EFileNameStyle.BundleName_HashName;
            p.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
            p.BuildinFileCopyParams = string.Empty;
            p.EncryptionServices    = ReadEncryptionFromDriver();
            p.ClearBuildCacheFiles  = false;
            p.UseAssetDependencyDB  = true;

            var result = new ScriptableBuildPipeline().Run(p, true);
            if (!result.Success)
                throw new Exception($"AssetBundle 构建失败：{result.ErrorInfo}");

            AssetDatabase.Refresh();
            Debug.Log($"[WechatMiniGame] AssetBundle 构建成功：{result.OutputPackageDirectory}");
        }

        /// <summary>
        /// 调用微信 SDK DoExport(buildWebGL=true)。
        /// SDK 内部会：
        ///   1. 以 DST/webgl/ 为输出路径构建 WebGL
        ///   2. 将结果转换为小游戏工程，输出到 DST/minigame/
        /// </summary>
        private static bool DoWechatExport()
        {
            var config = UnityUtil.GetEditorConf();
            if (config == null)
            {
                Debug.LogError("[WechatMiniGame] 未找到微信 SDK 配置（WXEditorScriptObject）。" +
                               "请先打开「微信小游戏 / 转换小游戏」面板，填写 AppID 和输出目录后保存，再执行打包。");
                return false;
            }

            // 保存原始 DST，完成后还原
            string origDST         = config.ProjectConf.DST;
            string origRelativeDST = config.ProjectConf.relativeDST;

            try
            {
                if (!Directory.Exists(MiniGameDst))
                    Directory.CreateDirectory(MiniGameDst);

                config.ProjectConf.DST         = MiniGameDst;
                config.ProjectConf.relativeDST = GetRelativePath(ProjectRoot, MiniGameDst);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                Debug.Log($"[WechatMiniGame] 微信 SDK DoExport，DST：{MiniGameDst}");

                // buildWebGL=true：SDK 自己构建 WebGL（输出到 DST/webgl/）再转换
                var error = WXConvertCore.DoExport(buildWebGL: true);

                if (error == WXConvertCore.WXExportError.SUCCEED)
                {
                    Debug.Log("[WechatMiniGame] 微信 SDK 转换成功。");
                    return true;
                }

                Debug.LogError($"[WechatMiniGame] 微信 SDK 转换失败，错误码：{error}");
                return false;
            }
            finally
            {
                config.ProjectConf.DST         = origDST;
                config.ProjectConf.relativeDST = origRelativeDST;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        // ── 工具方法 ──────────────────────────────────────────────────────────────

        private static void ApplyPlayModeToDriver(EPlayMode mode)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab GameEntry");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[WechatMiniGame] 未找到 GameEntry 预制体，跳过 PlayMode 设置。");
                return;
            }

            string path   = AssetDatabase.GUIDToAssetPath(guids[0]);
            var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[WechatMiniGame] 加载 GameEntry 预制体失败：{path}");
                return;
            }

            var driver = prefab.GetComponentInChildren<ResourceModuleDriver>(true);
            if (driver == null)
            {
                Debug.LogWarning("[WechatMiniGame] GameEntry 中未找到 ResourceModuleDriver，跳过。");
                return;
            }

            var so   = new SerializedObject(driver);
            var prop = so.FindProperty("playMode");
            if (prop == null)
            {
                Debug.LogWarning("[WechatMiniGame] 未找到 playMode 字段，跳过。");
                return;
            }

            EPlayMode prev = (EPlayMode)prop.intValue;
            if (prev == mode)
            {
                Debug.Log($"[WechatMiniGame] PlayMode 已是 {mode}，跳过。");
                return;
            }

            prop.intValue = (int)mode;
            so.ApplyModifiedProperties();
            PrefabUtility.SavePrefabAsset(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"[WechatMiniGame] ResourceModuleDriver.PlayMode：{prev} → {mode}。");
        }

        private static IEncryptionServices ReadEncryptionFromDriver()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab GameEntry");
            if (guids.Length == 0) return null;

            var path   = AssetDatabase.GUIDToAssetPath(guids[0]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;

            var driver = prefab.GetComponentInChildren<ResourceModuleDriver>();
            if (driver == null) return null;

            return driver.EncryptionType switch
            {
                EncryptionType.FileOffSet => new FileOffsetEncryption(),
                EncryptionType.FileStream => new FileStreamEncryption(),
                _                         => null
            };
        }

        private static string GetBuiltinShaderBundleName(string packageName)
        {
            var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            var packRuleResult   = DefaultPackRule.CreateShadersPackRuleResult();
            return packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }

        private static string MakeVersion()
        {
            int minutes = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
            return DateTime.Now.ToString("yyyy-MM-dd") + "-" + minutes;
        }

        private static string GetRelativePath(string basePath, string targetPath)
        {
            var baseUri   = new Uri(basePath.TrimEnd('/') + "/");
            var targetUri = new Uri(targetPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri).ToString());
        }

        private static void Progress(string msg, float t)
        {
            Debug.Log($"[WechatMiniGame] {msg}");
            EditorUtility.DisplayProgressBar("微信小游戏打包", msg, t);
        }
    }
}
