using System;

namespace RouteNetworkSearchIndexer.Config;

internal static class TypesenseCollectionConfig
{
    public const string AliasName = "RouteNodes";
    public static string CollectionName = $"{AliasName}-{Guid.NewGuid()}";
}
