# GameConfig Luban 使用说明

## 目的

这份文档记录 `Configs/GameConfig` 目录下 Luban 的实际使用方式、配表规范和常见注意事项，方便后续新增配置表或修改现有表时直接参考。

## 当前项目的真实入口

本项目生成配置时，实际使用的是下面这些文件：

- 主配置：`Configs/GameConfig/luban.conf`
- 表注册：`Configs/GameConfig/Datas/__tables__.xlsx`
- 同步源：`Configs/GameConfig/Datas/__tables__.csv`
- Bean 定义：`Configs/GameConfig/Datas/__beans__.xlsx`
- Enum 定义：`Configs/GameConfig/Datas/__enums__.xlsx`
- 生成脚本：`Configs/GameConfig/gen_code_bin_to_project_lazyload.sh`

当前 `luban.conf` 中的 schema 配置如下：

- `Datas/__tables__.xlsx`
- `Datas/__beans__.xlsx`
- `Datas/__enums__.xlsx`

这意味着：

- **Luban 实际读取的是 `__tables__.xlsx`**
- **日常维护建议先改 `__tables__.csv`，再用脚本同步到 `__tables__.xlsx`**

## 推荐工作流

当前项目推荐的维护顺序是：

1. 修改 `Datas/__tables__.csv`
2. 执行同步脚本，把 csv 覆盖到 xlsx
3. 执行生成脚本

如果想直接一步完成，也可以执行一键脚本：

```bash
cd Configs/GameConfig
sh ./sync_and_gen.sh
```

同步命令：

```bash
cd Configs/GameConfig
. .venv_xlsx/bin/activate
python sync_tables_csv_to_xlsx.py
```

生成命令：

```bash
sh ./gen_code_bin_to_project_lazyload.sh
```

### 为什么这样做

- `csv` 更适合程序维护和查看 diff
- `xlsx` 仍然可以作为 Luban 的实际输入
- 两者通过脚本保持同步，避免手工双改

## `sync_tables_csv_to_xlsx.py` 说明

脚本位置：

- `Configs/GameConfig/sync_tables_csv_to_xlsx.py`

功能：

- 读取 `Datas/__tables__.csv`
- 覆盖写入 `Datas/__tables__.xlsx`
- 保留 xlsx 所需的表头说明格式
- 自动把 csv 列顺序转换成 xlsx 当前使用的列顺序

注意：

- `__tables__.csv` 是维护源
- `__tables__.xlsx` 是 Luban 实际读取文件
- 如果只改了 xlsx，没有回写 csv，下次同步时会被 csv 覆盖

## `sync_and_gen.sh` 说明

脚本位置：

- `Configs/GameConfig/sync_and_gen.sh`

功能：

1. 自动同步 `Datas/__tables__.csv` 到 `Datas/__tables__.xlsx`
2. 自动执行 `gen_code_bin_to_project_lazyload.sh`

适用场景：

- 日常改完 `__tables__.csv` 后直接一键生成
- 避免忘记先同步再转表

## 生成方式

在 `Configs/GameConfig` 目录执行：

```bash
sh ./gen_code_bin_to_project_lazyload.sh
```

脚本会做这些事：

1. 调用 `Tools/Luban/Luban.dll`
2. 使用 target `client`
3. 生成 `cs-bin` 代码和 `bin` 数据
4. 输出到 Unity 工程

输出目录：

- 代码：`UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/`
- 数据：`UnityProject/Assets/AssetRaw/Configs/bytes/`

## 配表总规则

### 1. 所有业务表统一使用 `##var`

当前项目已经统一为：

- 第一行必须以 `##var` 开头
- 每条数据行第一列必须是 `var` 名

示例：

```csv
##var,stage_id,stage_name,sort_order,start_pack_id,unlock_next_stage
##type,string,string,int,string,string?
##group,c,c,c,c,c
##,学段ID,学段名称,排序序号,起始关卡包ID,完成后解锁的下一学段ID
stage_primary,primary,小学,1,primary_self_intro,junior_high
stage_junior_high,junior_high,初中,2,junior_daily_life,senior_high
```

### 2. `var` 列的命名建议

建议使用：

- `表语义前缀 + 主键值`

例如：

- `stage_primary`
- `pack_primary_self_intro`
- `daily_reward_7`
- `event_weekday_tournament`
- `season_crown_spring_2026`

要求：

- 全表内唯一
- 可读
- 尽量稳定，不要频繁改名

### 3. 表头结构

普通 csv/xlsx 业务表统一使用四行头：

1. 第 1 行：字段名行，A1 为 `##var`
2. 第 2 行：字段类型行，A1 为 `##type`
3. 第 3 行：字段分组行，A1 为 `##group`
4. 第 4 行：注释行，A1 为 `##`
5. 第 5 行起：数据行

### 4. 当前项目分组使用建议

当前生成脚本使用的是 `-t client`，所以当前表里常用的是 `c` 分组。

如果某张表要参与当前这条生成链路，字段分组至少要满足客户端可导出。

## 如何新增一张配置表

### 步骤 1：创建数据表

在 `Datas/local/` 或 `Datas/server/` 下新建 csv 文件，推荐命名风格：

- `xxx_config.csv`
- `xxx_rule.csv`
- `xxx_reward.csv`

表内容按 `##var` 规范填写。

