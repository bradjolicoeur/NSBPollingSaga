using Contracts.Commands;
using FluentScheduler;
using NServiceBus;
using Microsoft.Extensions.Logging;
using NServiceBus.Routing;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProducerEndpoint
{
    public class SendMessageJob : IJob, ISendMessageJob
    {
        private readonly ILogger _logger;

        public IEndpointInstance Endpoint { get; set; }

        public SendMessageJob(ILogger<SendMessageJob> logger)
        {
            _logger = logger;
        }

        public void Execute()
        {
            var guid = Guid.NewGuid();
            _logger.LogInformation($"Sending Process Request for: {guid:N}");
            var message = GenerateMessage(guid);

            //Send a message to a specific queue; use NSB routing for production ready code instead of sending direct to queue name
            Endpoint.Send("PollingRequestSaga", message)
                .GetAwaiter().GetResult();
        }

        private static ProcessPollingRequest GenerateMessage(Guid guid)
        {
            //create a message
            var request = new ProcessPollingRequest
            {
                RequestId = guid.ToString(),
                RequestTime = DateTime.UtcNow,
                ApiURL = "https://localhost:44381/api/values",
                PollIntervalSeconds = 20,
                MaxPollAttempts = 5
            };

            return request;
        }
    }
}
