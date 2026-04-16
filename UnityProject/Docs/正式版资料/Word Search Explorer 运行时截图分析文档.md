# Word Search Explorer 运行时截图深度分析文档

> 基于实际游戏运行截图逐屏分析，覆盖启动流程、主界面、核心玩法、活动系统、收藏系统、道具系统、广告策略等全部模块。

---

## 一、启动与登录流程

### 1.1 启动加载页

- 全屏山湖自然风景背景（高山、湖泊、绿地、蓝天），视觉风格清新治愈
- 游戏Logo "WORD SEARCH EXPLORER"，其中"O"替换为指南针图标，强化"探索"主题
- 中下方白色Loading进度条
- 底部显示 User ID（如 `b0dAZ-tBzA`），用于客服和数据追踪
- 无任何可交互元素，纯等待页

### 1.2 登录/注册页

- 背景与加载页一致（山湖风景），保持视觉连贯
- 左上角绿色齿轮设置按钮
- 三种登录方式从上到下排列：
  1. **Facebook Login and Play**（蓝色按钮）— 右侧红色角标"150 FREE!"，用免费金币激励绑定账号
  2. **Sign in with Apple**（白色按钮）— Apple图标 + 文字
  3. **Play as Guest**（绿色按钮）— 游客模式，零门槛进入
- 设计意图：游客模式放在最底部但用醒目绿色，降低进入门槛；Facebook登录用金币奖励引导绑定，便于云存档和社交功能

---

## 二、主界面（Home）

### 2.1 整体布局

- 背景：马来西亚山景（与当前地区主题匹配）
- 顶部导航栏：左侧返回+设置按钮，右侧金币数量（295）+ 商店购物车图标

### 2.2 中央信息区

- **头像**：圆形头像（熊猫），右下角蓝色编辑图标，可更换
- **问候语**："Morning, Tom!" — 根据时间段动态变化
- **当前地区**："MALAYSIA" — 世界旅行Meta主题
- **地区进度条**：4/20（绿色进度条），右侧礼盒图标（完成地区后领取奖励）
- **连续打卡**："Current streak 0 Days / Your Best: 1 Days"，右上角倒计时12:43:..

### 2.3 左侧活动入口（垂直排列）

- **奖杯图标**（紫色，标注"New"）— Weekday Tournament 入口
- **饼图图标**（0/5，倒计时12h 43m）— Daily Dash 活动
- **宝箱图标**（倒计时12h 43m）— Word Master 活动

### 2.4 其他入口

- **右上角**：红色禁止广告图标（ADS）— 去广告付费入口
- **左下角**：红色礼盒图标 — Daily Reward 每日奖励
- **右下角**：蓝色"More games!"图标 — 交叉推广其他游戏
- **底部大按钮**："Play Level 35"（绿色）— 一键进入当前关卡

### 2.5 底部Tab栏

三个Tab，图标+文字：
1. **日历图标** — 左侧（推测为活动/日历入口）
2. **Daily Challenge**（语录气泡图标）— 每日挑战
3. **Home**（房子图标，当前高亮黄色）— 主页
4. **My Collection**（书本图标）— 收藏系统

---

## 三、核心玩法

### 3.1 常规关卡界面（BIRD主题）

**顶部区域：**
- 左上：返回按钮 + 绿色星星按钮（学习积分/成就入口）
- 顶部中央：对战/社交标识 "大牛 已连接"（可能是好友对战或观战功能）
- 顶部提示："Solved by 49.96% players" — 显示全球玩家通过率，增加社交比较感
- 右上：金币数量（295）+ 商店

**主题标签：**
- 绿色横幅 "BIRD" — 当前关卡主题

**目标词列表：**
- 位于网格上方，瀑布流排列
- 已找到的词带删除线（如 ~~ALBATROSS~~、~~SPARROW~~）
- 未找到的词正常显示（CHICKEN、CROW、EAGLE、OWL、PARROT、PIGEON、SWAN、TURKEY）
- CROW 旁有4个金币图标 — 推测为 Hidden Word 或特殊奖励词标记

**字母网格：**
- 9行×8列的大网格
- 已找到的单词有彩色高亮线条覆盖：
  - ALBATROSS：紫色竖向高亮（左侧第一列，从上到下）
  - SPARROW：青色/绿色斜向高亮（从右上到左下方向）
- 不同单词用不同颜色区分，视觉清晰

