﻿using Newtonsoft.Json;

namespace DGP.Genshin.DataModel.Launching
{
    /// <summary>
    /// 对注册表内信息的抽象
    /// </summary>
    public class GenshinAccount
    {
        /// <summary>
        /// 记录性文本
        /// </summary>
        [JsonProperty("Name")] public string? Name { get; set; }

        /// <summary>
        /// 包含了账户登录信息
        /// </summary>
        [JsonProperty("MIHOYOSDK_ADL_PROD_CN_h3123967166")] public string? MihoyoSDK { get; set; }
    }
}
