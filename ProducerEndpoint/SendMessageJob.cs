using Contracts.Commands;
using FluentScheduler;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Routing;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProducerEndpoint
{
    public class SendMessageJob : IJob
    {
        static ILog log = LogManager.GetLogger<SendMessageJob>();

        IEndpointInstance Endpoint;

        public SendMessageJob(IEndpointInstance endpoint)
        {
            Endpoint = endpoint;
        }

        public void Execute()
        {
            var guid = Guid.NewGuid();
            log.Info($"Sending Process Request for: {guid:N}");
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
                ApiURL = "https://localhost:44381/api/values"
            };

            return request;
        }
    }
}