### 步骤 2：在 `__tables__.csv` 注册

需要填写这些关键列：

- `full_name`
- `value_type`
- `read_schema_from_file`
- `input`
- `index`
- `mode`
- `group`
- `comment`

写完后执行同步脚本，把内容同步到 `__tables__.xlsx`。

常见填写方式：

- `full_name`：`cfg.TbStageConfig`
- `value_type`：`StageConfig`
- `read_schema_from_file`：`True`
- `input`：`local/stage_config.csv`
- `index`：`stage_id`
- `mode`：`map`
- `group`：`c`

### 步骤 3：同步到 `__tables__.xlsx`

```bash
cd Configs/GameConfig
. .venv_xlsx/bin/activate
python sync_tables_csv_to_xlsx.py
```

### 步骤 4：执行生成脚本

```bash
sh ./gen_code_bin_to_project_lazyload.sh
```

### 步骤 5：检查产物

至少检查这三处：

- `UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/Tables.cs`
- `UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/cfg/TbXxx.cs`
- `UnityProject/Assets/AssetRaw/Configs/bytes/cfg_tbxxx.bytes`

## `__tables__.csv` 字段说明

### `full_name`

表完整类型名，例如：

- `cfg.TbStageConfig`
- `cfg.TbPackConfig`

### `value_type`

单条记录的数据类型名，例如：

- `StageConfig`
- `PackConfig`

### `read_schema_from_file`

当前项目建议统一填：

- `True`

表示从业务表本身的表头读取字段定义。

### `input`

输入数据文件相对路径，例如：

- `local/stage_config.csv`
- `server/event_config.csv`

### `index`

主键字段配置。

单主键示例：

- `stage_id`
- `rule_id`
- `word`

联合主键示例：

- `season_id+node_index`
- `tournament_type+rank_min+rank_max`

注意：

- `+` 表示联合主键
- `,` 表示独立索引

### `mode`

常用有：

- `map`
- `list`
- `one`

当前项目经验：

- 单主键表通常用 `map`
- 多主键表必须用 `list`

不要把多主键表配成单主键 `map`，否则生成时会报错。

## 多主键表配置规则

这次项目里已经验证过的正确写法：

### 联合主键表

例如：

- `SeasonCrownConfig`
- `SeasonFrameConfig`
- `TournamentRewardConfig`

在 `__tables__.csv` 里应该写成：

- `index = season_id+node_index`
- `mode = list`

或：

- `index = tournament_type+rank_min+rank_max`
- `mode = list`

### 为什么不能继续用 `map`

因为当前这版 Luban 会把 `map` 视为单主键表模式。

如果 `index` 里配置了多个 key，但 `mode` 仍然是 `map`，会报类似错误：

- 单主键表不能包含多个 key
- 主键重复

## 当前项目已经验证通过的表

目前这批表已经完成 `##var` 规范化，并且生成成功：

- `StageConfig`
- `PackConfig`
- `WordPool`
- `ItemConfig`
- `BadgeConfig`
- `LandmarkCardConfig`
- `CoinRule`
- `AntiAddictionConfig`
- `AdConfig`
- `AvatarEventConfig`
- `AvatarEventItem`
- `DailyChallengeConfig`
- `DailyDashConfig`
- `DailyRewardConfig`
- `EventConfig`
- `ProvinceMilestoneConfig`
- `SeasonConfig`
- `SeasonCrownConfig`
- `SeasonFrameConfig`
- `ShareRewardConfig`
- `StreakConfig`
- `TournamentRewardConfig`
- `WordMasterConfig`

## 常见坑

### 1. 改了 `__tables__.csv` 但生成没变化

原因：

- 当前项目实际使用的是 `__tables__.xlsx`
- 修改 csv 后还没有执行同步脚本

结论：

- 先改 csv，再执行 `python sync_tables_csv_to_xlsx.py`

### 2. 用了 `##var`，但数据行没有 var 列

这会导致字段整体错位，常见现象是：

- string 被读到 int 字段
- 某一列看起来总是类型错误

结论：

- 只要第一行是 `##var`，数据行第一列就必须是真实的 `var`

### 3. 多主键表主键重复

如果同一个字段值在多行重复，例如：

- `node_index = 1` 在多个 season 中重复

那么它就不适合做单主键 `map` 表。

应改成：

- 联合主键
- `mode = list`

### 4. 新表生成了代码，但没有在 `Tables.cs` 里出现

优先检查：

- 是否注册到了 `__tables__.xlsx`
- `group` 是否正确
- `read_schema_from_file` 是否为 `True`
- `input` 路径是否正确

### 5. 生成成功，但找不到 `.bytes`

检查：

- 是否真的执行了当前脚本
- 输出目录是否是 `UnityProject/Assets/AssetRaw/Configs/bytes/`
- `Tables.cs` 中默认 loader 对应的文件名是什么

## 建议维护方式

后续维护建议遵循：

1. 只维护 `__tables__.xlsx`
1. 以 `__tables__.csv` 为维护源
2. 每次修改完先同步到 `__tables__.xlsx`
3. 所有业务表统一使用 `##var`
4. `var` 名保持稳定、可读
5. 单主键表优先 `map`
6. 多主键表显式配置联合主键，并使用 `list`
7. 每次新增表后立即跑一次生成验证

这样后续再加表时，基本不会再踩这次遇到的几个坑。
