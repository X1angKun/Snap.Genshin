﻿using Newtonsoft.Json;

namespace DGP.Genshin.Models.MiHoYo
{
    public class GachaLogItem
    {
        [JsonProperty("uid")] public string Uid { get; set; }
        [JsonProperty("gacha_type")] public string GachaType { get; set; }
        [JsonProperty("item_id")] public string ItemId { get; set; }
        [JsonProperty("count")] public string Count { get; set; }
        [JsonProperty("time")] public string Time { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("lang")] public string Language { get; set; }
        [JsonProperty("item_type")] public string ItemType { get; set; }
        [JsonProperty("rank_type")] public string RankType { get; set; }
    }
}
