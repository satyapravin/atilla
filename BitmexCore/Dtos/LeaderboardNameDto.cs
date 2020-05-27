using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitmexCore.Dtos
{
    public class LeaderboardNameDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
