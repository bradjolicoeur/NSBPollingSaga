using Microsoft.Extensions.DependencyInjection;
using System;
using NServiceBus;

namespace Infrastructure.Configuration
{
    public static class EndpointConfigurations
    {

        public static EndpointConfiguration ConfigureNSB(IServiceCollection serviceCollection, string endpointName)
        {

            var endpointConfiguration = new EndpointConfiguration(endpointName);

            //Config sections left commented for reference, they will require additional NuGet packages to enable
            //These are plugins that should be enabled for production ready code

            //heartbeat configuration, this is to identify when an endpoint is off or unresponsive
            //endpointConfiguration.SendHeartbeatTo(
            //        serviceControlQueue: "particular.servicecontrol",
            //        frequency: TimeSpan.FromSeconds(15),
            //        timeToLive: TimeSpan.FromSeconds(30));

            //metrics configuration for servicepulse 
            //var metrics = endpointConfiguration.EnableMetrics();

            //metrics.SendMetricDataToServiceControl(
            //    serviceControlMetricsAddress: "particular.monitoring",
            //    interval: TimeSpan.FromMinutes(1),
            //    instanceId: "INSTANCE_ID_OPTIONAL");

            //performance counter configuration
            //var performanceCounters = endpointConfiguration.EnableWindowsPerformanceCounters();
            //performanceCounters.EnableSLAPerformanceCounters(TimeSpan.FromSeconds(10));

            //configuring audit queue and error queue
            endpointConfiguration.AuditProcessedMessagesTo("audit"); //copy of message after processing will go here for servicecontroller
            endpointConfiguration.SendFailedMessagesTo("error"); //after specified retries is hit, message will be moved here for alerting and recovery

            //this is config for circut breaker
            var recoverability = endpointConfiguration.Recoverability();
            recoverability.Delayed(
                delayed =>
                {
                    delayed.NumberOfRetries(3);
                });

            var transport = endpointConfiguration.UseTransport<LearningTransport>(); //for production ready, replace with Azure Service Bus, RabbitMQ or MSMQ

            endpointConfiguration.UsePersistence<LearningPersistence>(); //for production ready, replace with nHibernate or Azure Storage Provider

            var conventions = endpointConfiguration.Conventions();
            NSBConventions.ConfigureConventions(conventions); //this will configure message types by the defined conventions

            endpointConfiguration.EnableInstallers(); //not for production

            endpointConfiguration.UseContainer<ServicesBuilder>(
            customizations: customizations =>
            {
                customizations.ExistingServices(serviceCollection);
            });

            return endpointConfiguration;

        }
    }
}
