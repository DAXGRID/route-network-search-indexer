using Microsoft.Extensions.Hosting;
using RouteNetworkSearchIndexer.Config;
using System.IO;
using System.Threading.Tasks;

namespace RouteNetworkSearchIndexer;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        var dotenv = Path.Combine(root, ".env");
        DotEnv.Load(dotenv);

        using var host = HostConfig.Configure();
        await host.StartAsync().ConfigureAwait(false);
        await host.WaitForShutdownAsync().ConfigureAwait(false);
    }
}
