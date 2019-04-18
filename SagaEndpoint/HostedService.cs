using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SagaEndpoint
{
    internal class HostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly EndpointConfiguration _endpointConfiguration;

        private IEndpointInstance EndpointInstance { get; set; }

        public HostedService(ILogger<HostedService> logger, EndpointConfiguration endpointConfiguration)
        {
            _logger = logger;
            _endpointConfiguration = endpointConfiguration;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is starting.");
            //only enable this in dev/debug scenarios.  It produces a significant amount of data that will need to be processed by service control
            //endpointConfiguration.AuditSagaStateChanges(serviceControlQueue: "particular.servicecontrol");

            //Start NSB Endpoint
            EndpointInstance = await Endpoint.Start(_endpointConfiguration);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is stopping.");

            if (EndpointInstance != null)
            { await EndpointInstance.Stop().ConfigureAwait(false); }
        }
    }
}
