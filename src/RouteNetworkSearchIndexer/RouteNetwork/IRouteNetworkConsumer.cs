using System;

namespace RouteNetworkSearchIndexer.RouteNetwork
{
    public interface IRouteNetworkConsumer : IDisposable
    {
        void Consume();
    }
}
