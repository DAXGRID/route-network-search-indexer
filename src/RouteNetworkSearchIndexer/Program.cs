using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenFTTH.EventSourcing;
using RouteNetworkSearchIndexer.Config;
using System;
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

        var loggerFactory = host.Services.GetService<ILoggerFactory>();
        var logger = loggerFactory!.CreateLogger(nameof(Program));

        host.Services.GetService<IEventStore>()!.ScanForProjections();

        try
        {
            await host.StartAsync().ConfigureAwait(false);
            await host.WaitForShutdownAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogCritical("{Exception}", ex);
            throw;
        }
    }
}
