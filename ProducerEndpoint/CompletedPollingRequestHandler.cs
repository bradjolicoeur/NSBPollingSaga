using System.Threading.Tasks;
using Contracts.Events;
using Microsoft.Extensions.Logging;
using NServiceBus;


namespace ProducerEndpoint
{
    public class CompletedPollingRequestHandler : IHandleMessages<ICompletedPollingRequest>
    {
        private readonly ILogger _logger;

        public CompletedPollingRequestHandler(ILogger<CompletedPollingRequestHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle( ICompletedPollingRequest message, IMessageHandlerContext context)
        {
            _logger.LogInformation($"Handle Completed Request for {message.RequestId} " 
                        + message.Message + " attempts:" + message.NumberOfAttempts.ToString());

            //Suggestion; This handler might be where you do a webhook post back to the orininal caller

            return Task.CompletedTask;
        }
    }
}
