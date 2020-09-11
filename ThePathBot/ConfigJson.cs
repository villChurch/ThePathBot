using Newtonsoft.Json;

namespace ThePathBot
{
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }
        [JsonProperty("prefix")]
        public string Prefix { get; private set; }
        [JsonProperty("databaseName")]
        public string databaseName { get; private set; }
        [JsonProperty("databaseServer")]
        public string databaseServer { get; private set; }
        [JsonProperty("databasePassword")]
        public string databasePassword { get; private set; }
        [JsonProperty("databaseUser")]
        public string databaseUser { get; private set; }
        [JsonProperty("databasePort")]
        public string databasePort { get; private set; }
        [JsonProperty("githubToken")]
        public string gitHubToken { get; private set; }
    }
}
