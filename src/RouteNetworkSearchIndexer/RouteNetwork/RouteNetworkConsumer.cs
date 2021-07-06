using System;
using Topos.Config;
using Microsoft.Extensions.Options;
using RouteNetworkSearchIndexer.Config;
using Microsoft.Extensions.Logging;
using OpenFTTH.Events.RouteNetwork;
using DAX.EventProcessing.Serialization;
using MediatR;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    public class RouteNetworkConsumer : IRouteNetworkConsumer
    {
        private IDisposable _consumer;
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
                .Consumer(_kafkaSetting.Consumer, c => c.UseKafka(_kafkaSetting.Server))
                .Logging(l => l.UseSerilog())
                .Positions(x => x.StoreInPostgreSql(_kafkaSetting.PositionConnectionString, _kafkaSetting.Consumer))
                .Serialization(x => x.GenericEventDeserializer<RouteNetworkEditOperationOccuredEvent>())
                .Topics(x => x.Subscribe(_kafkaSetting.Topic))
                .Options(x => x.SetMinimumBatchSize(1))
                .Handle(async (messages, context, token) =>
                {
                    foreach (var message in messages)
                    {
                        switch (message.Body)
                        {
                            case RouteNetworkEditOperationOccuredEvent domainEvent:
                                await _mediator.Send(domainEvent);
                                break;
                        }
                    }
                })
                .Start();
        }

        public void Dispose()
        {
            if (_consumer is not null)
            {
                _consumer.Dispose();
            }
        }
    }
}
