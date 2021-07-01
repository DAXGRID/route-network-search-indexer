using System;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    internal interface IRouteNetworkConsumer : IDisposable
    {
        void Consume();
    }
}
