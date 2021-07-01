using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RouteNetworkSearchIndexer.RouteNetwork;

namespace RouteNetworkSearchIndexer
{
    public class RouteNetworkSearchIndexerHost : IHostedService
    {
        private readonly ILogger<RouteNetworkSearchIndexerHost> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IRouteNetworkConsumer _routeNetworkConsumer;

        public RouteNetworkSearchIndexerHost(
            ILogger<RouteNetworkSearchIndexerHost> logger,
            IHostApplicationLifetime hostApplicationLifetime,
            IRouteNetworkConsumer routeNetworkConsumer = null)
        {
            _logger = logger;
            _applicationLifetime = hostApplicationLifetime;
            _routeNetworkConsumer = routeNetworkConsumer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {nameof(RouteNetworkSearchIndexerHost)}");

            _applicationLifetime.ApplicationStarted.Register(OnStarted);
            _applicationLifetime.ApplicationStopping.Register(OnStopped);

            MarkAsReady();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void MarkAsReady()
        {
            File.Create("/tmp/healthy");
        }

        private void OnStarted()
        {
            _logger.LogInformation("Starting to consume RouteNodeEvents");
            _routeNetworkConsumer.Consume();
        }

        private void OnStopped()
        {
            _routeNetworkConsumer.Dispose();
            _logger.LogInformation("Stopped");
        }
    }
}
