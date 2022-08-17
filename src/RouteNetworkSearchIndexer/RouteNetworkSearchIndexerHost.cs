using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RouteNetworkSearchIndexer.Config;
using RouteNetworkSearchIndexer.RouteNetwork;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Typesense;

namespace RouteNetworkSearchIndexer
{
    public class RouteNetworkSearchIndexerHost : IHostedService
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {nameof(RouteNetworkSearchIndexerHost)}.");

            _applicationLifetime.ApplicationStarted.Register(OnStarted);
            _applicationLifetime.ApplicationStopping.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("Checking Typesense collections.");
            CreateNodeCollectionTypesense().Wait();

            _logger.LogInformation("Starting to consume RouteNodeEvents.");
            _routeNetworkConsumer.Consume();

            _logger.LogInformation("Marked as healthy.");
            MarkAsReady();
        }

        private void MarkAsReady()
        {
            File.Create("/tmp/healthy");
        }

        private void OnStopped()
        {
            if (_routeNetworkConsumer is not null)
                _routeNetworkConsumer.Dispose();

            _logger.LogInformation("Stopped");
        }

        private async Task CreateNodeCollectionTypesense()
        {
            var collections = await _typesense.RetrieveCollections();
            var collection = collections
                .FirstOrDefault(x => x.Name == TypesenseCollectionConfig.Name);

            if (collection is null)
            {
                var schema = new Schema
                {
                    Name = TypesenseCollectionConfig.Name,
                    Fields = new List<Field>
                    {
                        new Field("name", FieldType.String, false, true),
                    },
                };

                _logger.LogInformation(
                    "Creating Typesense collection '{CollectionName}'.",
                    TypesenseCollectionConfig.Name);

                await _typesense.CreateCollection(schema);
            }
        }
    }
}
