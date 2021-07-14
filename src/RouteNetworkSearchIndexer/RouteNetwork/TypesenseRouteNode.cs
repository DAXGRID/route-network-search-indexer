using Newtonsoft.Json;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    public record TypesenseRouteNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