**底部道具栏（6个图标）：**
1. 💬 语音气泡图标 — Bonus Words 查看入口
2. ✅ 勾选图标（标注"FREE"+ 金币）— 免费提示（可能需看广告）
3. ▶️ 播放图标（标注"GET"+ 金币）— 看广告获取道具
4. ⚡ 闪电图标（标注"200"+ 金币）— Wind Hint（200金币，高价道具）
5. 💡 灯泡图标（右上角红色数字"1"角标）— Normal Hint（显示剩余数量）
6. 🔄 旋转图标 — Rotate 旋转网格

### 3.2 每日挑战玩法界面（Daily Challenge）

**顶部区域：**
- 左上：返回 + 红色星星按钮
- 右上：金币295 + 商店

**题目区域：**
- 红色横幅 "WILL DURANT"（名言作者）
- 名言文本，缺失单词用下划线表示：
  > "___ of the _______ of history is ____ nothing is _____ a ____ thing to do and always a ______ thing to say."
- 玩家需要在网格中找到这些缺失的单词来补全名言

**字母网格：**
- 7行×6列（比常规关卡小）
- 纯字母网格，无高亮（尚未开始找词）

**底部道具栏：**
- 与常规关卡相同的5个道具（无语音气泡图标）
- 底部有横幅广告（Banner Ad）

### 3.3 Bonus Words 机制

**弹窗界面：**
- 标题 "Bonus Words"（红色横幅）
- 内容区："Bonus Words found in this level: You have not found any Bonus Words in this level."
- 底部进度条："Fill the progress bar to get the reward!"
  - 红色进度条已接近满格
  - 右侧显示奖励：25金币图标
  - 进度条是跨关卡累计的，不是单关重置
- 关闭按钮（X）

---

## 四、活动系统

### 4.1 Word Master（路径奖励）

- 弹窗标题 "Word Master"
- 倒计时：12h 43m（限时活动）
- 提示文字："Find 4 more words to unlock the next reward!"
- 奖励路径为网格布局（3列×4行+底部大奖），奖励之间有箭头（>、<、∨）连接形成蛇形路径：

| 位置 | 奖励1 | 奖励2 | 奖励3 |
|------|-------|-------|-------|
| 第1行 | x2标签 5min（当前高亮黄色，已领取） | x2灯泡 5min | 风车 +1 |
| 第2行 | -50%闪电 5min | 风车 +1 | 金币 +10 |
| 第3行 | 金币 +15 | x2标签 15min | x2灯泡 15min |
| 大奖行（粉色底） | 金币 +25 | -50%闪电 30min | 风车 +1 |

- 奖励类型包括：
  - **时间Buff**（5min/15min/30min）— 限时增益
  - **x2双倍道具**（标签/灯泡）— 双倍效果
  - **风车**（彩色风车）— 特殊道具（推测为Shuffle/旋转类）
  - **金币**（+10/+15/+25）
  - **-50%闪电**（半价Wind Hint）— 道具折扣
- 最终大奖行用粉色底色突出，奖励更丰厚
- 本质：轻量版Battle Pass，通过找词推进奖励路径

### 4.2 Daily Reward（每日登录奖励）

- 弹窗标题 "Daily Reward"（绿色横幅）
- 副标题："Days played in a row"
- 进度条：节点在第0天（已完成✓）、第7天（礼盒）、第15天（礼盒），当前第2天
- 当日奖励展示：-50%闪电 20mins（半价Wind Hint时间buff）
- 蓝色礼盒打开动画
- 状态："The Reward is already claimed!"
- 下次奖励倒计时："Next reward in 12:43:54"
- 底部有横幅广告

### 4.3 Daily Dash（每日冲刺）

- 弹窗标题 "Daily Dash"（红色丝带横幅）
- 紫色背景 + 金币堆图标（闪光效果）
- 进度条：0/5
- 任务说明："Beat 5 levels to earn 50 coins!"
- 绿色按钮 "Let's Go!"
- 本质：每日通关任务，简单直接的金币激励

### 4.4 Weekday Tournament（工作日锦标赛）

- 弹窗标题 "Weekday Tournament"（红色丝带横幅）
- 天蓝色草地背景 + 银色奖杯图标（放在红色跑道上）
- 说明："Complete a level to enter the Weekday Tournament!"
- 倒计时：23h 43m
- 绿色按钮 "Let's Go!"
- 参与门槛极低：只需完成1关即可进入锦标赛
- 主页左侧奖杯图标标注"New"，引导玩家注意

