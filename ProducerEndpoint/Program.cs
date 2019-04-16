namespace Producer
{

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus;
    using NServiceBus.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure.Configuration;
    using FluentScheduler;
    using ProducerEndpoint;
    using Contracts.Events;

    class Program
    {

        private static ILog log;

        public static IConfigurationRoot configuration;

        private static IEndpointInstance EndpointInstance { get; set; }

        public const string EndpointName = "Producer";

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

            //Start NSB Endpoint
            EndpointInstance = await Endpoint.Start(endpointConfiguration);

            //schedule job to send request message
            //Note, if this endpoint is scaled out, each instance will execute this job
            ConfigureJobLogger();
            JobManager.AddJob(
                new SendMessageJob(EndpointInstance),
                schedule =>
                {
                    schedule
                        .ToRunNow()
                        .AndEvery(20).Seconds();
                });

            //Support Graceful Shut Down of NSB Endpoint in PCF
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            log.Info("ENDPOINT READY");

            Console.Read();

        }

        private static void ConfigureJobLogger()
        {
            JobManager.JobException += info =>
            {
                log.Error($"Error occurred in job: {info.Name}", info.Exception);
            };
            JobManager.JobStart += info =>
            {
                log.Info($"Start job: {info.Name}. Duration: {info.StartTime}");
            };
            JobManager.JobEnd += info =>
            {
                log.Info($"End job: {info.Name}. Duration: {info.Duration}. NextRun: {info.NextRun}.");
            };
        }


        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            JobManager.StopAndBlock();

            if (EndpointInstance != null)
            { EndpointInstance.Stop().ConfigureAwait(false); }

            log.Info("Exiting!");
        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               //.AddEnvironmentVariables()
               //.AddCloudFoundry()
               //.AddConfigServer()
               .Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
        }

        private static string GetConnectionString()
        {
            string connection = configuration.GetConnectionString("");

            if (string.IsNullOrEmpty(connection))
                throw new Exception("Environment Variable 'AzureServiceBus_ConnectionString' not set");

            return connection;

        }

    }
}