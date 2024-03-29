using Newtonsoft.Json;

namespace RouteNetworkSearchIndexer.RouteNetwork;

internal sealed record TypesenseRouteNode
{
    [JsonProperty("id")]
    public string Id { get; init; }

    [JsonProperty("name")]
    public string Name { get; init; }

    [JsonConstructor]
    public TypesenseRouteNode(string id, string name)
    {
        Id = id;
        Name = name;
    }
}
