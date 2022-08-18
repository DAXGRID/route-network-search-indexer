using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenFTTH.Events.Core;
using OpenFTTH.Events.RouteNetwork;
using RouteNetworkSearchIndexer.Config;
using System;
using System.Threading;
using System.Threading.Tasks;
using Typesense;

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

        public async Task<Unit> Handle(
            RouteNetworkEditOperationOccuredEvent request,
            CancellationToken cancellationToken)
        {
            foreach (var command in request.RouteNetworkCommands)
            {
                foreach (var routeNetworkEvent in command.RouteNetworkEvents)
                {
                    try
                    {
                        switch (routeNetworkEvent)
                        {
                            case RouteNodeAdded domainEvent:
                                await HandleRouteNodeAdded(domainEvent)
                                    .ConfigureAwait(false);
                                break;
                            case RouteNodeMarkedForDeletion domainEvent:
                                await HandleRouteNodeMarkedForDeletion(domainEvent)
                                    .ConfigureAwait(false);
                                break;
                            case NamingInfoModified domainEvent:
                                await HandleNamingInfoModified(domainEvent)
                                    .ConfigureAwait(false);
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

                await _typesense.CreateDocument<TypesenseRouteNode>(TypesenseCollectionConfig.CollectionName, new TypesenseRouteNode
                {
                    Id = routeNodeAdded.NodeId.ToString(),
                    Name = routeNodeAdded.NamingInfo.Name.Trim(),
                }).ConfigureAwait(false);
            }
        }

        private async Task HandleRouteNodeMarkedForDeletion(RouteNodeMarkedForDeletion routeNodeMarkedForDeletion)
        {
            try
            {
                await _typesense.DeleteDocument<TypesenseRouteNode>(
                    TypesenseCollectionConfig.CollectionName,
                    routeNodeMarkedForDeletion.NodeId.ToString()).ConfigureAwait(false);

                _logger.LogInformation(
                    $"{nameof(HandleRouteNodeMarkedForDeletion)}, NodeId: '{routeNodeMarkedForDeletion.NodeId}'");

            }
            catch (TypesenseApiNotFoundException)
            {
                // This is valid.
            }
        }

        private async Task HandleNamingInfoModified(NamingInfoModified namingInfoModified)
        {
            if (namingInfoModified.AggregateType.ToLower() != "routenode")
                return;

            if (!string.IsNullOrWhiteSpace(namingInfoModified.NamingInfo?.Name))
            {
                _logger.LogInformation(
                    $"{nameof(HandleNamingInfoModified)}, NodeId: '{namingInfoModified.AggregateId}'");
                await _typesense.UpsertDocument<TypesenseRouteNode>(TypesenseCollectionConfig.CollectionName, new TypesenseRouteNode
                {
                    Id = namingInfoModified.AggregateId.ToString(),
                    Name = namingInfoModified.NamingInfo.Name.Trim(),
                }).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    // we delete it because the name has been removed so it should no longer be searchable
                    // when name is empty
                    await _typesense.DeleteDocument<TypesenseRouteNode>(
                        TypesenseCollectionConfig.CollectionName,
                        namingInfoModified.AggregateId.ToString()).ConfigureAwait(false);

                    _logger.LogInformation(
                        $"{nameof(HandleNamingInfoModified)}, NodeId: '{namingInfoModified.AggregateId}' deleted.");
                }
                catch (TypesenseApiNotFoundException)
                {
                    // This is valid.
                }
            }
        }
    }
}