### 4.5 活动系统总结

| 活动 | 类型 | 周期 | 核心机制 | 奖励 |
|------|------|------|----------|------|
| Word Master | 路径奖励 | 限时（~12h） | 找词推进奖励路径 | 道具/金币/时间Buff |
| Daily Reward | 登录奖励 | 每日 | 连续登录累计 | 道具Buff（7/15天大奖） |
| Daily Dash | 通关任务 | 每日 | 通过5关 | 50金币 |
| Weekday Tournament | 竞赛排名 | 工作日（~24h） | 通关积分排名 | 排名奖励 |
| Daily Challenge | 名言填空 | 每日 | 补全名言 | Streak + 名言收集 |

---

## 五、收藏系统（My Collection）

收藏系统位于底部Tab栏最右侧"My Collection"，内含4个子Tab页。

### 5.1 STAMPS（邮票收集）

**列表视图：**
- 标题提示："Collect a stamp on completing the country tour!"
- 按主题分类，每个主题对应特定解锁关卡等级：

| 邮票主题 | 解锁关卡 | 状态 |
|----------|----------|------|
| Natural Landmark | Level 91 | 🔒 |
| Fauna | Level 171 | 🔒 |
| Natural Landmark II | Level 251 | 🔒 |
| Monument II | Level 331 | 🔒 |
| Culture II | Level 411 | 🔒 |
| ... | ... | ... |
| Natural Landmark V | Level 1611 | 🔒 |
| Culture IV | Level 1691 | 🔒 |
| Fauna VII | Level 1771 | 🔒 |
| Monuments VI | Level 1851 | 🔒 |
| Fauna VIII | Level 1931 | 🔒 |
| Flora V | Level 2011 | 🔒 |
| Natural Landmark VI | Level 2171 | 🔒 |
| Culture V | Level 2331 | 🔒 |
| Fauna IX | Level 2491 | 🔒 |

- 底部提示："Play 16 more levels to collect a new stamp!"（距离下一个邮票还需16关）

**详情视图：**
- 邮票按主题分组展示，每组3张邮票：
  - **Flora**（植物）：3张灰色未解锁邮票（圆形加载图标）
  - **Culture**（文化）：Bahamas-Junkanoo（彩色已解锁，狂欢节面具图案）、Japan（剪影待解锁）、Turkey（剪影待解锁）
  - **Monument**（古迹）：Italia（已解锁，比萨斜塔/罗马竞技场）、Germany（部分可见）
- 提示："Tap on the stamps to learn interesting facts!" — 点击邮票可查看趣味知识
- 邮票设计精美，已解锁的为彩色插画风格，未解锁的显示主题剪影轮廓

### 5.2 CROWNS（皇冠收集）

- 标题提示："Wear your glory with the Crown of Champions!"
- 按四季分类：Spring / Summer / Fall / Winter
- 当前赛季：**Spring**（Mar 2 - Jun 1）
- 进度条：当前0分，节点在10和30
- 皇冠预览：展示两个皇冠样式（蓝紫色宝石皇冠），分别对应10分和30分解锁
- Summer/Fall/Winter 均显示锁定状态
- 底部提示："Play through the Seasons to collect more Crowns!"
- 本质：季节性限时收集，每个赛季有独特皇冠样式

### 5.3 FRAMES（头像框收集）

- 标题提示："Unlock stunning frames and showcase your skill!"
- 同样按四季分类：Spring / Summer / Fall / Winter
- 当前赛季：**Spring**（Mar 2 - Jun 1）
- 进度条：当前0分，节点在10和40
- 头像框预览：木质圆环风格头像框，带星星和绿叶装饰，两个等级（10分/40分）
- Summer/Fall/Winter 均锁定
- 底部提示："Play River Race to unlock more frames!" — 明确关联River Race活动
- 本质：通过River Race活动获取积分，解锁季节限定头像框

### 5.4 QUOTES（名言收集）

- 标题提示："Collect new quotes from the Daily Challenge!"
- 展示已收集的名言卡片（便签纸样式，胶带固定效果）：
  > "The only way to keep your health is to eat what you don't want, drink what you don't like, and do what you'd rather not."
  > — MARK TWAIN
  > Collected on 16.03.26
- 底部倒计时："Complete the quote before it expires in 12:43:17"
- 绿色按钮 "Solve Today's Quote"
- 本质：通过每日挑战收集名人名言，形成个人名言集

### 5.5 收藏系统总结

