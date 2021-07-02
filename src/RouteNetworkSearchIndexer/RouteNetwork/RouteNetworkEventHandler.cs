using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenFTTH.Events.RouteNetwork;
using Typesense;
using OpenFTTH.Events.Core;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    public class RouteNetworkEventHandler : IRequestHandler<RouteNetworkEditOperationOccuredEvent>
    {
        private readonly ILogger<RouteNetworkEventHandler> _logger;
        private readonly ITypesenseClient _typesense;

        public RouteNetworkEventHandler(ILogger<RouteNetworkEventHandler> logger, ITypesenseClient typesense)
        {
            _logger = logger;
            _typesense = typesense;
        }

        public async Task<Unit> Handle(RouteNetworkEditOperationOccuredEvent request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received message");
            foreach (var command in request.RouteNetworkCommands)
            {
                foreach (var routeNetworkEvent in command?.RouteNetworkEvents)
                {
                    switch (routeNetworkEvent)
                    {
                        // case RouteNodeAdded domainEvent:
                        //     await HandleRouteNodeAdded(domainEvent);
                        //     break;
                        // case RouteNodeMarkedForDeletion domainEvent:
                        //     await HandleRouteNodeMarkedForDeletion(domainEvent);
                        //     break;
                        // case NamingInfoModified domainEvent:
                        //     await HandleNamingInfoModified(domainEvent);
                        //     break;
                    }
                }
            }

            return await Unit.Task;
        }

        private async Task HandleRouteNodeAdded(RouteNodeAdded routeNodeAdded)
        {
            _logger.LogInformation(nameof(HandleRouteNodeAdded));

            if (!string.IsNullOrWhiteSpace(routeNodeAdded?.NamingInfo?.Name))
            {
                var geometry = JsonConvert.DeserializeObject<string[]>(routeNodeAdded.Geometry);
                await _typesense.CreateDocument<TypesenseRouteNode>("RouteNodes", new TypesenseRouteNode
                {
                    Id = routeNodeAdded.NodeId.ToString(),
                    Name = routeNodeAdded.NamingInfo.Name.Trim(),
                    NorthCoordinate = geometry[0],
                    EastCoordinate = geometry[1]
                });
            }
        }

        private async Task HandleRouteNodeMarkedForDeletion(RouteNodeMarkedForDeletion routeNodeMarkedForDeletion)
        {
            _logger.LogInformation(nameof(HandleRouteNodeMarkedForDeletion));
            await _typesense.DeleteDocument<TypesenseRouteNode>("RouteNodes", routeNodeMarkedForDeletion.NodeId.ToString());
        }

        private async Task HandleNamingInfoModified(NamingInfoModified namingInfoModified)
        {
            _logger.LogInformation(nameof(HandleNamingInfoModified));
            if (namingInfoModified.AggregateType.ToLower() != "routenode")
                return;

            var result = await _typesense
                .RetrieveDocument<TypesenseRouteNode>("RouteNodes", namingInfoModified.AggregateId.ToString());

            if (result is not null)
            {
                if (string.IsNullOrWhiteSpace(namingInfoModified?.NamingInfo?.Name))
                {
                    // we delete it because the name has been removed
                    await _typesense.DeleteDocument<TypesenseRouteNode>(
                        "RouteNodes",
                        namingInfoModified.AggregateId.ToString());
                }
                else
                {
                    await _typesense.UpsertDocument<TypesenseRouteNode>("RouteNodes", new TypesenseRouteNode
                    {
                        Id = namingInfoModified.AggregateId.ToString(),
                        Name = namingInfoModified.NamingInfo.Name.Trim(),
                        NorthCoordinate = result.NorthCoordinate,
                        EastCoordinate = result.EastCoordinate,
                    });
                }
            }
        }
    }
}
