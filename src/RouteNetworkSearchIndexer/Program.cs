using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RouteNetworkSearchIndexer.Config;

namespace RouteNetworkSearchIndexer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var host = HostConfig.Configure())
            {
                await host.StartAsync();
                await host.WaitForShutdownAsync();
            }
        }
    }
}
