using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RouteNetworkSearchIndexer.Config;
using RouteNetworkSearchIndexer.RouteNetwork;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Typesense;

namespace RouteNetworkSearchIndexer;

internal sealed class RouteNetworkSearchIndexerHost : BackgroundService
{
    private readonly ILogger<RouteNetworkSearchIndexerHost> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IRouteNetworkConsumer _routeNetworkConsumer;
    private readonly ITypesenseClient _typesense;

    public RouteNetworkSearchIndexerHost(
        ILogger<RouteNetworkSearchIndexerHost> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IRouteNetworkConsumer routeNetworkConsumer,
        ITypesenseClient typesense)
    {
        _logger = logger;
        _applicationLifetime = hostApplicationLifetime;
        _routeNetworkConsumer = routeNetworkConsumer;
        _typesense = typesense;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Starting {nameof(RouteNetworkSearchIndexerHost)}.");

        // We migrate when first time run.
        await MigratePreviousCollection().ConfigureAwait(false);

        await SetupCollection().ConfigureAwait(false);

        _logger.LogInformation("Starting to consume RouteNodeEvents.");
        _routeNetworkConsumer.Consume();

        // This is a hack, it will be removed when solution is using event-store instead.
        var lastReceivedTimeSec = 15;
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(lastReceivedTimeSec), stoppingToken).ConfigureAwait(false);

            var lastMessageReceived = _routeNetworkConsumer.LastMessageReceived();
            if (lastMessageReceived is not null &&
                lastMessageReceived.Value.AddSeconds(lastReceivedTimeSec) < DateTime.UtcNow)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Upserting {CollectionAlias} to {CollectionName}.",
            TypesenseCollectionConfig.AliasName,
            TypesenseCollectionConfig.CollectionName);

        await _typesense.UpsertCollectionAlias(
            TypesenseCollectionConfig.AliasName,
            new CollectionAlias(TypesenseCollectionConfig.CollectionName)).ConfigureAwait(false);

        var collections = await RetrieveMatchingCollectionNames(
            $"{TypesenseCollectionConfig.AliasName}-").ConfigureAwait(false);

        foreach (var collectionName in collections.Where(x => x != TypesenseCollectionConfig.CollectionName))
        {
            _logger.LogInformation("Deleting {Collection}.", collectionName);
            await _typesense.DeleteCollection(collectionName).ConfigureAwait(false);
        }

        File.Create("/tmp/healthy");
        _logger.LogInformation("Marked as healthy.");
    }

    private async Task MigratePreviousCollection()
    {
        try
        {
            await _typesense.DeleteCollection(TypesenseCollectionConfig.AliasName)
                .ConfigureAwait(false);

            _logger.LogInformation("Migration: deleting previous collection.");
        }
        catch (TypesenseApiNotFoundException)
        {
            // this is valid in case we have already migrated.
        }
    }

    private async Task SetupCollection()
    {
        var schema = new Schema(
            name: TypesenseCollectionConfig.CollectionName,
            fields: new List<Field>
            {
                    new Field("name", FieldType.String, false, true),
            });

        _logger.LogInformation(
            "Creating Typesense collection '{CollectionName}'.",
            TypesenseCollectionConfig.CollectionName);

        await _typesense.CreateCollection(schema).ConfigureAwait(false);
    }

    private async Task<IEnumerable<string>> RetrieveMatchingCollectionNames(string prefix)
    {
        return (await _typesense.RetrieveCollections().ConfigureAwait(false))
            .Where(x => x.Name.StartsWith(prefix))
            .Select(x => x.Name);
    }

    public override void Dispose()
    {
        if (_routeNetworkConsumer is not null)
            _routeNetworkConsumer.Dispose();

        _logger.LogInformation("Stopped.");
    }
}
