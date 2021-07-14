using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenFTTH.Events.RouteNetwork;
using Typesense;
using OpenFTTH.Events.Core;
using System;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    public class RouteNetworkEventHandler : IRequestHandler<RouteNetworkEditOperationOccuredEvent>
    {
        private readonly ILogger<RouteNetworkEventHandler> _logger;
        private readonly ITypesenseClient _typesense;
        private const string RouteNodeCollectionName = "RouteNodes";

        public RouteNetworkEventHandler(ILogger<RouteNetworkEventHandler> logger, ITypesenseClient typesense)
        {
            _logger = logger;
            _typesense = typesense;
        }

        public async Task<Unit> Handle(
            RouteNetworkEditOperationOccuredEvent request,
            CancellationToken cancellationToken)
        {
            foreach (var command in request.RouteNetworkCommands)
            {
                foreach (var routeNetworkEvent in command?.RouteNetworkEvents)
                {
                    try
                    {
                        switch (routeNetworkEvent)
                        {
                            case RouteNodeAdded domainEvent:
                                await HandleRouteNodeAdded(domainEvent);
                                break;
                            case RouteNodeMarkedForDeletion domainEvent:
                                await HandleRouteNodeMarkedForDeletion(domainEvent);
                                break;
                            case NamingInfoModified domainEvent:
                                await HandleNamingInfoModified(domainEvent);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }
                }
            }

            return await Unit.Task;
        }

        private async Task HandleRouteNodeAdded(RouteNodeAdded routeNodeAdded)
        {
            if (!string.IsNullOrWhiteSpace(routeNodeAdded?.NamingInfo?.Name))
            {
                _logger.LogInformation($"{nameof(HandleRouteNodeAdded)}, NodeId: '{routeNodeAdded.NodeId}'");
                var geometry = JsonConvert.DeserializeObject<string[]>(routeNodeAdded.Geometry);
                await _typesense.CreateDocument<TypesenseRouteNode>("RouteNodes", new TypesenseRouteNode
                {
                    Id = routeNodeAdded.NodeId.ToString(),
                    Name = routeNodeAdded.NamingInfo.Name.Trim(),
                });
            }
        }

        private async Task HandleRouteNodeMarkedForDeletion(RouteNodeMarkedForDeletion routeNodeMarkedForDeletion)
        {
            _logger.LogInformation(
                $"{nameof(HandleRouteNodeMarkedForDeletion)}, NodeId: '{routeNodeMarkedForDeletion.NodeId}'");
            await _typesense.DeleteDocument<TypesenseRouteNode>(
                "RouteNodes", routeNodeMarkedForDeletion.NodeId.ToString());
        }

        private async Task HandleNamingInfoModified(NamingInfoModified namingInfoModified)
        {
            if (namingInfoModified.AggregateType.ToLower() != "routenode")
                return;

            if (!string.IsNullOrWhiteSpace(namingInfoModified.NamingInfo.Name))
            {
                _logger.LogInformation(
                    $"{nameof(HandleNamingInfoModified)}, NodeId: '{namingInfoModified.AggregateId}'");
                await _typesense.UpsertDocument<TypesenseRouteNode>("RouteNodes", new TypesenseRouteNode
                {
                    Id = namingInfoModified.AggregateId.ToString(),
                    Name = namingInfoModified.NamingInfo.Name.Trim(),
                });
            }
            else
            {
                _logger.LogInformation(
                    $"{nameof(HandleNamingInfoModified)}, NodeId: '{namingInfoModified.AggregateId}' deletes if exists");

                // we delete it because the name has been removed so it should no longer be searchable
                // when name is empty
                await _typesense.DeleteDocument<TypesenseRouteNode>(
                    "RouteNodes",
                    namingInfoModified.AggregateId.ToString());
            }
        }
    }
}
