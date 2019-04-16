using System.Threading.Tasks;
using Contracts.Events;
using NServiceBus;
using NServiceBus.Logging;

namespace ProducerEndpoint
{
    public class CompletedPollingRequestHandler : IHandleMessages<ICompletedPollingRequest>
    {
        static ILog log = LogManager.GetLogger<CompletedPollingRequestHandler>();

        public Task Handle(ICompletedPollingRequest message, IMessageHandlerContext context)
        {
            log.Info($"Handle Completed Request for {message.RequestId} " 
                        + message.Message + " attempts:" + message.NumberOfAttempts.ToString());

            //Suggestion; This handler might be where you do a webhook post back to the orininal caller

            return Task.CompletedTask;
        }
    }
}
