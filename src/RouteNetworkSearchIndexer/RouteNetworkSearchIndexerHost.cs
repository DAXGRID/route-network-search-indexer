using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenFTTH.EventSourcing;
using RouteNetworkSearchIndexer.Config;
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
    private const int CATCHUP_INTERVAL_MS = 125;
    private readonly ILogger<RouteNetworkSearchIndexerHost> _logger;
    private readonly ITypesenseClient _typesense;
    private readonly IEventStore _eventStore;

    public RouteNetworkSearchIndexerHost(
        ILogger<RouteNetworkSearchIndexerHost> logger,
        ITypesenseClient typesense,
        IEventStore eventStore)
    {
        _logger = logger;
        _typesense = typesense;
        _eventStore = eventStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Starting {nameof(RouteNetworkSearchIndexerHost)}.");

        // We migrate when first time run.
        await MigratePreviousCollection().ConfigureAwait(false);

        _logger.LogInformation(
            "Creating Typesense collection '{CollectionName}'.",
            TypesenseCollectionConfig.CollectionName);
        await SetupCollection().ConfigureAwait(false);

        _logger.LogInformation("Starting initial dehydration of projections.");
        await _eventStore.DehydrateProjectionsAsync(stoppingToken).ConfigureAwait(false);
        _logger.LogInformation("Finished initial dehydration.");

        _logger.LogInformation(
            "Upserting {CollectionAlias} to {CollectionName}.",
            TypesenseCollectionConfig.AliasName,
            TypesenseCollectionConfig.CollectionName);

        await _typesense
            .UpsertCollectionAlias(
                TypesenseCollectionConfig.AliasName,
                new CollectionAlias(TypesenseCollectionConfig.CollectionName))
            .ConfigureAwait(false);

        var collections =
            (await RetrieveMatchingCollectionNames(
                $"{TypesenseCollectionConfig.AliasName}-")
             .ConfigureAwait(false))
            .Where(x => x != TypesenseCollectionConfig.CollectionName)
            .ToList()
            .AsReadOnly();

        foreach (var collectionName in collections)
        {
            _logger.LogInformation("Deleting {Collection}.", collectionName);
            await _typesense.DeleteCollection(collectionName).ConfigureAwait(false);
        }

        File.Create("/tmp/healthy");
        _logger.LogInformation("Marked as healthy.");

        _logger.LogInformation("Starting listening for new events.");
        while (!stoppingToken.IsCancellationRequested)
        {
            await _eventStore.CatchUpAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(CATCHUP_INTERVAL_MS, stoppingToken).ConfigureAwait(false);
        }
    }

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "{HostName} stopped.",
            nameof(RouteNetworkSearchIndexerHost));

        return Task.CompletedTask;
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

        await _typesense.CreateCollection(schema).ConfigureAwait(false);
    }

    private async Task<IEnumerable<string>> RetrieveMatchingCollectionNames(string prefix)
    {
        return (await _typesense.RetrieveCollections().ConfigureAwait(false))
            .Where(x => x.Name.StartsWith(prefix))
            .Select(x => x.Name);
    }
}
