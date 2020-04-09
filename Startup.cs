using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DurableFunctionPOC.Model;
using System.Net.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;

[assembly: FunctionsStartup(typeof(MyNamespace.Startup))]

namespace MyNamespace
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var localRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            var azureRoot = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";
            var actualRoot = localRoot ?? azureRoot;

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(actualRoot)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.debug.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.prod.json", optional: true, reloadOnChange: true)
                .Build();

            builder.Services.Configure<AppConfig>(configuration);
            builder.Services.AddLogging(loggingBuilder =>
            {
                var logLevel = LogEventLevel.Debug;
                var levelSwitch = new LoggingLevelSwitch(logLevel);
                var logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("System", "Durable Function POC System")
                    .Enrich.WithProperty("Service", "Durable Function POC Service")
                    .Enrich.WithProperty("Env", "local")
                    .WriteTo.Seq(
                        "http://localhost:5341",
                        apiKey: "GOvgyWoisx87qAGy7vGY",
                        controlLevelSwitch: levelSwitch,
                        compact: true)
                    .WriteTo.ColoredConsole()
                    .MinimumLevel.ControlledBy(levelSwitch)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .CreateLogger();
                loggingBuilder.AddSerilog(logger);
            });
        }
    }
}