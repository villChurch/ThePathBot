using System;
using Newtonsoft.Json;

namespace ThePathBot.Models
{
    public class GithubIssue
    {
        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("body")]
        public string body { get; set; }
    }
}
