using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitmexCore.Dtos
{
    public class LeaderboardDto
    {
        [JsonProperty("profit")]
        public double? Profit { get; set; }

        [JsonProperty("isRealName")]
        public bool? IsRealName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
