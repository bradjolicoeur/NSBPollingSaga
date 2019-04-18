namespace SagaEndpoint
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NServiceBus;
    using NServiceBus.Logging;
    using System;
    using System.Threading.Tasks;
    using Infrastructure.Configuration;
    using System.Net.Http;
    using SagaEndpoint.Proxy;
    using System.IO;

    class Program
    {
        public const string EndpointName = "PollingRequestSaga";

        static async Task Main()
        {

            //Set console title
            Console.Title = EndpointName;

            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("hostsettings.json", optional: true);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.AddJsonFile("appsettings.json", optional: true);
                    configApp.AddJsonFile(
                        $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                        optional: true);
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<HttpClient>();

                    services.AddSingleton<IApiProxy, ApiProxy>();

                    //Configure NSB Endpoint
                    services.AddSingleton<EndpointConfiguration>(EndpointConfigurations.ConfigureNSB(services, EndpointName));

                    services.AddHostedService<HostedService>();

                })
                
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();

        }


    }
}
