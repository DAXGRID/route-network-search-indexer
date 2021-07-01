using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OpenFTTH.Events.RouteNetwork;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    public class RouteNetworkEventHandler : IRequestHandler<RouteNetworkEditOperationOccuredEvent>
    {
        private readonly ILogger<RouteNetworkEventHandler> _logger;

        public RouteNetworkEventHandler(ILogger<RouteNetworkEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task<Unit> Handle(RouteNetworkEditOperationOccuredEvent request,
                                       CancellationToken cancellationToken)
        {
            _logger.LogInformation("Test");
            foreach (var command in request.RouteNetworkCommands)
            {
                foreach (var routeNetworkEvent in command?.RouteNetworkEvents)
                {
                    switch (routeNetworkEvent)
                    {
                        case RouteNodeAdded domainEvent:
                            break;

                        case RouteNodeMarkedForDeletion domainEvent:
                            break;

                        case RouteNodeGeometryModified:
                            break;

                        case RouteNodeInfoModified domainEvent:
                            break;
                    }
                }
            }

            return await Unit.Task;
        }
    }
}
