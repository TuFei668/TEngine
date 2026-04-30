# 微信小游戏 CDN 配置指南

## 背景

微信小游戏不支持相对路径加载资源，所有 AssetBundle 必须通过 **完整的 HTTPS CDN 地址** 下载。  
本项目使用 YooAsset 管理资源，运行时下载地址由 `UpdateSetting.asset` 控制。

---

## 一、资源目录结构

打包后的 AssetBundle 位于：

```
Bundles/
└── WebGL/
    └── DefaultPackage/
        ├── DefaultPackage.version          ← 版本索引文件
        └── 2026-04-29-977/                 ← 版本号目录（每次打包生成）
            ├── DefaultPackage_xxx.bytes    ← 资源清单
            ├── DefaultPackage_xxx.hash
            ├── DefaultPackage_xxx.json
            ├── *.bundle                    ← 所有 AssetBundle 文件
            └── ...
```

**需要上传到 CDN 的内容：整个 `Bundles/WebGL/` 目录。**

---

## 二、UpdateSetting 地址规则

`UpdateSetting.asset` 中配置的地址会自动拼接平台名：

```
最终下载地址 = ResDownLoadPath / projectName / WebGL
```

例如：
- `ResDownLoadPath` = `https://cdn.example.com/game`
- `projectName` = `Demo`
- 实际请求路径 = `https://cdn.example.com/game/Demo/WebGL/`

---

## 三、本地调试配置

本地调试时用 HTTP 静态服务器模拟 CDN，无需真实上传。

### 3.1 启动本地静态服务器

**方式一：使用 Python（推荐，无需安装）**

```bash
# 在项目根目录执行，将 Bundles 目录作为静态资源根
python3 -m http.server 8081 --directory Bundles
```

**方式二：使用 Node.js serve**

```bash
npm install -g serve
serve Bundles -p 8081
```

启动后访问 `http://127.0.0.1:8081/WebGL/DefaultPackage/` 验证能否看到文件列表。

### 3.2 配置 UpdateSetting（本地）

打开 `Assets/TEngine/Settings/UpdateSetting.asset`，在 Inspector 中设置：

| 字段 | 本地调试值 |
|---|---|
| **Project Name** | `Demo`（与项目名一致） |
| **Res Down Load Path** | `http://127.0.0.1:8081` |
| **Fallback Res Down Load Path** | `http://127.0.0.1:8082` |
| **Load Res Way WebGL** | `Remote` |

> ⚠️ 微信开发者工具默认不允许 HTTP 请求，需要在工具中开启：  
> **详情 → 本地设置 → 勾选「不校验合法域名、web-view（业务域名）、TLS 版本以及 HTTPS 证书」**

### 3.3 配置微信 SDK 转换面板（本地）

打开 Unity 菜单 `微信小游戏 / 转换小游戏`，填写：

| 字段 | 本地调试值 |
|---|---|
| **游戏 AppID** | 你的小游戏 AppID |
| **CDN** | `http://127.0.0.1:8081/WebGL/DefaultPackage` |
| **输出目录（DST）** | `../Builds/WechatMiniGame`（相对于项目根） |

---

## 四、正式 CDN 配置

### 4.1 选择 CDN 服务

推荐使用以下任一服务：

| 服务 | 特点 |
|---|---|
| **腾讯云 COS + CDN** | 与微信生态同源，延迟低，推荐 |
| **阿里云 OSS + CDN** | 国内覆盖好，价格实惠 |
| **七牛云** | 免费额度较多，适合小项目 |

### 4.2 上传资源到 CDN

以**腾讯云 COS** 为例：

**步骤一：创建存储桶**

1. 登录 [腾讯云 COS 控制台](https://console.cloud.tencent.com/cos)
2. 创建存储桶，地域选择**华南（广州）**或**华东（上海）**
3. 访问权限设置为**公有读私有写**

**步骤二：上传资源**

```bash
# 安装腾讯云 CLI
pip install coscmd

# 配置（替换为你的 SecretId、SecretKey、Bucket、Region）
coscmd config -a <SecretId> -s <SecretKey> -b <BucketName> -r ap-guangzhou

# 上传整个 WebGL 目录
coscmd upload -r Bundles/WebGL/ /game/WebGL/
```

或直接在 COS 控制台网页上传 `Bundles/WebGL/` 目录。

**步骤三：开启 CDN 加速（可选但推荐）**

在 COS 控制台 → 域名与传输管理 → 自定义 CDN 加速域名，绑定你的域名并开启 HTTPS。

最终 CDN 地址示例：
```
https://cdn.example.com/game
```

### 4.3 配置 UpdateSetting（正式）

| 字段 | 正式环境值 |
|---|---|
| **Project Name** | `Demo`（与项目名一致） |
| **Res Down Load Path** | `https://cdn.example.com/game` |
| **Fallback Res Down Load Path** | `https://cdn-backup.example.com/game` |
| **Load Res Way WebGL** | `Remote` |

### 4.4 配置微信 SDK 转换面板（正式）

| 字段 | 正式环境值 |
|---|---|
| **游戏 AppID** | 你的小游戏 AppID |
| **CDN** | `https://cdn.example.com/game/Demo/WebGL` |
| **输出目录（DST）** | `../Builds/WechatMiniGame` |

> ⚠️ 微信小游戏要求 CDN 域名必须在**微信公众平台 → 开发管理 → 开发设置 → 服务器域名**中添加为合法域名，否则请求会被拦截。

---

## 五、完整路径对照表

假设：
- `projectName` = `Demo`
- CDN 根地址 = `https://cdn.example.com/game`

| 内容 | 本地路径 | CDN 路径 |
|---|---|---|
| 版本索引 | `Bundles/WebGL/DefaultPackage/DefaultPackage.version` | `https://cdn.example.com/game/Demo/WebGL/DefaultPackage/DefaultPackage.version` |
| 资源清单 | `Bundles/WebGL/DefaultPackage/2026-04-29-977/DefaultPackage_xxx.bytes` | `https://cdn.example.com/game/Demo/WebGL/DefaultPackage/2026-04-29-977/DefaultPackage_xxx.bytes` |
| Bundle 文件 | `Bundles/WebGL/DefaultPackage/2026-04-29-977/*.bundle` | `https://cdn.example.com/game/Demo/WebGL/DefaultPackage/2026-04-29-977/*.bundle` |

---

## 六、每次发版的上传流程

```
1. Unity 执行「TEngine/Build/一键打包AssetBundle」
   → 产物：Bundles/WebGL/DefaultPackage/<版本号>/

2. 将新版本目录上传到 CDN
   coscmd upload -r Bundles/WebGL/ /game/WebGL/

3. 同时更新 DefaultPackage.version（YooAsset 自动生成，一并上传）

4. 执行「TEngine/Build/一键打包微信小游戏」导出小游戏工程

5. 用微信开发者工具打开 Builds/WechatMiniGame/minigame/ 上传审核
```

---

## 七、常见问题

**Q：报错 `request:fail invalid url "xxx.bundle"`**  
A：CDN 地址未配置或配置了相对路径。检查 `UpdateSetting.asset` 的 `ResDownLoadPath` 是否为完整 HTTPS 地址。

**Q：本地调试时请求被拦截**  
A：微信开发者工具需勾选「不校验合法域名」，见 3.2 节。

**Q：正式环境请求被拦截**  
A：CDN 域名未加入微信公众平台的合法域名白名单，见 4.4 节。

**Q：资源更新后客户端还是加载旧版本**  
A：YooAsset 通过版本号控制更新，确保每次打包后 `DefaultPackage.version` 文件也一并上传到 CDN。
