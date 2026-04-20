using System.Collections.Generic;
using System.Linq;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 道具配置查询。
    /// </summary>
    public class ItemConfigMgr
    {
        private static ItemConfigMgr _instance;
        public static ItemConfigMgr Instance => _instance ??= new ItemConfigMgr();

        public ItemConfig GetItemConfig(string itemId)
        {
            return ConfigSystem.Instance.Tables.TbItemConfig.GetOrDefault(itemId);
        }

        public List<ItemConfig> GetAvailableItems(string version = "mvp")
        {
            return ConfigSystem.Instance.Tables.TbItemConfig.DataList
                .Where(i => i.Version == version)
                .OrderBy(i => i.SlotPosition)
                .ToList();
        }
    }
}
