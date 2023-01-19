namespace RouteNetworkSearchIndexer.Config;

internal sealed record KafkaSetting
{
    public string Consumer { get; init; }
    public string Server { get; init; }
    public string PositionConnectionString { get; init; }
    public string Topic { get; init; }
}
