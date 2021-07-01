using System;
using Topos.Config;
using Microsoft.Extensions.Options;
using RouteNetworkSearchIndexer.Config;
using Microsoft.Extensions.Logging;
using OpenFTTH.Events.RouteNetwork;
using DAX.EventProcessing.Dispatcher;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    internal class RouteNetworkConsumer : IRouteNetworkConsumer
    {
        private IDisposable _consumer;
        private readonly KafkaSetting _kafkaSetting;
        private readonly ILogger<RouteNetworkConsumer> _logger;
        private readonly IToposTypedEventMediator<RouteNetworkEditOperationOccuredEvent> _eventMediator;

        public RouteNetworkConsumer(
            IOptions<KafkaSetting> kafkaSetting,
            ILogger<RouteNetworkConsumer> logger,
            IToposTypedEventMediator<RouteNetworkEditOperationOccuredEvent> eventMediator)
        {
            _kafkaSetting = kafkaSetting.Value;
            _logger = logger;
            _eventMediator = eventMediator;
        }

        public void Consume()
        {
            _consumer = _eventMediator
                .Config(_kafkaSetting.Consumer + Guid.NewGuid(), c =>
                {
                    c.UseKafka(_kafkaSetting.Server);
                })
                .Logging(l => l.UseSerilog())
                .Positions(x => x.StoreInMemory())
                .Topics(x => x.Subscribe(_kafkaSetting.Topic))
                .Handle(async (messages, context, token) =>
                {
                    foreach (var message in messages)
                    {
                        if (message.Body is RouteNetworkEditOperationOccuredEvent)
                        {
                            var editEvent = message.Body as RouteNetworkEditOperationOccuredEvent;
                            _logger.LogInformation("Id is " + editEvent.EventId);
                        }
                    }
                })
                .Start();
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }
    }
}