| 收藏类型 | 获取方式 | 内容 | 长线目标 |
|----------|----------|------|----------|
| Stamps | 完成地区关卡 | 世界各地文化/自然/动植物邮票 | 2491+关卡的超长线收集 |
| Crowns | 季节性活动积分 | 四季限定皇冠装饰 | 每季度更新 |
| Frames | River Race活动 | 四季限定头像框 | 每季度更新 |
| Quotes | Daily Challenge | 名人名言卡片 | 每日1张，持续收集 |

---

## 六、道具系统

### 6.1 道具栏布局（底部固定）

游戏界面底部固定显示道具栏，从左到右：

| 位置 | 图标 | 名称 | 价格/获取 | 功能 |
|------|------|------|-----------|------|
| 1 | 💬 | Bonus Words | 免费 | 查看当前关卡已找到的Bonus Words和进度条 |
| 2 | ✅ | Free Hint | FREE（看广告） | 免费获得一次提示（通过观看广告） |
| 3 | ▶️ | Get Coins | GET（看广告） | 观看广告获取金币 |
| 4 | ⚡ | Wind Hint | 200金币 | 移除网格中所有干扰字母（高价重度道具） |
| 5 | 💡 | Normal Hint | 金币购买 | 揭示一个目标单词的起始位置（角标显示剩余数量） |
| 6 | 🔄 | Rotate | 金币购买 | 旋转整个网格180度，换视角 |

注意：Daily Challenge界面的道具栏没有Bonus Words气泡图标（因为每日挑战不支持Bonus Words机制）。

### 6.2 道具经济设计

- **Wind Hint 定价200金币**：这是最贵的道具，对比通关奖励（约10金币/关），需要约20关收入才能买一次，形成强烈的"金币饥渴"
- **Free Hint 通过广告获取**：道具系统是广告的核心入口
- **Normal Hint 带数量角标**：显示剩余可用次数，制造稀缺感
- **道具不影响胜负**：所有道具只提供便利，不存在"不用道具就过不了"的设计

---

## 七、广告策略

### 7.1 广告位分布（从截图观察）

| 广告类型 | 位置 | 出现场景 |
|----------|------|----------|
| Banner广告（横幅） | 页面底部 | 游戏界面底部、Daily Reward弹窗底部 |
| 去广告入口 | 主页右上角 | 红色ADS禁止图标，常驻显示 |
| 激励视频入口 | 道具栏 | Free Hint(FREE)、Get Coins(GET) |
| 交叉推广 | 主页右下角 | "More games!" 推广其他游戏 |

### 7.2 广告设计特点

- 底部Banner广告在多个页面出现（游戏界面、弹窗底部），是持续性收入来源
- 激励视频与道具深度绑定：免费提示和金币获取都需要看广告
- 去广告按钮位于主页右上角醒目位置，持续提醒付费选项
- 交叉推广（More games）放在不影响核心体验的角落位置

---

## 八、货币经济系统

### 8.1 金币（Coins）

- 全局显示在右上角（当前295金币）
- 旁边有购物车图标（商店入口，可直接购买金币）

### 8.2 金币获取途径（从截图可见）

| 来源 | 数量 |
|------|------|
| 通关奖励 | 约10金币/关 |
| Daily Dash（通5关） | 50金币 |
| Bonus Words进度条满 | 25金币 |
| Word Master路径奖励 | +10/+15/+25金币 |
| Daily Reward登录奖励 | 道具Buff为主 |
| 看激励视频 | 金币（具体数量未显示） |
| Facebook登录奖励 | 150金币（一次性） |

### 8.3 金币消耗

| 用途 | 消耗 |
|------|------|
| Wind Hint | 200金币 |
| Normal Hint | 未显示具体价格（推测20-50金币） |
| Rotate | 未显示具体价格 |

---

## 九、UI/UX设计特点

### 9.1 视觉风格

- **自然风景背景**：每个地区有对应的真实风景照片（马来西亚山景、绿叶微距等）
- **色彩体系**：绿色（主按钮/正向操作）、红色（活动/重要提示）、蓝色（信息/链接）、黄色/金色（金币/奖励）
- **圆角卡片**：所有弹窗和信息卡片都使用大圆角设计
- **图标风格**：扁平化+微立体，色彩鲜明

### 9.2 交互设计

