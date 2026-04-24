/*
 * Word Search Generator - Level Profile
 *
 * 难度档位配置（ScriptableObject）：
 *   把"骨架配额 / 方向集 / 采样 / Best-of-N"等生成参数捆绑成可命名的档位，
 *   UI 侧用一个下拉切换即可产出风格差异明显的关卡。
 *
 *   stage（内容大类 primary/junior）与 difficultyTier（骨架难度）是正交的：
 *     同一 stage 可以跨多个 tier，同一 tier 可以跑在不同 stage 的词表上。
 *
 *   用法：
 *     - 在 Unity 右键菜单 "Word Search / Level Profile" 创建自定义 .asset 覆盖默认；
 *     - 或直接调用 LevelProfile.GetBuiltIns() 拿到 6 个内存内置档位（无需 .asset 文件）。
 */

using System.Collections.Generic;
using UnityEngine;

namespace WordSearchGenerator
{
    /// <summary>
    /// 允许的方向集预设（对应 Constants 里的 Vector2Int[]）。
    /// 参考图中罕见 ↗↖（Anti-Diagonal），所以中间档位默认剔除这两个方向。
    /// </summary>
    public enum DirectionPreset
    {
        FourForward = 0,  // → ↓ ↘ ↗          （4 方向，仅正向，最简单）
        SixNoAntiDiag,    // → ← ↓ ↑ ↘ ↙      （6 方向，保留 ↘↙，去掉 ↗↖）
        EightAll,         // 全 8 向
    }

    /// <summary>
    /// 档位配置对象。字段直接映射到生成器参数：
    ///   - 骨架配额：决定 LayoutScorer 的配额缺口加成
    ///   - 方向集：决定候选位置的搜索空间
    ///   - 采样：决定 Generator.Top-K 行为
    ///   - Best-of-N：决定生成器做几次择优
    /// </summary>
    [CreateAssetMenu(fileName = "LevelProfile", menuName = "Word Search/Level Profile")]
    public class LevelProfile : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("显示名，用于 UI 下拉")]
        public string tierName = "Normal";

        [Header("骨架配额（目标值）")]
        [Tooltip("希望有多少条贴边长词（4 条边中最多占 4）")]
        public int borderQuota = 3;

        [Tooltip("希望有多少条对角词")]
        public int diagonalQuota = 2;

        [Tooltip("希望形成多少个 X 十字（两对角在中心带相交）")]
        public int xCrossQuota = 1;

        [Tooltip("希望有多少对相邻列的竖直栈")]
        public int verticalStackQuota = 2;

        [Range(0f, 1f)]
        [Tooltip("贴边长词走反向的偏好强度（参考图中贴边词多走反向）")]
        public float borderReversePreference = 0.6f;

        [Header("方向集")]
        public DirectionPreset directionPreset = DirectionPreset.SixNoAntiDiag;

        [Header("交叉偏好")]
        [Tooltip("-1 避免 / 0 随机 / +1 偏好")]
        [Range(-1, 1)]
        public int intersectBias = Constants.INTERSECT_BIAS_AVOID;

        [Header("采样")]
        [Tooltip("Top-K 采样中的 K（1=贪心）")]
        [Range(1, 8)]
        public int topK = 3;

        [Tooltip("Softmax 温度（0=贪心，越大越随机）")]
        [Range(0f, 2f)]
        public float softmaxTemperature = 0.5f;

        [Header("Best-of-N")]
        [Tooltip("单次生成尝试多少种布局，取综合分最高者")]
        [Range(1, 32)]
        public int bestOfN = 3;

        [Header("尺寸策略")]
        [Tooltip("自动尺寸 baseDim × scale + padding")]
        public float dimensionScale = 1.15f;

        // ========== 静态工厂：6 档内置档位 ==========

        public static readonly string[] BuiltInTiers =
        {
            "Tutorial", "Easy", "Normal", "Hard", "Expert", "Event"
        };

        /// <summary>
        /// 获取 6 个内存内置档位实例（不写 .asset，使用 ScriptableObject.CreateInstance）。
        /// 调用方持有引用即可使用；不持有时会被 GC 回收。
        /// </summary>
        public static List<LevelProfile> GetBuiltIns()
        {
            return new List<LevelProfile>
            {
                Build("Tutorial",  2, 0, 0, 0, 0.0f, DirectionPreset.FourForward,
                                     Constants.INTERSECT_BIAS_AVOID, 1, 0.0f, 1, 1.10f),
                Build("Easy",      3, 1, 0, 1, 0.3f, DirectionPreset.FourForward,
                                     Constants.INTERSECT_BIAS_AVOID, 3, 0.4f, 2, 1.12f),
                Build("Normal",    3, 2, 0, 2, 0.6f, DirectionPreset.SixNoAntiDiag,
                                     Constants.INTERSECT_BIAS_AVOID, 3, 0.5f, 3, 1.15f),
                Build("Hard",      3, 2, 1, 2, 0.6f, DirectionPreset.SixNoAntiDiag,
                                     Constants.INTERSECT_BIAS_AVOID, 4, 0.6f, 5, 1.15f),
                Build("Expert",    4, 3, 1, 3, 0.5f, DirectionPreset.EightAll,
                                     Constants.INTERSECT_BIAS_RANDOM, 5, 0.8f, 8, 1.18f),
                Build("Event",     4, 3, 1, 3, 0.5f, DirectionPreset.EightAll,
                                     Constants.INTERSECT_BIAS_AVOID, 5, 1.0f, 8, 1.18f),
            };
        }

        private static LevelProfile Build(
            string name, int borderQ, int diagQ, int xQ, int vstackQ, float revPref,
            DirectionPreset dirs, int bias, int topK, float T, int N, float scale)
        {
            var p = ScriptableObject.CreateInstance<LevelProfile>();
            p.tierName                = name;
            p.borderQuota             = borderQ;
            p.diagonalQuota           = diagQ;
            p.xCrossQuota             = xQ;
            p.verticalStackQuota      = vstackQ;
            p.borderReversePreference = revPref;
            p.directionPreset         = dirs;
            p.intersectBias           = bias;
            p.topK                    = topK;
            p.softmaxTemperature      = T;
            p.bestOfN                 = N;
            p.dimensionScale          = scale;
            return p;
        }

        // ========== 方向集映射 ==========

        private static readonly Vector2Int[] SIX_NO_ANTI_DIAG = new Vector2Int[]
        {
            new Vector2Int( 1,  0),  // →
            new Vector2Int(-1,  0),  // ←
            new Vector2Int( 0,  1),  // ↓
            new Vector2Int( 0, -1),  // ↑
            new Vector2Int( 1,  1),  // ↘
            new Vector2Int(-1,  1),  // ↙
        };

        /// <summary>
        /// 把方向预设枚举解析成具体的 Vector2Int[]。
        /// </summary>
        public static Vector2Int[] GetDirections(DirectionPreset preset)
        {
            switch (preset)
            {
                case DirectionPreset.FourForward:   return Constants.EASY_DIRECTIONS;
                case DirectionPreset.EightAll:      return Constants.ALL_DIRECTIONS;
                case DirectionPreset.SixNoAntiDiag: return SIX_NO_ANTI_DIAG;
                default:                            return Constants.EASY_DIRECTIONS;
            }
        }
    }
}
