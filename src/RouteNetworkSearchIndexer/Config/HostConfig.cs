using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenFTTH.EventSourcing;
using OpenFTTH.EventSourcing.Postgres;
using RouteNetworkSearchIndexer.RouteNetwork;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.Collections.Generic;
using Typesense.Setup;

namespace RouteNetworkSearchIndexer.Config;

internal static class HostConfig
{
    public static IHost Configure()
    {
        var hostBuilder = new HostBuilder();

        ConfigureApp(hostBuilder);
        ConfigureLogging(hostBuilder);
        ConfigureServices(hostBuilder);
        ConfigureSerialization();

        return hostBuilder.Build();
    }

    private static void ConfigureApp(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
        {
            config.AddEnvironmentVariables();
        });
    }

    private static void ConfigureSerialization()
    {
        JsonConvert.DefaultSettings = (() =>
        {
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.Converters.Add(new StringEnumConverter());
            settings.TypeNameHandling = TypeNameHandling.Auto;
            return settings;
        });
    }

    private static void ConfigureServices(IHostBuilder hostBuilder)
    {
        var typesenseApi = Environment.GetEnvironmentVariable("TYPESENSE__APIKEY");
        if (typesenseApi is null)
        {
            throw new ArgumentNullException($"{nameof(typesenseApi)} cannot be null.");
        }

        var typesenseHost = Environment.GetEnvironmentVariable("TYPESENSE__HOST");
        if (typesenseHost is null)
        {
            throw new ArgumentNullException($"{nameof(typesenseHost)} cannot be null.");
        }

        var typesensePort = Environment.GetEnvironmentVariable("TYPESENSE__PORT");
        if (typesensePort is null)
        {
            throw new ArgumentNullException($"{nameof(typesensePort)} cannot be null.");
        }

        var typesenseProtocol = Environment.GetEnvironmentVariable("TYPESENSE__PROTOCOL");
        if (typesenseProtocol is null)
        {
            throw new ArgumentException($"{nameof(typesenseProtocol)} cannot be null.");
        }

        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<RouteNetworkSearchIndexerHost>();
            services.AddTypesenseClient(c =>
            {
                c.ApiKey = typesenseApi;
                c.Nodes = new List<Node>
                {
                    new Node(
                        host: typesenseHost,
                        port: typesensePort,
                        protocol: typesenseProtocol)
                };
            });

            services.AddSingleton<IProjection, RouteNetworkProjection>();

            services.AddSingleton<IEventStore>(
                e =>
                new PostgresEventStore(
                    serviceProvider: e.GetRequiredService<IServiceProvider>(),
                    connectionString: hostContext.Configuration.GetSection("EventStore").GetValue<string>("ConnectionString"),
                    databaseSchemaName: "events"
                )
            );
        });
    }

    private static void ConfigureLogging(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            var loggingConfiguration = new ConfigurationBuilder()
               .AddEnvironmentVariables().Build();

            services.AddLogging(loggingBuilder =>
            {
                var logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(loggingConfiguration)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(new CompactJsonFormatter())
                    .CreateLogger();

                loggingBuilder.AddSerilog(logger, true);
            });
        });
    }
}
