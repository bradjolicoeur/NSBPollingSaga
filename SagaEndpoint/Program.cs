namespace SagaEndpoint
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.Logging;
    using System;
    using System.Threading.Tasks;
    using Infrastructure.Configuration;

    class Program
    {
        private static ILog log;

        public static IConfigurationRoot configuration;

        private static IEndpointInstance EndpointInstance { get; set; }

        public const string EndpointName = "PollingRequestSaga";

        static async Task Main()
        {
            // Create service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            //Set console title
            Console.Title = EndpointName;

            //Configure logging
            LogManager.Use<DefaultFactory>()
                .Level(LogLevel.Info);
            log = LogManager.GetLogger<Program>();

            //Configure NSB Endpoint
            EndpointConfiguration endpointConfiguration = EndpointConfigurations.ConfigureNSB(serviceCollection, EndpointName);

            //only enable this in dev/debug scenarios.  It produces a significant amount of data that will need to be processed by service control
            //endpointConfiguration.AuditSagaStateChanges(serviceControlQueue: "particular.servicecontrol");

            //Start NSB Endpoint
            EndpointInstance = await Endpoint.Start(endpointConfiguration);

            //Support Graceful Shut Down of NSB Endpoint in PCF
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            log.Info("ENDPOINT READY");

            Console.Read();

        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (EndpointInstance != null)
            { EndpointInstance.Stop().ConfigureAwait(false); }

            log.Info("Exiting!");
        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
        }

    }
}
