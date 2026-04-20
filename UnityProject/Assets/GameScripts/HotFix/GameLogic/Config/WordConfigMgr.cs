using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 单词池配置查询。
    /// </summary>
    public class WordConfigMgr
    {
        private static WordConfigMgr _instance;
        public static WordConfigMgr Instance => _instance ??= new WordConfigMgr();

        public WordPool GetWordPool(string word)
        {
            return ConfigSystem.Instance.Tables.TbWordPool.GetOrDefault(word.ToUpper());
        }
    }
}
