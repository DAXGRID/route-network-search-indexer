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
    public class RouteNetworkSearchIndexerHost : BackgroundService
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

            _logger.LogInformation("Checking Typesense collections.");
            await CreateNodeCollectionTypesense().ConfigureAwait(false);

            _logger.LogInformation("Starting to consume RouteNodeEvents.");
            _routeNetworkConsumer.Consume();

            _logger.LogInformation("Marked as healthy.");
            File.Create("/tmp/healthy");
        }

        private async Task CreateNodeCollectionTypesense()
        {
            var collections = await _typesense.RetrieveCollections().ConfigureAwait(false);
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

                await _typesense.CreateCollection(schema).ConfigureAwait(false);
            }
        }

        public override void Dispose()
        {
            if (_routeNetworkConsumer is not null)
                _routeNetworkConsumer.Dispose();

            _logger.LogInformation("Stopped.");
        }
    }
}