- **一键进入**：主页大绿色按钮"Play Level 35"，最短路径进入游戏
- **弹窗叠加**：活动信息通过弹窗展示，不离开主页
- **进度可视化**：进度条无处不在（地区进度、Bonus进度、活动进度、收藏进度）
- **倒计时制造紧迫感**：多个活动同时显示倒计时（12h43m、23h43m、12:43:17）

### 9.3 信息层级

```
第一层：主页（一键开始游戏）
第二层：活动弹窗（从主页左侧入口触发）
第三层：收藏系统（底部Tab切换）
第四层：游戏内道具（底部固定栏）
```

---

## 十、整体游戏流程图

```
启动加载 → 登录选择（FB/Apple/Guest）
    ↓
主页（Home）
    ├── Play Level X → 进入常规关卡 → 找词 → 结算 → 下一关
    ├── Daily Challenge → 名言填空关卡 → 收集名言
    ├── 左侧活动入口：
    │   ├── Word Master → 路径奖励（找词推进）
    │   ├── Daily Dash → 通5关得50金币
    │   ├── Weekday Tournament → 竞赛排名（收集智慧叶子）
    │   └── 主题头像活动（如Jurassic Titans）→ 收集代币开箱
    ├── Daily Reward → 每日登录领奖
    ├── My Collection → 收藏系统
    │   ├── Stamps（邮票，通关解锁）
    │   ├── Avatars（头像，限时活动开箱）
    │   ├── Crowns（皇冠，季节活动）
    │   ├── Frames（头像框，River Race）
    │   └── Quotes（名言，Daily Challenge）
    └── 商店（金币包/礼包/去广告）/设置
```

---

## 十一、关键设计洞察

### 11.1 多层活动叠加驱动DAU

主页同时展示3-4个活动入口（Word Master、Daily Dash、Tournament、Daily Reward），每个活动都有独立倒计时，制造"错过就没了"的紧迫感。玩家每次登录都有多个理由继续玩。

### 11.2 收藏系统的超长线设计

Stamps系统从Level 91一直延伸到Level 2491+，按约80关一个邮票的节奏，提供了数千关的长线目标。配合四季轮换的Crowns和Frames，确保玩家始终有收集目标。

### 11.3 道具即广告入口

道具栏中"FREE"和"GET"标签直接关联激励视频，Wind Hint定价200金币制造金币饥渴，最终引导玩家观看广告获取金币/道具。

### 11.4 社交元素的轻量植入

- "Solved by 49.96% players" — 全球通过率比较
- "大牛 已连接" — 好友/对战连接
- Weekday Tournament — 竞赛排名
- 头像+昵称系统 — 个人身份展示
- 头像框/皇冠 — 社交炫耀装饰

### 11.5 季节性内容更新机制（结合官方文档推断）

官方帮助文档中没有直接提到 Crowns 和 Frames 的季节系统，说明这是后续版本迭代加入的功能。结合截图和 River Race 官方说明，可以拼出完整的更新逻辑：

**Crowns（皇冠）积分来源：**
- 截图显示 Spring 赛季 Mar 2 - Jun 1，进度条节点 0→10→30
- 官方文档无对应说明，但从 Weekday Tournament 机制推断：皇冠积分大概率来自 Tournament 类竞赛活动
- 每个赛季约3个月，赛季结束时积分清零，下个赛季开启新的皇冠样式

**Frames（头像框）积分来源：**
- 截图底部明确标注 "Play River Race to unlock more frames!"
- 官方文档说明 River Race 每周一和周四开放，收集船舵（Helms）→ 赢得 Frame points → 解锁头像框
- 完整链路：**River Race 收集 Helms → 转化为 Frame points → 在当前赛季进度条上累积（0→10→40）→ 解锁赛季限定头像框**
- 每个赛季的 Frame points 独立计算（与 River Race "previous helms do not contribute" 的设计一致）

**季节轮换规则：**

| 赛季 | 大致时间 | 持续 |
|------|----------|------|
| Spring | 3月初 - 6月初 | ~3个月 |
| Summer | 6月初 - 9月初 | ~3个月 |
| Fall | 9月初 - 12月初 | ~3个月 |
| Winter | 12月初 - 3月初 | ~3个月 |

**设计本质：** 季节性内容不需要开发新玩法，它是在现有活动（Tournament → Crowns，River Race → Frames）之上叠加了一层"赛季制"包装。每个赛季自动更换皇冠和头像框的美术资源，积分清零重新开始收集。用时间窗口制造稀缺性和FOMO（错过就要等一年），零开发成本即可保持收集系统的新鲜感。
