using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CHEF.Components.Watcher.Spam
{
    public class SearchResult
    {
        [JsonProperty("analytics_id")]
        public string AnalyticsId { get; set; }
        [JsonProperty("total_results")]
        public int TotalResults { get; set; }
        [JsonProperty("messages")]
        public List<List<SocketMessage>> Messages { get; set; }
    }
}
