using System;
using System.Collections.Generic;
using DAX.EventProcessing.Dispatcher;
using DAX.EventProcessing.Dispatcher.Topos;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenFTTH.Events.RouteNetwork;
using RouteNetworkSearchIndexer.RouteNetwork;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Typesense;
using Typesense.Setup;

namespace RouteNetworkSearchIndexer.Config
{
    public static class HostConfig
    {
        public static IHost Configure()
        {
            var hostBuilder = new HostBuilder();

            ConfigureApp(hostBuilder);
            ConfigureLogging(hostBuilder);
            ConfigureServices(hostBuilder);
            ConfigureSerialization(hostBuilder);

            return hostBuilder.Build();
        }

        private static void ConfigureApp(IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables();
            });
        }

        private static void ConfigureSerialization(IHostBuilder hostBuilder)
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
            hostBuilder.ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();
                services.AddMediatR(typeof(Program));
                services.AddTransient<IToposTypedEventMediator<RouteNetworkEditOperationOccuredEvent>,
                    ToposTypedEventMediator<RouteNetworkEditOperationOccuredEvent>>();
                services.AddHostedService<RouteNetworkSearchIndexerHost>();
                services.AddTransient<IRouteNetworkConsumer, RouteNetworkConsumer>();
                services.AddTypesenseClient(c =>
                {
                    c.ApiKey = "";
                    c.Nodes = new List<Node>
                    {
                        new Node
                        {
                            Host = "",
                            Port = "80",
                            Protocol = "http",
                        }
                    };
                });
                services.Configure<KafkaSetting>(kafkaSettings =>
                                                 hostContext.Configuration.GetSection("kafka").Bind(kafkaSettings));
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
}
