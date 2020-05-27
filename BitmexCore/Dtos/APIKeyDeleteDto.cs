using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitmexCore.Dtos
{
    public class APIKeyDeleteDto
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
