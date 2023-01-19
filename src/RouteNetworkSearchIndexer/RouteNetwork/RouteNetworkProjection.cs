using Microsoft.Extensions.Logging;
using OpenFTTH.EventSourcing;
using OpenFTTH.Events.Core;
using OpenFTTH.Events.RouteNetwork;
using RouteNetworkSearchIndexer.Config;
using System;
using System.Threading.Tasks;
using Typesense;

namespace RouteNetworkSearchIndexer.RouteNetwork;

internal sealed class RouteNetworkProjection : ProjectionBase
{
    private readonly ILogger<RouteNetworkProjection> _logger;
    private readonly ITypesenseClient _typesense;

    public RouteNetworkProjection(
        ILogger<RouteNetworkProjection> logger,
        ITypesenseClient typesense)
    {
        _logger = logger;
        _typesense = typesense;

        ProjectEventAsync<RouteNetworkEditOperationOccuredEvent>(Project);
    }

    public async Task Project(IEventEnvelope eventEnvelope)
    {
        switch (eventEnvelope.Data)
        {
            case (RouteNetworkEditOperationOccuredEvent @event):
                await Handle(@event).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentException(
                    $"Could not handle type {eventEnvelope.GetType()}");
        }
    }

    public async Task Handle(
        RouteNetworkEditOperationOccuredEvent request)
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
    }

    private async Task HandleRouteNodeAdded(RouteNodeAdded routeNodeAdded)
    {
        // If the route node does not have a name we do not want to index it.
        if (string.IsNullOrWhiteSpace(routeNodeAdded?.NamingInfo?.Name))
        {
            return;
        }

        _logger.LogDebug($"{nameof(HandleRouteNodeAdded)}, NodeId: '{routeNodeAdded.NodeId}'");

        var typesenseRouteNode = new TypesenseRouteNode(routeNodeAdded.NodeId.ToString(), routeNodeAdded.NamingInfo.Name.Trim());
        await _typesense
            .CreateDocument<TypesenseRouteNode>(
                TypesenseCollectionConfig.CollectionName,
                typesenseRouteNode)
            .ConfigureAwait(false);
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
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(namingInfoModified.NamingInfo?.Name))
        {
            _logger.LogDebug(
                $"{nameof(HandleNamingInfoModified)}, NodeId: '{namingInfoModified.AggregateId}'");

            var typesenseRouteNode = new TypesenseRouteNode(
                namingInfoModified.AggregateId.ToString(),
                namingInfoModified.NamingInfo.Name.Trim());

            await _typesense
                .UpsertDocument<TypesenseRouteNode>(
                    TypesenseCollectionConfig.CollectionName,
                    typesenseRouteNode)
                .ConfigureAwait(false);
        }
        else
        {
            try
            {
                // Delete it because the name has been removed so it should no longer be searchable.
                await _typesense
                    .DeleteDocument<TypesenseRouteNode>(
                        TypesenseCollectionConfig.CollectionName,
                        namingInfoModified.AggregateId.ToString())
                    .ConfigureAwait(false);

                _logger.LogDebug(
                    $"{nameof(HandleNamingInfoModified)}, NodeId: '{namingInfoModified.AggregateId}' deleted.");
            }
            catch (TypesenseApiNotFoundException)
            {
                // This can happen if it changes from empty name to empty name with whitespaces.
            }
        }
    }
}
