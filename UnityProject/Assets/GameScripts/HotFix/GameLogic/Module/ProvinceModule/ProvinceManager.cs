using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 省份管理器。通过微信API获取地理位置，自动分配省份。
    /// MVP 阶段使用默认值，后续接入微信小程序 wx.getLocation。
    /// </summary>
    public class ProvinceManager : Singleton<ProvinceManager>
    {
        private const string KEY_PROVINCE_CODE = "player_province_code";
        private const string KEY_PROVINCE_NAME = "player_province_name";
        private const string KEY_CITY_CODE     = "player_city_code";

        private string _provinceCode;
        private string _provinceName;

        protected override void OnInit()
        {
            _provinceCode = PlayerDataStorage.GetString(KEY_PROVINCE_CODE, "");
            _provinceName = PlayerDataStorage.GetString(KEY_PROVINCE_NAME, "");
        }

        public string ProvinceCode => _provinceCode;
        public string ProvinceName => _provinceName;
        public bool HasProvince => !string.IsNullOrEmpty(_provinceCode);

        /// <summary>
        /// 请求获取省份信息。
        /// MVP 阶段：如果微信API不可用，使用默认值。
        /// </summary>
        public void RequestProvince()
        {
            if (HasProvince) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            // 微信小游戏环境：调用 wx.getLocation
            RequestWxLocation();
#else
            // 编辑器/非微信环境：使用默认值
            SetProvince("440000", "广东省");
#endif
        }

        /// <summary>
        /// 设置省份（由微信回调或手动设置）。
        /// </summary>
        public void SetProvince(string code, string name)
        {
            _provinceCode = code;
            _provinceName = name;
            PlayerDataStorage.SetString(KEY_PROVINCE_CODE, code);
            PlayerDataStorage.SetString(KEY_PROVINCE_NAME, name);
            Log.Info($"[ProvinceManager] Province set: {code} {name}");
        }

        /// <summary>
        /// 获取省份贡献文案。
        /// </summary>
        public string GetContributionText()
        {
            if (!HasProvince) return "";
            int score = EconomyManager.Instance.GetLearningScore();
            return $"你为{_provinceName}贡献了 {score} 学习积分";
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void RequestWxLocation()
        {
            // TODO: 接入微信小游戏 wx.getLocation API
            // WX.GetLocation(new GetLocationOption
            // {
            //     type = "wgs84",
            //     success = (res) => {
            //         string province = ReverseGeocode(res.latitude, res.longitude);
            //         SetProvince(provinceCode, provinceName);
            //     },
            //     fail = (res) => {
            //         SetProvince("000000", "未知");
            //     }
            // });
            SetProvince("000000", "未知");
        }
#endif

        /// <summary>
        /// 根据省份代码获取省份名称（简化映射）。
        /// </summary>
        public static string GetProvinceName(string code)
        {
            return code switch
            {
                "110000" => "北京市",
                "120000" => "天津市",
                "310000" => "上海市",
                "500000" => "重庆市",
                "130000" => "河北省",
                "140000" => "山西省",
                "210000" => "辽宁省",
                "220000" => "吉林省",
                "230000" => "黑龙江省",
                "320000" => "江苏省",
                "330000" => "浙江省",
                "340000" => "安徽省",
                "350000" => "福建省",
                "360000" => "江西省",
                "370000" => "山东省",
                "410000" => "河南省",
                "420000" => "湖北省",
                "430000" => "湖南省",
                "440000" => "广东省",
                "450000" => "广西壮族自治区",
                "460000" => "海南省",
                "510000" => "四川省",
                "520000" => "贵州省",
                "530000" => "云南省",
                "540000" => "西藏自治区",
                "610000" => "陕西省",
                "620000" => "甘肃省",
                "630000" => "青海省",
                "640000" => "宁夏回族自治区",
                "650000" => "新疆维吾尔自治区",
                "150000" => "内蒙古自治区",
                "710000" => "台湾省",
                "810000" => "香港特别行政区",
                "820000" => "澳门特别行政区",
                _ => "未知",
            };
        }
    }
}
