using FluentScheduler;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProducerEndpoint
{
    internal class HostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly EndpointConfiguration _endpointConfiguration;
        private readonly ISendMessageJob _job;

        private IEndpointInstance EndpointInstance { get; set; }

        public HostedService(ILogger<HostedService> logger, EndpointConfiguration endpointConfiguration, ISendMessageJob job)
        {
            _logger = logger;
            _endpointConfiguration = endpointConfiguration;
            _job = job;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is starting.");

            //Start NSB Endpoint
            EndpointInstance = await Endpoint.Start(_endpointConfiguration);

            //schedule job to send request message
            //Note, if this endpoint is scaled out, each instance will execute this job
            ConfigureJobLogger();
            _job.Endpoint = EndpointInstance;
            JobManager.AddJob(
                (IJob)_job,
                schedule =>
                {
                    schedule
                        .ToRunNow()
                        .AndEvery(20).Seconds();
                });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service is stopping.");

            if (EndpointInstance != null)
            { await EndpointInstance.Stop().ConfigureAwait(false); }
        }

        private void ConfigureJobLogger()
        {
            JobManager.JobException += info =>
            {
                _logger.LogError($"Error occurred in job: {info.Name}", info.Exception);
            };
            JobManager.JobStart += info =>
            {
                _logger.LogDebug($"Start job: {info.Name}. Duration: {info.StartTime}");
            };
            JobManager.JobEnd += info =>
            {
                _logger.LogDebug($"End job: {info.Name}. Duration: {info.Duration}. NextRun: {info.NextRun}.");
            };
        }
    }
}
