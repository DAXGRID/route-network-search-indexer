using Newtonsoft.Json;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    public record TypesenseRouteNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("northCoordinate")]
        public string NorthCoordinate { get; set; }
        [JsonProperty("eastCoordinate")]
        public string EastCoordinate { get; set; }
    }
}
