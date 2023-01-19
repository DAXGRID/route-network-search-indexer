using DAX.EventProcessing.Serialization;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenFTTH.Events.RouteNetwork;
using RouteNetworkSearchIndexer.Config;
using System;
using Topos.Config;

namespace RouteNetworkSearchIndexer.RouteNetwork;

internal sealed class RouteNetworkConsumer : IRouteNetworkConsumer
{
    private DateTime? _lastMessageReceived = null;
    private IDisposable? _consumer;
    private readonly KafkaSetting _kafkaSetting;
    private readonly ILogger<RouteNetworkConsumer> _logger;
    private readonly IMediator _mediator;

    public RouteNetworkConsumer(
        IOptions<KafkaSetting> kafkaSetting,
        ILogger<RouteNetworkConsumer> logger,
        IMediator mediator)
    {
        _kafkaSetting = kafkaSetting.Value;
        _logger = logger;
        _mediator = mediator;
    }

    public void Consume()
    {
        _consumer = Configure
            .Consumer($"{_kafkaSetting.Consumer}-{Guid.NewGuid()}", c => c.UseKafka(_kafkaSetting.Server))
            .Logging(l => l.UseSerilog())
            .Positions(x => x.StoreInMemory())
            .Serialization(x => x.GenericEventDeserializer<RouteNetworkEditOperationOccuredEvent>())
            .Topics(x => x.Subscribe(_kafkaSetting.Topic))
            .Handle(async (messages, context, token) =>
            {
                foreach (var message in messages)
                {
                    switch (message.Body)
                    {
                        case RouteNetworkEditOperationOccuredEvent domainEvent:
                            _lastMessageReceived = DateTime.UtcNow;
                            await _mediator.Send(domainEvent).ConfigureAwait(false);
                            break;
                    }
                }
            }).Start();
    }

    public DateTime? LastMessageReceived()
    {
        return _lastMessageReceived;
    }

    public void Dispose()
    {
        if (_consumer is not null)
        {
            _consumer.Dispose();
        }
    }
}
